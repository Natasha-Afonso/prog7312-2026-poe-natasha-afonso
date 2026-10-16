using System.ComponentModel.DataAnnotations;

//Emeris School of Computer Science, 2025
namespace backendAPI.Models
{
    public class Alert
    {
        [Key]
        public int Id { get; set; }

        public int SensorId { get; set; }
        public Sensor? Sensor { get; set; }

        public string Title { get; set; }
        public string Message { get; set; }
        public string Metric { get; set; }
        public string Value { get; set; }
        public string Threshold { get; set; }

        public string Status { get; set; } = "unread"; // unread, acknowledged, resolved

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}