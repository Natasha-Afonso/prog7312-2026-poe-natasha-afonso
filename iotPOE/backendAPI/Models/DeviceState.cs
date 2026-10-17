//Emeris School of Computer Science, 2025
namespace backendAPI.Models
{
    // the live copy of a device that is kept in memory by the DeviceRegistryService
    // it is a record so a change makes a new copy (using "with"), which keeps readers safe while a packet is being processed
    public record DeviceState(
        int SensorId,
        string DeviceIdentifier,
        string Category,
        string Status,
        string? State,
        string? LastReading,
        double? LastValue,
        DateTime? LastSeenAt)
    {
        // actuators (pump, relay) can be switched on and off, sensors only send readings
        public bool IsActuator => Category.Equals("Actuator", StringComparison.OrdinalIgnoreCase);
    }
}