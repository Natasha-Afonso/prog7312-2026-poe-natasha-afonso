using backendAPI.Models;

//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    // the safe limit for a category and which direction is dangerous
    // AlertWhenAbove = true means too HIGH is bad (temperature), false means too LOW is bad (moisture)
    public record AlertRule(double Limit, bool AlertWhenAbove);

    public static class AlertRules
    {
        // lookup by category name, a Dictionary again (Microsoft Learn, 2026)
        private static readonly Dictionary<string, AlertRule> Rules = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Temperature", new AlertRule(35.0, true) },
            { "Environmental", new AlertRule(35.0, true) },
            { "Power", new AlertRule(80.0, true) },
            { "Power Consumption", new AlertRule(80.0, true) },
            { "Pressure", new AlertRule(150.0, true) },
            { "Humidity", new AlertRule(70.0, true) },
            { "Moisture", new AlertRule(20.0, false) } // a moisture drop below 20% is critical
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