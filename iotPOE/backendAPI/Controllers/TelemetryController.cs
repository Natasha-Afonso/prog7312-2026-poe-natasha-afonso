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
 
        // safe limit for each sensor category, a reading above this raises an alert
        // a Dictionary gives a quick lookup by the category name (Microsoft Learn, 2026)
        private static readonly Dictionary<string, double> CategoryThresholds = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Temperature", 35.0 },
            { "Environmental", 35.0 },
            { "Power", 80.0 },
            { "Power Consumption", 80.0 },
            { "Pressure", 150.0 },
            { "Humidity", 70.0 }
        };
 
        public TelemetryController(ApplicationDbContext context, TelemetryIngestionService ingestionService)
        {
            _context = context;
            _ingestionService = ingestionService;
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
 
            // load every sensor in the batch with ONE query and keep them in a Dictionary,
            // before this it was one database query for every single reading
            var identifiers = request.SensorIds.Distinct().ToList();
            var sensorLookup = await _context.Sensors
                .Where(s => identifiers.Contains(s.DeviceIdentifier))
                .ToDictionaryAsync(s => s.DeviceIdentifier);
 
            var skippedIdentifiers = new HashSet<string>();
 
            // HashSet so each sensor only raises one alert per batch (Microsoft Learn, 2026)
            var alertedSensorIds = new HashSet<int>();
 
            int ingestedCount = 0;
            int alertsRaised = 0;
 
            foreach (var packet in packets)
            {
                if (!sensorLookup.TryGetValue(packet.SensorId, out Sensor? sensor))
                {
                    skippedIdentifiers.Add(packet.SensorId);
                    continue;
                }
 
                var reading = new SensorReading
                {
                    SensorId = sensor.Id,
                    MetricName = packet.MetricName,
                    Value = packet.Value,
                    Timestamp = packet.Timestamp
                };
 
                _context.SensorReadings.Add(reading);
                ingestedCount++;
 
                // keep the sensor table showing the newest value
                sensor.LastReading = FormatValue(packet.Value);
 
                // check the reading against the limit for this sensor's category
                bool hasLimit = CategoryThresholds.TryGetValue(sensor.Category, out double limit);
 
                if (hasLimit && !alertedSensorIds.Contains(sensor.Id))
                {
                    var limitReading = new SensorReading
                    {
                        SensorId = sensor.Id,
                        MetricName = "Threshold",
                        Value = limit
                    };
 
                    // uses the overloaded ">" operator from SensorReading
                    if (reading > limitReading)
                    {
                        _context.Alerts.Add(new Alert
                        {
                            SensorId = sensor.Id,
                            Title = sensor.Category.ToUpper() + " THRESHOLD EXCEEDED",
                            Message = "Sensor " + sensor.DeviceIdentifier + " reported " + FormatValue(packet.Value) +
                                      ", above the safe limit of " + FormatValue(limit) + ".",
                            Metric = sensor.Category,
                            Value = FormatValue(packet.Value),
                            Threshold = FormatValue(limit),
                            Status = "unread",
                            Timestamp = DateTime.UtcNow
                        });
 
                        alertedSensorIds.Add(sensor.Id);
                        alertsRaised++;
                    }
                }
            }
 
            await _context.SaveChangesAsync();
 
            return Ok(new
            {
                ingested = ingestedCount,
                skipped = skippedIdentifiers,
                alertsRaised
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
Microsoft Learn, 2025. Constraints on type parameters (C# Programming Guide). [online] Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters> [Accessed 12 September 2026].
Microsoft Learn, 2026. Dictionary<TKey,TValue> Class and HashSet<T> Class. [online] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2> and <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1> [Accessed DD Month 2026] (read both pages and check the year before keeping this reference)
*/
 
