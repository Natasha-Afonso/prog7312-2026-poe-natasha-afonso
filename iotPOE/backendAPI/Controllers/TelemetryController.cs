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
            
                return BadRequest("SensorIds and RawBatches must be the same length");
            

            var packets = _ingestionService.ProcessRawBatches(request.SensorIds, request.RawBatches);
            var skippedIdentifiers = new List<string>();

            foreach (var packet in packets)
            {
                var sensor = await _context.Sensors
                    .FirstOrDefaultAsync(s => s.DeviceIdentifier == packet.SensorId);

                if (sensor == null)
                {
                    skippedIdentifiers.Add(packet.SensorId);
                    continue;
                }

                _context.SensorReadings.Add(new SensorReading
                {
                    SensorId = sensor.Id,
                    MetricName = packet.MetricName,
                    Value = packet.Value,
                    Timestamp = packet.Timestamp
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ingested = packets.Count - skippedIdentifiers.Count,
                skipped = skippedIdentifiers
            });
        }

        // GET: api/Telemetry/aggregate?sensorIdA=1&sensorIdB=2
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
            
                return NotFound("One or both sensors have no readings yet");
            

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
            
                return NotFound("This sensor has no readings yet");
            

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
Chandra, S. 2026. JWT Authentication in ASP.NET Core: A complete step-bystey guide. (Version 10.0) [Source Code]. Available at: <https://medium.com/@Rajdip27/jwt-authentication-in-asp-net-core-a-complete-step-by-step-guide-94c955d907da> [Accessed 7 June 2026]
CodeWithGopi. 2025. Create and Consume Web API in ASP.NET Core MVC | Full CRUD Operatins & API Consumption. (Version 10.0) [Source Code]. Available at:<https://youtu.be/knTcwvFWOQM?si=b3q3ksLg6HNnBjDg> [Accessed 4 June 2026]
Emad, C. 2024. ASP.NET Core Web API with JWT (JSON Web Token) authentication. (Version 10.0) [Source Code]. Available at: <https://github.com/carolemad965/ASP.NET-Core-Web-API-with-JWT-Authentication> [Accessed 7 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 1 - Getting started. (Version 10.0) [Source Code]. Available at: <https://youtu.be/ldfC-rTzHPY?si=znsmPz-ZSPaY-4Ii> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 2- connecting between the two!. (Version 10.0) [Source Code]. Available at: <https://youtu.be/_0EhC3WauAc?si=nEazkCpBTV1YpHBp> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 3- databases in docker. (Version 10.0) [Source Code]. Available at: <https://youtu.be/6tYEbngWY-E?si=10rSEEpfSNL5Kg8k> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 4- connecting to database from the API.(Version 10.0) [Source Code]. Available at: <https://youtu.be/_PwpPMoeqzw?si=Q0ELlPZdN42dFU9q> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 5- connecting them all together- DB -API -MVC. (Version 10.0) [Source Code]. Available at: <https://youtu.be/YlIajRMdLPk?si=D75XZKO-L6nQhR-3> [Accessed 5 June 2026]
Microsoft Learn. 2026. Parameter Binding in Minimal API apps. (Version 10)[source code]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0> [Accessed 5 June 2026]
Microsoft Learn. 2023. Use IHttpClientFactory to implement resilient HTTP requests. (Version 10)[source code]. Available at: <https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests> [Accessed 5 June 2026]
Verma, S. 2024. Model Binding in ASP.NET Core. (Version 10)[source code]. Available at: <https://www.c-sharpcorner.com/article/model-binding-in-asp-net-core2/> [Accessed 5 June 2026]           
Saha, D.2024. Implement Cascading Dropdown In ASP.NET Core MVC Application Using InMemory DB.(Version 10)[source code]. Available at: < https://www.c-sharpcorner.com/article/implement-cascading-dropdown-in-asp-net-core-mvc-application-using-inmemory-db/ > [Accessed 7 June 2026]
Williams, R.2026.SEND IT QUICK COURIER SERVICE.(Version 10)[source code]. Available at: < https://codepen.io/dasia16/pen/azvEBBp > [Accessed 5 June 2026]
*/