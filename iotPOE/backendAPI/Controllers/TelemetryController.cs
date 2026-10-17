using System.Globalization;
using backendAPI.Data;
using backendAPI.Models;
using backendAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

//Emeris School of Computer Science, 2025
namespace backendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelemetryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly TelemetryIngestionService _ingestionService;
        private readonly DeviceRegistryService _registry;
        private readonly TelemetryQueueService _queues;

        public TelemetryController(
            ApplicationDbContext context,
            TelemetryIngestionService ingestionService,
            DeviceRegistryService registry,
            TelemetryQueueService queues)
        {
            _context = context;
            _ingestionService = ingestionService;
            _registry = registry;
            _queues = queues;
        }

        // GET: api/Telemetry/5
        // returns the most recent readings for one sensor, for the chart/log
        [HttpGet("{sensorId}")]
        public async Task<ActionResult<IEnumerable<SensorReading>>> GetBySensor(int sensorId)
        {
            var readings = await _context.SensorReadings
                .Where(r => r.SensorId == sensorId)
                .OrderByDescending(r => r.Timestamp)
                .Take(25)
                .ToListAsync();

            return readings;
        }

        // POST: api/Telemetry/ingest-batch
        // accepts a jagged batch of raw readings, one row per sensor,
        // identified by their real-world device identifier
        [HttpPost("ingest-batch")]
        public async Task<ActionResult> IngestBatch(TelemetryBatchRequest request)
        {
            if (request.SensorIds.Length != request.RawBatches.Length)
            {
                return BadRequest("SensorIds and RawBatches must be the same length");
            }

            var packets = _ingestionService.ProcessRawBatches(request.SensorIds, request.RawBatches);

            var skippedIdentifiers = new HashSet<string>();
            int acceptedCount = 0;
            int priorityCount = 0;

            foreach (var packet in packets)
            {
                // O(1) lookup in the registry
                if (!_registry.TryGet(packet.SensorId, out DeviceState? device) || device == null)
                {
                    skippedIdentifiers.Add(packet.SensorId);
                    continue;
                }

                // the registry (the cache) is updated straight away, before anything is saved
                _registry.RecordReading(device.DeviceIdentifier, packet.Value, packet.Timestamp);

                // 0 = normal reading, 1 or more = critical (how far past the limit)
                int severity = AlertRules.GetSeverity(device.Category, packet.Value);

                if (severity > 0)
                {
                    // critical readings go into the PriorityQueue, the worst one (biggest severity) comes out first
                    _queues.EnqueuePriority(packet, -severity);
                    priorityCount++;
                }
                else
                {
                    // normal readings wait in the FIFO queue for the background worker
                    _queues.EnqueueNormal(packet);
                }

                acceptedCount++;
            }

            // critical readings bypass the standard queue and are processed right now, most severe first
            int alertsRaised = await ProcessPriorityNowAsync();

            return Ok(new
            {
                ingested = acceptedCount,
                skipped = skippedIdentifiers,
                alertsRaised,
                priorityReadings = priorityCount,
                queuedForWorker = acceptedCount - priorityCount
            });
        }

        // GET: api/Telemetry
        // demonstrates the "+" operator: combines the latest reading of two sensors
        [HttpGet("aggregate")]
        public async Task<ActionResult<SensorReading>> Aggregate(int sensorIdA, int sensorIdB)
        {
            var readingA = await _context.SensorReadings
                .Where(r => r.SensorId == sensorIdA)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            var readingB = await _context.SensorReadings
                .Where(r => r.SensorId == sensorIdB)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            if (readingA == null || readingB == null)
            {
                return NotFound("One or both sensors have no readings yet");
            }

            var combined = readingA + readingB;
            return combined;
        }

        // GET: api/Telemetry/threshold-check?sensorId=1&thresholdValue=35
        // demonstrates the "-" and ">" operators: checks whether a reading
        // has exceeded a threshold, and by how much
        [HttpGet("threshold-check")]
        public async Task<ActionResult> ThresholdCheck(int sensorId, double thresholdValue)
        {
            var latestReading = await _context.SensorReadings
                .Where(r => r.SensorId == sensorId)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            if (latestReading == null)
            {
                return NotFound("This sensor has no readings yet");
            }

            var thresholdReading = new SensorReading
            {
                SensorId = sensorId,
                MetricName = "Threshold",
                Value = thresholdValue
            };

            bool exceeded = latestReading > thresholdReading;
            var delta = latestReading - thresholdReading;

            return Ok(new
            {
                exceeded,
                delta = delta.Value
            });
        }

        // POST: api/Telemetry/switch-trigger
        // demonstrates TelemetryPacket<T> instantiated with a non-double T (bool)
        [HttpPost("switch-trigger")]
        public ActionResult<string> SwitchTrigger(string deviceIdentifier, bool isOn)
        {
            var packet = new TelemetryPacket<bool>(deviceIdentifier, "SwitchState", isOn);
            return Ok(packet.ToString());
        }

        // empties the PriorityQueue, saves the readings and raises the alerts straight away
        // returns how many alerts were created
        private async Task<int> ProcessPriorityNowAsync()
        {
            int alertsRaised = 0;

            // HashSet so each sensor only raises one alert per call (Microsoft Learn, 2026)
            var alertedSensorIds = new HashSet<int>();

            while (_queues.TryDequeuePriority(out TelemetryPacket<double>? packet))
            {
                if (packet == null)
                {
                    continue;
                }

                if (!_registry.TryGet(packet.SensorId, out DeviceState? device) || device == null)
                {
                    continue;
                }

                _context.SensorReadings.Add(new SensorReading
                {
                    SensorId = device.SensorId,
                    MetricName = packet.MetricName,
                    Value = packet.Value,
                    Timestamp = packet.Timestamp
                });

                // the most severe reading for a sensor comes out first, so later ones for the same sensor are skipped
                if (alertedSensorIds.Contains(device.SensorId))
                {
                    continue;
                }

                alertedSensorIds.Add(device.SensorId);

                // dont stack up alerts, skip if this sensor already has an unread one
                bool alreadyUnread = await _context.Alerts
                    .AnyAsync(a => a.SensorId == device.SensorId && a.Status == "unread");

                if (alreadyUnread)
                {
                    continue;
                }

                AlertRules.TryGetRule(device.Category, out AlertRule? rule);
                double limit = rule?.Limit ?? 0;
                bool tooHigh = rule == null || rule.AlertWhenAbove;

                _context.Alerts.Add(new Alert
                {
                    SensorId = device.SensorId,
                    Title = device.Category.ToUpper() + (tooHigh ? " THRESHOLD EXCEEDED" : " BELOW SAFE LEVEL"),
                    Message = "Sensor " + device.DeviceIdentifier + " reported " + FormatValue(packet.Value) +
                              ", " + (tooHigh ? "above" : "below") + " the safe limit of " + FormatValue(limit) + ".",
                    Metric = device.Category,
                    Value = FormatValue(packet.Value),
                    Threshold = FormatValue(limit),
                    Status = "unread",
                    Timestamp = DateTime.UtcNow
                });

                alertsRaised++;
            }

            await _context.SaveChangesAsync();

            return alertsRaised;
        }

        // always one decimal place with a "." so it does not change with the computer's region settings
        private static string FormatValue(double value)
        {
            return value.ToString("F1", CultureInfo.InvariantCulture);
        }
    }

    // Advanced Arrays and Lists: request shape carrying the jagged batch
    public class TelemetryBatchRequest
    {
        public string[] SensorIds { get; set; }
        public double[][] RawBatches { get; set; }
    }
}

