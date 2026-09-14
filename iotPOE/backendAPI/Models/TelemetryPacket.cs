//Emeris School of Computer Science, 2025
namespace backendAPI.Models
{
    //Generics: a reusable wrapper that can hold a float for temperature,
    //an int for power metrics, or a bool for smart switch triggers,
    //without boxing/unboxing overhead
    public class TelemetryPacket<T>
    {
        public string SensorId { get; set; }
        public string MetricName { get; set; }
        public T Value { get; set; }
        public DateTime Timestamp { get; set; }

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