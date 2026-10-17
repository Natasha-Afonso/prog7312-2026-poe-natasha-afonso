using System.ComponentModel.DataAnnotations;

//Emeris School of Computer Science, 2025
namespace backendAPI.Models
{
    // one row for every command sent to a device, this is the saved command history
    public class CommandLog
    {
        [Key]
        public int Id { get; set; }

        public string DeviceIdentifier { get; set; }
        public string Action { get; set; }

        // what the device was before and after the command, so undo can restore it
        public string? PreviousState { get; set; }
        public string? NewState { get; set; }

        public string Source { get; set; } = "manual"; // manual, undo, automated

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}