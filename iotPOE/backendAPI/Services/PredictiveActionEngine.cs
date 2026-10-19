using System.Globalization;
using backendAPI.Models;

//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    // Represents a single action recommended by the automated engine (Gami, 2025)
    public record PredictiveAction(string Type, string Title, string Detail, string? DeviceIdentifier, string? Action, int Score);
    public record PatternSummary(string Trigger, string DeviceIdentifier, string State, int Count);

    // Analyzes historical data to predict future anomalies and recommend actions (Gami, 2025)
    public class PredictiveActionEngine
    {
        private const int MinCount = 2;
        private const int ConditionWindowSeconds = 30;
        private const int SearchWindowSeconds = 30;
        private const int PredictedWindowSeconds = 90;

        private readonly DeviceRegistryService _registry;
        private readonly TimelineService _timeline;
        private readonly SearchHistoryService _searchHistory;
        private readonly AnomalyDetectionService _anomalyDetector;

        // Use a Dictionary to store historical patterns and frequency (Microsoft Learn, 2026)
        private readonly Dictionary<(string Trigger, string DeviceIdentifier, string State), int> _patterns = new();
        private readonly object _lock = new();

        public PredictiveActionEngine(
            DeviceRegistryService registry,
            TimelineService timeline,
            SearchHistoryService searchHistory,
            AnomalyDetectionService anomalyDetector)
        {
            _registry = registry;
            _timeline = timeline;
            _searchHistory = searchHistory;
            _anomalyDetector = anomalyDetector;
        }

        public void LearnFromCommand(string deviceIdentifier, string newState)
        {
            var conditions = GetActiveConditions();
            var searchTerms = GetRecentSearchTerms(SearchWindowSeconds, deviceIdentifier);

            lock (_lock)
            {
                foreach (var condition in conditions)
                {
                    CountPattern((condition.Trigger, deviceIdentifier, newState));
                }

                foreach (var term in searchTerms)
                {
                    CountPattern(("search " + term, deviceIdentifier, newState));
                }
            }
        }

        public List<PredictiveAction> GetPredictiveActions(int maxCount = 8)
        {
            var conditions = GetActiveConditions();

            var activeTriggers = new Dictionary<string, int>();

            foreach (var condition in conditions)
            {
                activeTriggers[condition.Trigger] = condition.Severity;
            }

            foreach (var term in GetRecentSearchTerms(ConditionWindowSeconds, null))
            {
                activeTriggers["search " + term] = 0;
            }

            var best = new Dictionary<string, PredictiveAction>(StringComparer.OrdinalIgnoreCase);

            foreach (var pattern in GetPatterns())
            {
                if (pattern.Count < MinCount) continue;

                if (!activeTriggers.TryGetValue(pattern.Trigger, out int severity)) continue;

                if (!_registry.TryGet(pattern.DeviceIdentifier, out DeviceState? device) || device == null || !device.IsActuator) continue;

                if (string.Equals(device.State, pattern.State, StringComparison.OrdinalIgnoreCase)) continue;

                bool turnOn = pattern.State == "on";
                int score = 100 + pattern.Count * 10 + Math.Min(severity, 50);

                var action = new PredictiveAction(
                    "command",
                    (turnOn ? "Turn on " : "Turn off ") + device.DeviceIdentifier,
                    "You did this " + pattern.Count + " time(s) when " + DescribeTrigger(pattern.Trigger),
                    device.DeviceIdentifier,
                    turnOn ? "turn_on" : "turn_off",
                    score);

                string key = device.DeviceIdentifier + " " + pattern.State;

                if (!best.TryGetValue(key, out PredictiveAction? existing) || existing.Score < score)
                {
                    best[key] = action;
                }
            }

            foreach (var condition in conditions)
            {
                best["highlight " + condition.DeviceIdentifier] = new PredictiveAction(
                    condition.Predicted ? "forecast" : "highlight",
                    condition.Title,
                    condition.Detail,
                    condition.DeviceIdentifier,
                    null,
                    condition.Severity);
            }

            // Rank using a PriorityQueue: the highest score gets the lowest priority number (Microsoft Learn, 2026)
            var ranked = new PriorityQueue<PredictiveAction, int>();

            foreach (var action in best.Values)
            {
                ranked.Enqueue(action, -action.Score);
            }

            var result = new List<PredictiveAction>();

            while (result.Count < maxCount && ranked.TryDequeue(out PredictiveAction? next, out _))
            {
                result.Add(next);
            }

            return result;
        }

        public List<PatternSummary> GetPatterns()
        {
            lock (_lock)
            {
                return _patterns
                    .Select(p => new PatternSummary(p.Key.Trigger, p.Key.DeviceIdentifier, p.Key.State, p.Value))
                    .OrderByDescending(p => p.Count)
                    .ThenBy(p => p.Trigger)
                    .ToList();
            }
        }

        private void CountPattern((string Trigger, string DeviceIdentifier, string State) key)
        {
            _patterns[key] = _patterns.TryGetValue(key, out int existing) ? existing + 1 : 1;
        }

        private List<ActiveCondition> GetActiveConditions()
        {
            var conditions = new List<ActiveCondition>();
            DateTime cutoff = DateTime.UtcNow.AddSeconds(-ConditionWindowSeconds);

            foreach (var device in _registry.GetAll())
            {
                if (device.IsActuator) continue;

                if (string.Equals(device.Status, "offline", StringComparison.OrdinalIgnoreCase))
                {
                    conditions.Add(new ActiveCondition(
                        device.DeviceIdentifier + " offline",
                        device.DeviceIdentifier,
                        40,
                        device.DeviceIdentifier + " needs attention",
                        "Sensor is disconnected",
                        false));
                    continue;
                }

                int worstSeverity = 0;
                double worstValue = 0;

                foreach (var point in _timeline.GetPoints(device.DeviceIdentifier, 30))
                {
                    if (point.Timestamp < cutoff) continue;

                    int severity = AlertRules.GetSeverity(device.Category, point.Value);

                    if (severity > worstSeverity)
                    {
                        worstSeverity = severity;
                        worstValue = point.Value;
                    }
                }

                if (worstSeverity > 0)
                {
                    conditions.Add(new ActiveCondition(
                        device.DeviceIdentifier + " critical",
                        device.DeviceIdentifier,
                        worstSeverity,
                        device.DeviceIdentifier + " needs attention",
                        "Critical reading " + worstValue.ToString("F1", CultureInfo.InvariantCulture) +
                        ", " + worstSeverity + "% past the safe limit",
                        false));
                    continue;
                }

                var anomaly = _anomalyDetector.DetectAnomaly(device);

                if (anomaly != null && anomaly.SecondsToBreach <= PredictedWindowSeconds)
                {
                    int rank = Math.Max(1, 90 - anomaly.SecondsToBreach / 2);

                    conditions.Add(new ActiveCondition(
                        device.DeviceIdentifier + " critical",
                        device.DeviceIdentifier,
                        rank,
                        device.DeviceIdentifier + " predicted to reach " +
                        anomaly.Limit.ToString("F1", CultureInfo.InvariantCulture) +
                        " in about " + FormatSeconds(anomaly.SecondsToBreach),
                        (anomaly.ChangePerSecond >= 0 ? "Rising " : "Falling ") +
                        Math.Abs(anomaly.ChangePerSecond).ToString("F2", CultureInfo.InvariantCulture) +
                        " per second (Method: " + anomaly.Method + ")",
                        true));
                }
            }

            return conditions;
        }

        private List<string> GetRecentSearchTerms(int seconds, string? mustHaveFoundDevice)
        {
            DateTime cutoff = DateTime.UtcNow.AddSeconds(-seconds);
            var terms = new List<string>();

            foreach (var searchEvent in _searchHistory.GetRecent(20))
            {
                if (searchEvent.Timestamp < cutoff) continue;

                if (mustHaveFoundDevice != null &&
                    !searchEvent.ResultIdentifiers.Contains(mustHaveFoundDevice, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                string term = DescribeSearch(searchEvent);

                if (!terms.Contains(term))
                {
                    terms.Add(term);
                }
            }

            return terms;
        }

        private static string DescribeSearch(SearchEvent searchEvent)
        {
            if (searchEvent.Query != "") return searchEvent.Query.ToLowerInvariant();
            if (searchEvent.Category != "") return "category:" + searchEvent.Category.ToLowerInvariant();
            return "state:" + searchEvent.AlertState;
        }

        private static string DescribeTrigger(string trigger)
        {
            if (trigger.StartsWith("search ")) return "you searched \"" + trigger.Substring(7) + "\"";
            int lastSpace = trigger.LastIndexOf(' ');
            return trigger.Substring(0, lastSpace) + " was " + trigger.Substring(lastSpace + 1);
        }

        private static string FormatSeconds(int seconds)
        {
            if (seconds < 90) return seconds + " seconds";
            return Math.Round(seconds / 60.0) + " minutes";
        }

        private record ActiveCondition(
            string Trigger,
            string DeviceIdentifier,
            int Severity,
            string Title,
            string Detail,
            bool Predicted);
    }
}

/** 
References
Gami, R., 2025. Building Predictive Analytics Dashboards with ML.NET and Chart.js: Turning Data into Real-Time Insights.(Version 10.0) [Source Code] C# Corner. Available at: <https://www.c-sharpcorner.com/article/building-predictive-analytics-dashboards-with-ml-net-and-chart-js-turning-data> [Accessed 16 October 2026]
Microsoft Learn, 2021. Tutorial: Detect anomalies in time series with ML.NET. (Version 10.0) [Source Code] Available at: <https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/phone-calls-anomaly-detection> [Accessed 16 October 2026]
Microsoft Learn, 2026. Dictionary<TKey,TValue> Class and PriorityQueue<TElement,TPriority> Class. (Version 10.0) [Source Code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2> [Accessed 16 October 2026]
*/