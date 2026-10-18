//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    // what the connectivity endpoint sends back
    public record ConnectivityStatus(List<string> Disconnected, long DuplicatesPrevented, int TimeoutSeconds);

    // keeps the sensors that are disconnected right now in a HashSet
    public class DisconnectTrackerService
    {
        // a sensor that has not reported for this many seconds counts as disconnected
        public const int TimeoutSeconds = 15;

        // a HashSet only holds each item once, so a sensor can only be "disconnected" one time no matter how often it is checked
        // Add() tells us if the sensor was new, and that is what stops a second disconnect alert (Microsoft Learn, 2026)
        private readonly HashSet<string> _disconnected = new(StringComparer.OrdinalIgnoreCase);

        // the watcher and the status endpoint use the set at the same time, so every access is inside the lock
        private readonly object _lock = new();

        private long _duplicatesPrevented;

        // returns true only the FIRST time, so the caller raises one alert and not one every check
        public bool MarkDisconnected(string deviceIdentifier)
        {
            lock (_lock)
            {
                bool isNew = _disconnected.Add(deviceIdentifier);

                if (!isNew)
                {
                    _duplicatesPrevented++;
                }

                return isNew;
            }
        }

        // takes a sensor out of the set when it reports again, returns true if it really was disconnected
        public bool MarkReconnected(string deviceIdentifier)
        {
            lock (_lock)
            {
                return _disconnected.Remove(deviceIdentifier);
            }
        }

        // forgets sensors that were deleted while they were in the set
        public void RemoveMissing(HashSet<string> knownIdentifiers)
        {
            lock (_lock)
            {
                _disconnected.RemoveWhere(id => !knownIdentifiers.Contains(id));
            }
        }

        public ConnectivityStatus GetStatus()
        {
            lock (_lock)
            {
                return new ConnectivityStatus(
                    _disconnected.OrderBy(id => id).ToList(),
                    _duplicatesPrevented,
                    TimeoutSeconds);
            }
        }
    }
}

/** 
References
Microsoft Learn, 2026. HashSet<T> Class. [online] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1> [Accessed 17 October 2026] 
*/