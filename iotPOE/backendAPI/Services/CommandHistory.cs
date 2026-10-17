//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    // one device that was changed by a command, with the state it had BEFORE so the change can be undone
    public record DeviceChange(string DeviceIdentifier, string? PreviousState, string NewState);

    // one command from the operator, a command can change many devices (for example "shut down all valves")
    public record CommandRecord(
        int CommandNumber,
        string Action,
        string Source,
        DateTime Timestamp,
        List<DeviceChange> Changes);

    // keeps the history of manual override commands on a Stack so the last one can be undone
    // registered as a SINGLETON in Program.cs so every request shares the same stack
    public class CommandHistoryService
    {
        // last in, first out: the newest command is on top and is the first one undone (Microsoft Learn, 2026)
        private readonly Stack<CommandRecord> _undoStack = new();

        // many requests can send commands at the same time, so every access is inside the lock
        private readonly object _lock = new();

        private int _commandCounter;

        // puts a new command on top of the stack
        public CommandRecord Push(string action, string source, List<DeviceChange> changes)
        {
            lock (_lock)
            {
                var record = new CommandRecord(++_commandCounter, action, source, DateTime.UtcNow, changes);
                _undoStack.Push(record);
                return record;
            }
        }

        // takes the newest command off the stack, returns false when there is nothing to undo
        public bool TryPop(out CommandRecord? record)
        {
            lock (_lock)
            {
                return _undoStack.TryPop(out record);
            }
        }

        // the stack as a list, newest command first (a Stack lists its top item first)
        public List<CommandRecord> GetStack()
        {
            lock (_lock)
            {
                return _undoStack.ToList();
            }
        }
    }
}

/** 
References
Microsoft Learn, 2026. Stack<T> Class. (Version 10.0) [Source Code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.stack-1> [Accessed 17 October 2026] 
*/