using backendAPI.Models;

//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    // one point on a timeline graph
    public record TimelinePoint(DateTime Timestamp, double Value, string MetricName);

    // keeps the history of readings for every device in time order, ready for the frontend to draw a timeline graph
    // registered as a SINGLETON in Program.cs so every request shares the same timelines
    public class TimelineService
    {
        // the oldest points are dropped after this many so memory stays small
        private const int MaxPointsPerDevice = 500;

        // device identifier -> that device's timeline
        // a SortedDictionary keeps its keys in order as items are added, so readings that arrive out of order
        // still end up sorted by timestamp and nothing has to be sorted when the graph is requested (Microsoft Learn, 2026)
        // the key is the timestamp PLUS a sequence number, because a SortedDictionary throws if the same key is added twice
        private readonly Dictionary<string, SortedDictionary<(DateTime Timestamp, long Sequence), TimelinePoint>> _timelines
            = new(StringComparer.OrdinalIgnoreCase);

        // many requests and the worker use the timelines at the same time, so every access is inside the lock
        private readonly object _lock = new();

        private long _sequence;

        // adds one reading to a device's timeline, O(log n)
        public void Add(string deviceIdentifier, DateTime timestamp, double value, string metricName)
        {
            // database times come back without a kind, treat them as UTC like everything else
            DateTime utc = timestamp.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
                : timestamp.ToUniversalTime();

            lock (_lock)
            {
                if (!_timelines.TryGetValue(deviceIdentifier, out var timeline))
                {
                    timeline = new SortedDictionary<(DateTime Timestamp, long Sequence), TimelinePoint>();
                    _timelines[deviceIdentifier] = timeline;
                }

                timeline[(utc, ++_sequence)] = new TimelinePoint(utc, value, metricName);

                // drop the oldest point (the first key in the sorted order) when the timeline is too long
                while (timeline.Count > MaxPointsPerDevice)
                {
                    timeline.Remove(timeline.Keys.First());
                }
            }
        }

        // the newest "limit" points of a device, oldest first, so a graph can draw them left to right
        public List<TimelinePoint> GetPoints(string deviceIdentifier, int limit)
        {
            lock (_lock)
            {
                if (!_timelines.TryGetValue(deviceIdentifier, out var timeline))
                {
                    return new List<TimelinePoint>();
                }

                int skip = Math.Max(0, timeline.Count - limit);
                return timeline.Values.Skip(skip).ToList();
            }
        }

        // rebuilds the timelines from saved readings, called once when the API starts
        public void LoadFromReadings(IEnumerable<SensorReading> readings, IEnumerable<DeviceState> devices)
        {
            // the saved readings only know the database id of the sensor, so map that back to the device identifier
            var idToIdentifier = devices.ToDictionary(d => d.SensorId, d => d.DeviceIdentifier);

            foreach (var reading in readings)
            {
                if (idToIdentifier.TryGetValue(reading.SensorId, out string? identifier))
                {
                    Add(identifier, reading.Timestamp, reading.Value, reading.MetricName);
                }
            }
        }
    }
}

/** 
References
Microsoft Learn, 2026. SortedDictionary<TKey,TValue> Class. [online] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sorteddictionary-2> [Accessed 17 October 2026] 
*/