using System.Globalization;
using backendAPI.Models;

//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    // keeps every registered device in memory so a packet can be matched to its device without a database query
    // registered as a SINGLETON in Program.cs, so there is one registry shared by every request
    public class DeviceRegistryService
    {
        // key = device identifier (MAC style id like "SS:01"), case does not matter so "ss:01" and "SS:01" are the same key
        // lookups on a Dictionary are O(1) (Microsoft Learn, 2026)
        private readonly Dictionary<string, DeviceState> _devices = new(StringComparer.OrdinalIgnoreCase);

        // many requests use the registry at the same time, so every access is inside the lock
        private readonly object _lock = new();

        // fills the registry from the database, called once when the API starts
        public void Load(IEnumerable<Sensor> sensors)
        {
            lock (_lock)
            {
                _devices.Clear();

                foreach (var sensor in sensors)
                {
                    _devices[sensor.DeviceIdentifier] = ToDeviceState(sensor);
                }
            }
        }

        // add a new device or replace the details of an existing one
        public void Upsert(Sensor sensor)
        {
            lock (_lock)
            {
                _devices[sensor.DeviceIdentifier] = ToDeviceState(sensor);
            }
        }

        public void Remove(string deviceIdentifier)
        {
            lock (_lock)
            {
                _devices.Remove(deviceIdentifier);
            }
        }

        // the O(1) lookup that runs every time a telemetry packet arrives
        public bool TryGet(string deviceIdentifier, out DeviceState? device)
        {
            lock (_lock)
            {
                return _devices.TryGetValue(deviceIdentifier, out device);
            }
        }

        // store the newest reading and the time the device was last heard from
        public void RecordReading(string deviceIdentifier, double value, DateTime seenAt)
        {
            lock (_lock)
            {
                if (_devices.TryGetValue(deviceIdentifier, out var existing))
                {
                    _devices[deviceIdentifier] = existing with
                    {
                        LastValue = value,
                        LastReading = value.ToString("F1", CultureInfo.InvariantCulture),
                        LastSeenAt = seenAt
                    };
                }
            }
        }

        // sets the on/off state of an actuator and hands back what it was BEFORE,
        // the old state is what the undo stack needs to put the device back
        public bool TrySetState(string deviceIdentifier, string newState, out string? previousState)
        {
            lock (_lock)
            {
                if (_devices.TryGetValue(deviceIdentifier, out var existing))
                {
                    previousState = existing.State;
                    _devices[deviceIdentifier] = existing with { State = newState };
                    return true;
                }

                previousState = null;
                return false;
            }
        }

        public List<DeviceState> GetAll()
        {
            lock (_lock)
            {
                return _devices.Values.ToList();
            }
        }

        private static DeviceState ToDeviceState(Sensor sensor)
        {
            return new DeviceState(
                sensor.Id,
                sensor.DeviceIdentifier,
                sensor.Category,
                sensor.Status,
                sensor.State,
                sensor.LastReading,
                null,
                sensor.LastSeenAt);
        }
    }
}

/** 
References
Microsoft Learn, 2026. Dictionary<TKey,TValue> Class. (Version 10.0) [Source Code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2> [Accessed 16 October 2026]
*/