//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    // one search the operator made
    public record SearchEvent(
        DateTime Timestamp,
        string Query,
        string Category,
        string AlertState,
        List<string> ResultIdentifiers);

    // remembers the searches the operator made so the suggestion engine can look for patterns in them later
    // registered as a SINGLETON in Program.cs so every request shares the same history
    public class SearchHistoryService
    {
        // only the newest searches are kept
        private const int MaxEvents = 200;

        // a Queue keeps the oldest search at the front, so when it is full the oldest one is simply dropped (Microsoft Learn, 2026)
        private readonly Queue<SearchEvent> _events = new();

        // requests use the history at the same time, so every access is inside the lock
        private readonly object _lock = new();

        public void Record(string query, string category, string alertState, List<string> resultIdentifiers)
        {
            lock (_lock)
            {
                _events.Enqueue(new SearchEvent(DateTime.UtcNow, query, category, alertState, resultIdentifiers));

                while (_events.Count > MaxEvents)
                {
                    _events.Dequeue();
                }
            }
        }

        // the newest searches first
        public List<SearchEvent> GetRecent(int limit)
        {
            lock (_lock)
            {
                return _events.Reverse().Take(limit).ToList();
            }
        }
    }
}

/** 
References
Microsoft Learn, 2026. Queue<T> Class. (Version 10.0) [Source Code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.queue-1> [Accessed 17 October  2026]
*/