/** 
References
CodeWithGopi. 2025. Create and Consume Web API in ASP.NET Core MVC | Full CRUD Operatins & API Consumption. (Version 10.0) [Source Code]. Available at:<https://youtu.be/knTcwvFWOQM?si=b3q3ksLg6HNnBjDg> [Accessed 4 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 1 - Getting started. (Version 10.0) [Source Code]. Available at: <https://youtu.be/ldfC-rTzHPY?si=znsmPz-ZSPaY-4Ii> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 2- connecting between the two!. (Version 10.0) [Source Code]. Available at: <https://youtu.be/_0EhC3WauAc?si=nEazkCpBTV1YpHBp> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 3- databases in docker. (Version 10.0) [Source Code]. Available at: <https://youtu.be/6tYEbngWY-E?si=10rSEEpfSNL5Kg8k> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 4- connecting to database from the API.(Version 10.0) [Source Code]. Available at: <https://youtu.be/_PwpPMoeqzw?si=Q0ELlPZdN42dFU9q> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 5- connecting them all together- DB -API -MVC. (Version 10.0) [Source Code]. Available at: <https://youtu.be/YlIajRMdLPk?si=D75XZKO-L6nQhR-3> [Accessed 5 June 2026]
Microsoft Learn, 2025. Constraints on type parameters (C# Programming Guide). (Version 10.0) [Source Code] Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters> [Accessed 12 September 2026].
Microsoft Learn, 2026. HashSet<T> Class.  (Version 10.0) [Source Code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1> [Accessed 16 Month 2026] 
*/