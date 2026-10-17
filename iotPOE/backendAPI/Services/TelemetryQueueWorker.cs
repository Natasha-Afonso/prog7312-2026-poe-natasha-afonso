using backendAPI.Data;
using backendAPI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    // background service that empties the standard (FIFO) queue and saves the readings to the database
    // pattern: hosted service + a scope per round, because a hosted service is a singleton and cannot take the
    // scoped DbContext in its constructor (Microsoft Learn, 2026) (LearnSmartCoding, YYYY)
    public class TelemetryQueueWorker : BackgroundService
    {
        private readonly TelemetryQueueService _queues;
        private readonly DeviceRegistryService _registry;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TelemetryQueueWorker> _logger;

        // save this many readings in one database round trip
        private const int BatchSize = 50;

        public TelemetryQueueWorker(
            TelemetryQueueService queues,
            DeviceRegistryService registry,
            IServiceScopeFactory scopeFactory,
            ILogger<TelemetryQueueWorker> logger)
        {
            _queues = queues;
            _registry = registry;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Telemetry queue worker started");

            // wakes up four times a second (PeriodicTimer example from Microsoft Learn, 2026)
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        await DrainNormalQueueAsync();
                    }
                    catch (Exception ex)
                    {
                        // one bad round must not kill the worker
                        _logger.LogError(ex, "Error while saving queued telemetry");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Telemetry queue worker is stopping");
            }
        }

        private async Task DrainNormalQueueAsync()
        {
            while (true)
            {
                var batch = _queues.DequeueNormalBatch(BatchSize);

                if (batch.Count == 0)
                {
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                int saved = 0;

                foreach (var packet in batch)
                {
                    // O(1) lookup, skips readings for a device that was deleted while it waited in the queue
                    if (_registry.TryGet(packet.SensorId, out DeviceState? device) && device != null)
                    {
                        context.SensorReadings.Add(new SensorReading
                        {
                            SensorId = device.SensorId,
                            MetricName = packet.MetricName,
                            Value = packet.Value,
                            Timestamp = packet.Timestamp
                        });

                        saved++;
                    }
                }

                await context.SaveChangesAsync();

                _queues.RecordNormalProcessed(saved);
                _logger.LogInformation("Queue worker saved {Count} standard reading(s)", saved);
            }
        }
    }
}

/** 
References
LearnSmartCoding, 2026. NotificationBackgroundService.cs (smartcertify-api-clean-architecture-dotnet9). (Version 10.0) [Source Code]] Available at: <https://github.com/learnsmartcoding/smartcertify-api-clean-architecture-dotnet9/blob/main/LSC.SmartCertify.Infrastructure/BackgroundServices/NotificationBackgroundService.cs> [Accessed 16 October 2026] 
*/