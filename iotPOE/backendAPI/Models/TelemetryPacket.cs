//Emeris School of Computer Science, 2025
namespace backendAPI.Models
{
  
    public class TelemetryPacket<T>
    {
        public string SensorId { get; set; }
        public string MetricName { get; set; }
        public T Value { get; set; }
        public DateTime Timestamp { get; set; }
// (Microsoft Learn, 2025)
        public TelemetryPacket(string sensorId, string metricName, T value)
        {
            SensorId = sensorId;
            MetricName = metricName;
            Value = value;
            Timestamp = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"[{SensorId}] {MetricName}: {Value}";
        }
    }
}

/** 
References
Microsoft Learn, 2025. Constraints on type parameters (C# Programming Guide). [online] Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters> [Accessed 12 September 2026].
*/