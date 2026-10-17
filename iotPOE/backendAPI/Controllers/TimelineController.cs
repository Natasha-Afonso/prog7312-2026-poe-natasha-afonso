using backendAPI.Data;
using backendAPI.Models;
using backendAPI.Services;
using Microsoft.AspNetCore.Mvc;

//Emeris School of Computer Science, 2025
namespace backendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimelineController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DeviceRegistryService _registry;
        private readonly TimelineService _timeline;

        public TimelineController(
            ApplicationDbContext context,
            DeviceRegistryService registry,
            TimelineService timeline)
        {
            _context = context;
            _registry = registry;
            _timeline = timeline;
        }

        // GET: api/Timeline/SS:01?limit=100
        // the newest readings of one device, already in time order (oldest first) for a timeline graph
        [HttpGet("{deviceIdentifier}")]
        public ActionResult<TimelineResponse> GetTimeline(string deviceIdentifier, int limit = 100)
        {
            if (!_registry.TryGet(deviceIdentifier, out DeviceState? device) || device == null)
            {
                return NotFound("No device with that identifier is registered");
            }

            limit = Math.Clamp(limit, 1, 500);

            var points = _timeline.GetPoints(device.DeviceIdentifier, limit);

            return Ok(new TimelineResponse(device.DeviceIdentifier, device.Category, points.Count, points));
        }

        // POST: api/Timeline/import
        // loads historical readings for one device, they can be sent in ANY order
        // the SortedDictionary puts them in time order, and they are saved to the database too
        [HttpPost("import")]
        public async Task<ActionResult> Import(TimelineImportRequest request)
        {
            if (!_registry.TryGet(request.DeviceIdentifier, out DeviceState? device) || device == null)
            {
                return NotFound("No device with that identifier is registered");
            }

            if (request.Readings.Count == 0 || request.Readings.Count > 1000)
            {
                return BadRequest("Send between 1 and 1000 readings");
            }

            foreach (var reading in request.Readings)
            {
                DateTime utc = ToUtc(reading.Timestamp);

                _timeline.Add(device.DeviceIdentifier, utc, reading.Value, "Reading");

                _context.SensorReadings.Add(new SensorReading
                {
                    SensorId = device.SensorId,
                    MetricName = "Reading",
                    Value = reading.Value,
                    Timestamp = utc
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                imported = request.Readings.Count,
                earliest = request.Readings.Min(r => ToUtc(r.Timestamp)),
                latest = request.Readings.Max(r => ToUtc(r.Timestamp))
            });
        }

        private static DateTime ToUtc(DateTime time)
        {
            return time.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(time, DateTimeKind.Utc)
                : time.ToUniversalTime();
        }
    }

    // what the timeline endpoint sends back
    public record TimelineResponse(string DeviceIdentifier, string Category, int Count, List<TimelinePoint> Points);

    // request shapes for importing history
    public class TimelineImportReading
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    public class TimelineImportRequest
    {
        public string DeviceIdentifier { get; set; }
        public List<TimelineImportReading> Readings { get; set; }
    }
}