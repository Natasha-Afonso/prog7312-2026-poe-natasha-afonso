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
/** 
References
Emeris School of Computer Science. 2025. PROG7311 Docker part 1 - Getting started. (Version 10.0) [Source Code]. Available at: <https://youtu.be/ldfC-rTzHPY?si=znsmPz-ZSPaY-4Ii> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 2- connecting between the two!. (Version 10.0) [Source Code]. Available at: <https://youtu.be/_0EhC3WauAc?si=nEazkCpBTV1YpHBp> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 3- databases in docker. (Version 10.0) [Source Code]. Available at: <https://youtu.be/6tYEbngWY-E?si=10rSEEpfSNL5Kg8k> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 4- connecting to database from the API.(Version 10.0) [Source Code]. Available at: <https://youtu.be/_PwpPMoeqzw?si=Q0ELlPZdN42dFU9q> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 5- connecting them all together- DB -API -MVC. (Version 10.0) [Source Code]. Available at: <https://youtu.be/YlIajRMdLPk?si=D75XZKO-L6nQhR-3> [Accessed 5 June 2026]
*/