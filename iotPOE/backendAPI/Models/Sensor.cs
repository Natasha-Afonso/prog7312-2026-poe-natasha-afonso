using System.ComponentModel.DataAnnotations;

//Emeris School of Computer Science, 2025
namespace backendAPI.Models
{
    public class Sensor
    {
        [Key]
        public int Id { get; set; }

        public string DeviceIdentifier { get; set; }
        public string Category { get; set; }

        public int DeploymentNodeId { get; set; }
        public DeploymentNode? DeploymentNode { get; set; }

        public string Status { get; set; }
        public string? LastReading { get; set; }

        public string? FilePath {get; set;}
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }
}