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
    public class DevicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DeviceRegistryService _registry;
        private readonly SearchHistoryService _searchHistory;

        public DevicesController(
            ApplicationDbContext context,
            DeviceRegistryService registry,
            SearchHistoryService searchHistory)
        {
            _context = context;
            _registry = registry;
            _searchHistory = searchHistory;
        }

        // GET: api/Devices
        // every live device straight from the in-memory registry (no database query)
        [HttpGet]
        public ActionResult<IEnumerable<DeviceState>> GetAll()
        {
            return Ok(_registry.GetAll());
        }

        // GET: api/Devices/search?query=hvac&category=Actuator&alertState=alerting&track=true
        // finds devices by part of their identifier or category (query), by exact category,
        // and by alert state: alerting (has an unread alert), offline (disconnected) or normal
        // track=true records the search in the search history, the page's automatic refresh sends track=false
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<DeviceSearchItem>>> Search(
            string? query,
            string? category,
            string? alertState,
            bool track = false)
        {
            string text = (query ?? "").Trim();
            string categoryFilter = (category ?? "").Trim();
            string stateFilter = (alertState ?? "").Trim().ToLowerInvariant();

            if (stateFilter != "" && stateFilter != "alerting" && stateFilter != "offline" && stateFilter != "normal")
            {
                return BadRequest("alertState must be alerting, offline or normal");
            }

            // the sensors that have an unread alert, kept in a HashSet so each check below is O(1)
            var alertingSensorIds = (await _context.Alerts
                .AsNoTracking()
                .Where(a => a.Status == "unread")
                .Select(a => a.SensorId)
                .Distinct()
                .ToListAsync()).ToHashSet();

            var items = new List<DeviceSearchItem>();

            foreach (var device in _registry.GetAll())
            {
                // the text can match any part of the identifier or the category, upper or lower case
                if (text != ""
                    && !device.DeviceIdentifier.Contains(text, StringComparison.OrdinalIgnoreCase)
                    && !device.Category.Contains(text, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (categoryFilter != "" && !device.Category.Equals(categoryFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string deviceAlertState = GetAlertState(device, alertingSensorIds);

                if (stateFilter != "" && deviceAlertState != stateFilter)
                {
                    continue;
                }

                items.Add(new DeviceSearchItem(
                    device.SensorId,
                    device.DeviceIdentifier,
                    device.Category,
                    device.Status,
                    device.State,
                    device.LastReading,
                    device.LastValue,
                    device.LastSeenAt,
                    device.IsActuator,
                    deviceAlertState));
            }

            items = items.OrderBy(i => i.DeviceIdentifier).ToList();

            // only searches the operator starts are recorded
            if (track && (text != "" || categoryFilter != "" || stateFilter != ""))
            {
                _searchHistory.Record(text, categoryFilter, stateFilter, items.Select(i => i.DeviceIdentifier).ToList());
            }

            return Ok(items);
        }

        // GET: api/Devices/search-history?limit=20
        // the newest recorded searches first
        [HttpGet("search-history")]
        public ActionResult<IEnumerable<SearchEvent>> GetSearchHistory(int limit = 20)
        {
            limit = Math.Clamp(limit, 1, 100);
            return Ok(_searchHistory.GetRecent(limit));
        }

        // GET: api/Devices/SS:01
        // one device by its identifier, O(1) Dictionary lookup
        [HttpGet("{deviceIdentifier}")]
        public ActionResult<DeviceState> GetByIdentifier(string deviceIdentifier)
        {
            if (!_registry.TryGet(deviceIdentifier, out DeviceState? device) || device == null)
            {
                return NotFound("No device with that identifier is registered");
            }

            return Ok(device);
        }

        // offline comes first, then alerting, otherwise normal
        private static string GetAlertState(DeviceState device, HashSet<int> alertingSensorIds)
        {
            if (string.Equals(device.Status, "offline", StringComparison.OrdinalIgnoreCase))
            {
                return "offline";
            }

            return alertingSensorIds.Contains(device.SensorId) ? "alerting" : "normal";
        }
    }

    // one search result: the live device plus its alert state
    public record DeviceSearchItem(
        int SensorId,
        string DeviceIdentifier,
        string Category,
        string Status,
        string? State,
        string? LastReading,
        double? LastValue,
        DateTime? LastSeenAt,
        bool IsActuator,
        string AlertState);
}