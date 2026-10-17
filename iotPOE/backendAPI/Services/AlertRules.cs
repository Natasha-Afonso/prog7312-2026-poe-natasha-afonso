using backendAPI.Models;

//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    // the safe limit for a category and which direction is dangerous
    // AlertWhenAbove = true means too HIGH is bad, false means too LOW is bad
    public record AlertRule(double Limit, bool AlertWhenAbove);

    public static class AlertRules
    {
        // lookup by category name, a Dictionary again (Microsoft Learn, 2026)
        // Occupancy and Actuator have no rule on purpose, they never raise a threshold alert
        private static readonly Dictionary<string, AlertRule> Rules = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Environmental", new AlertRule(30.0, true) },  // server room above 30 degrees C is overheating
            { "Power", new AlertRule(300.0, true) },         // a block using more than 300 kWh is an energy spike
            { "Water", new AlertRule(40.0, true) },          // flow above 40 L/min suggests a burst pipe
            { "Temperature", new AlertRule(35.0, true) },    // generic categories for sensors added from the register form
            { "Humidity", new AlertRule(70.0, true) }
        };

        public static bool TryGetRule(string category, out AlertRule? rule)
        {
            return Rules.TryGetValue(category, out rule);
        }

        // 0 = normal reading, 1 or more = critical, and the bigger the number the further past the limit it is
        // this number becomes the priority in the PriorityQueue
        public static int GetSeverity(string category, double value)
        {
            if (!TryGetRule(category, out AlertRule? rule) || rule == null)
            {
                return 0;
            }

            var reading = new SensorReading { MetricName = "Reading", Value = value };
            var limit = new SensorReading { MetricName = "Threshold", Value = rule.Limit };

            // uses the overloaded ">" and "<" operators from SensorReading
            bool critical = rule.AlertWhenAbove ? reading > limit : reading < limit;

            if (!critical)
            {
                return 0;
            }

            // how far past the limit as a percentage, at least 1
            double percentPast = Math.Abs(value - rule.Limit) / rule.Limit * 100;
            return Math.Max(1, (int)Math.Round(percentPast));
        }
    }
}


/** 
References
Microsoft Learn, 2026. Dictionary<TKey,TValue> Class. (Version 10.0) [Source Code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2> [Accessed 17 October 2026] 
*/