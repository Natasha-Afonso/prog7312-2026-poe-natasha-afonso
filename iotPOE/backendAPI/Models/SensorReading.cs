using System.ComponentModel.DataAnnotations;

//Emeris School of Computer Science, 2025
namespace backendAPI.Models
{
    public class SensorReading
    {
        [Key]
        public int Id { get; set; }

        public int SensorId { get; set; }
        public Sensor? Sensor { get; set; }

        public string MetricName { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        //operator "+" : combining two readings (e.g. average load of two power sensors)
        public static SensorReading operator +(SensorReading a, SensorReading b)
        {
            return new SensorReading
            {
                SensorId = a.SensorId,
                MetricName = $"{a.MetricName} + {b.MetricName} Combined",
                Value = a.Value + b.Value,
                Timestamp = DateTime.UtcNow
            };
        }

        //operator "-" : finding the delta between a reading and a threshold
        public static SensorReading operator -(SensorReading a, SensorReading b)
        {
            return new SensorReading
            {
                SensorId = a.SensorId,
                MetricName = $"Delta between {a.MetricName} and {b.MetricName}",
                Value = a.Value - b.Value,
                Timestamp = DateTime.UtcNow
            };
        }

        //using the comparison operators
        // > and 
        public static bool operator >(SensorReading a, SensorReading b)
        {
            return a.Value > b.Value;
        }

        public static bool operator <(SensorReading a, SensorReading b)
        {
            return a.Value < b.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SensorId, MetricName, Value);
        }

        public override string ToString()
        {
            return $"[Sensor {SensorId}] {MetricName}: {Value:F2}";
        }
    }
}