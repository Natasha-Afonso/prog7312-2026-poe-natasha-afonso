using backendAPI.Data;
using backendAPI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    // background service that looks at every sensor's "last seen" time every 2 seconds
    // a silent sensor is added to the HashSet and ONE alert is raised, a sensor that reports again is taken out
    // same hosted service pattern as TelemetryQueueWorker (Microsoft Learn, 2026)
    public class DisconnectWatcher : BackgroundService
    {
        private readonly DeviceRegistryService _registry;
        private readonly DisconnectTrackerService _tracker;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DisconnectWatcher> _logger;

        public DisconnectWatcher(
            DeviceRegistryService registry,
            DisconnectTrackerService tracker,
            IServiceScopeFactory scopeFactory,
            ILogger<DisconnectWatcher> logger)
        {
            _registry = registry;
            _tracker = tracker;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // the time before the API started does not count, otherwise every sensor would look silent at startup
            DateTime startedAt = DateTime.UtcNow;

            _logger.LogInformation("Disconnect watcher started, timeout is {Seconds} seconds", DisconnectTrackerService.TimeoutSeconds);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        await CheckDevicesAsync(startedAt);
                    }
                    catch (Exception ex)
                    {
                        // one bad round must not kill the watcher
                        _logger.LogError(ex, "Error while checking for disconnected sensors");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Disconnect watcher is stopping");
            }
        }

        private async Task CheckDevicesAsync(DateTime startedAt)
        {
            DateTime now = DateTime.UtcNow;

            var knownIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newlyOffline = new List<DeviceState>();

            foreach (var device in _registry.GetAll())
            {
                knownIdentifiers.Add(device.DeviceIdentifier);

                // actuators do not send readings, so only sensors can go silent
                if (device.IsActuator || device.LastSeenAt == null)
                {
                    continue;
                }

                DateTime lastSeen = device.LastSeenAt.Value > startedAt ? device.LastSeenAt.Value : startedAt;
                bool silent = (now - lastSeen).TotalSeconds > DisconnectTrackerService.TimeoutSeconds;

                if (silent)
                {
                    // MarkDisconnected is true only the first time, so this runs once per disconnect
                    if (_tracker.MarkDisconnected(device.DeviceIdentifier))
                    {
                        _registry.SetStatus(device.DeviceIdentifier, "offline");
                        newlyOffline.Add(device);
                        _logger.LogWarning("Sensor {Id} is disconnected", device.DeviceIdentifier);
                    }
                }
                else if (_tracker.MarkReconnected(device.DeviceIdentifier))
                {
                    _registry.SetStatus(device.DeviceIdentifier, "active");
                    _logger.LogInformation("Sensor {Id} is back online", device.DeviceIdentifier);
                }
            }

            _tracker.RemoveMissing(knownIdentifiers);

            if (newlyOffline.Count == 0)
            {
                return;
            }

            // one alert for every sensor that just went offline
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            foreach (var device in newlyOffline)
            {
                context.Alerts.Add(new Alert
                {
                    SensorId = device.SensorId,
                    Title = "SENSOR OFFLINE",
                    Message = "Sensor " + device.DeviceIdentifier + " has not reported for more than " +
                              DisconnectTrackerService.TimeoutSeconds + " seconds.",
                    Metric = "Connectivity",
                    Value = "offline",
                    Threshold = DisconnectTrackerService.TimeoutSeconds + " s",
                    Status = "unread",
                    Timestamp = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }
    }
}

/** 
References
Microsoft Learn, 2026. Background tasks with hosted services in ASP.NET Core. [online] Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services> [Accessed 17 October 2026] 
*/