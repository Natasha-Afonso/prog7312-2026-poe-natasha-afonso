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
    public class CommandsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DeviceRegistryService _registry;
        private readonly CommandHistoryService _history;

        public CommandsController(
            ApplicationDbContext context,
            DeviceRegistryService registry,
            CommandHistoryService history)
        {
            _context = context;
            _registry = registry;
            _history = history;
        }

        // POST: api/Commands
        // sends a manual override to one actuator, action is turn_on, turn_off or toggle
        [HttpPost]
        public async Task<ActionResult<CommandRecord>> Send(CommandRequest request)
        {
            if (!_registry.TryGet(request.DeviceIdentifier, out DeviceState? device) || device == null)
            {
                return NotFound("No device with that identifier is registered");
            }

            if (!device.IsActuator)
            {
                return BadRequest("Only actuators (pumps, relays, valves) accept commands");
            }

            string? newState = ResolveNewState(request.Action, device.State);

            if (newState == null)
            {
                return BadRequest("Action must be turn_on, turn_off or toggle");
            }

            if (string.Equals(device.State, newState, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(device.DeviceIdentifier + " is already " + newState);
            }

            var change = await ApplyStateAsync(device, newState);

            // the command goes on top of the undo stack together with the state the device had before
            var record = _history.Push(request.Action.ToLowerInvariant(), "manual", new List<DeviceChange> { change });
            await SaveToHistoryAsync(record);

            return Ok(record);
        }

        // POST: api/Commands/shutdown-all
        // turns off every actuator that is on, as ONE command so a single undo puts them all back
        [HttpPost("shutdown-all")]
        public async Task<ActionResult<CommandRecord>> ShutdownAll()
        {
            var running = _registry.GetAll()
                .Where(d => d.IsActuator && string.Equals(d.State, "on", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (running.Count == 0)
            {
                return BadRequest("No actuators are on");
            }

            var changes = new List<DeviceChange>();

            foreach (var device in running)
            {
                changes.Add(await ApplyStateAsync(device, "off"));
            }

            var record = _history.Push("shutdown_all", "manual", changes);
            await SaveToHistoryAsync(record);

            return Ok(record);
        }

        // POST: api/Commands/undo
        // takes the newest command off the stack and restores every device to the state it had BEFORE that command
        [HttpPost("undo")]
        public async Task<ActionResult<CommandRecord>> Undo()
        {
            if (!_history.TryPop(out CommandRecord? last) || last == null)
            {
                return NotFound("Nothing to undo");
            }

            var restored = new List<DeviceChange>();

            // newest change first, so a command that touched a device twice unwinds in the right order
            foreach (var change in Enumerable.Reverse(last.Changes))
            {
                // skip a device that was deleted after the command was sent
                if (!_registry.TryGet(change.DeviceIdentifier, out DeviceState? device) || device == null)
                {
                    continue;
                }

                string target = change.PreviousState ?? "off";
                restored.Add(await ApplyStateAsync(device, target));
            }

            // the undo is saved in the history but is NOT pushed on the stack (there is no redo)
            var undoRecord = new CommandRecord(last.CommandNumber, "undo_" + last.Action, "undo", DateTime.UtcNow, restored);
            await SaveToHistoryAsync(undoRecord);

            return Ok(undoRecord);
        }

        // GET: api/Commands/stack
        // what is on the undo stack right now, newest command first
        [HttpGet("stack")]
        public ActionResult<IEnumerable<CommandRecord>> GetStack()
        {
            return Ok(_history.GetStack());
        }

        // GET: api/Commands/history?limit=50
        // the saved command history from the database, newest first
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<CommandLog>>> GetHistory(int limit = 50)
        {
            limit = Math.Clamp(limit, 1, 200);

            var logs = await _context.CommandLogs
                .AsNoTracking()
                .OrderByDescending(c => c.Timestamp)
                .ThenByDescending(c => c.Id)
                .Take(limit)
                .ToListAsync();

            return logs;
        }

        // turns an action into the new on/off state, returns null for an action we do not know
        private static string? ResolveNewState(string action, string? currentState)
        {
            switch (action.ToLowerInvariant())
            {
                case "turn_on":
                    return "on";
                case "turn_off":
                    return "off";
                case "toggle":
                    return string.Equals(currentState, "on", StringComparison.OrdinalIgnoreCase) ? "off" : "on";
                default:
                    return null;
            }
        }

        // changes the state in the registry (instant) and in the database (so it survives a restart)
        // returns the change with the state the device had before
        private async Task<DeviceChange> ApplyStateAsync(DeviceState device, string newState)
        {
            _registry.TrySetState(device.DeviceIdentifier, newState, out string? previousState);

            // ExecuteUpdateAsync changes the row directly without loading it first (Microsoft Learn, 2026)
            await _context.Sensors
                .Where(s => s.Id == device.SensorId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, newState));

            return new DeviceChange(device.DeviceIdentifier, previousState, newState);
        }

        // one row per device that changed, this is the saved command history
        private async Task SaveToHistoryAsync(CommandRecord record)
        {
            foreach (var change in record.Changes)
            {
                _context.CommandLogs.Add(new CommandLog
                {
                    DeviceIdentifier = change.DeviceIdentifier,
                    Action = record.Action,
                    PreviousState = change.PreviousState,
                    NewState = change.NewState,
                    Source = record.Source,
                    Timestamp = record.Timestamp
                });
            }

            await _context.SaveChangesAsync();
        }
    }

    // request shape for sending a command to one device
    public class CommandRequest
    {
        public string DeviceIdentifier { get; set; }
        public string Action { get; set; }
    }
}

/** 
References
Microsoft Learn, 2026. ExecuteUpdate and ExecuteDelete (EF Core). [online] Available at: <https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete> [Accessed 17 October 2026] 
**/