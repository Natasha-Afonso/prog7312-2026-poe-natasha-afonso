using backendAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace backendAPI.Services
{
    // Background service to simulate live IoT telemetry (Huan, 2025)
    public class SeedDataBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<SeedDataBackgroundService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly Random _random = new Random();
        
        private static readonly TimeSpan PushInterval = TimeSpan.FromSeconds(5); 

        private List<SeedSensorConfig> _seedConfigs = new();
        
        // Keeps track of the last value to simulate a realistic "drifting" sensor wave
        private Dictionary<string, double> _lastValues = new();

        public SeedDataBackgroundService(
            IServiceScopeFactory serviceScopeFactory, 
            ILogger<SeedDataBackgroundService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Parsing JSON seed data (Home Assistant Community, 2025)
            await LoadSeedSensorsAsync();

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("http://localhost:8080"); 

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Creating a scoped DbContext within a Singleton BackgroundService (LearnSmartCoding, 2025)
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        var allSensors = await dbContext.Sensors.ToListAsync(stoppingToken);
                        if (allSensors.Count == 0) 
                        { 
                            await Task.Delay(PushInterval, stoppingToken); 
                            continue; 
                        }

                        // Keep Actuators alive
                        var actuators = allSensors.Where(s => s.Category == "Actuator").ToList();
                        foreach (var act in actuators)
                        {
                            act.LastSeenAt = DateTime.UtcNow;
                        }
                        await dbContext.SaveChangesAsync(stoppingToken);

                        // Pick 1 or 2 random sensors to send data for
                        var telemetrySensors = allSensors.Where(s => s.Category != "Actuator").ToList();
                        var batchSensors = telemetrySensors.OrderBy(x => _random.Next()).Take(_random.Next(1, 3)).ToList();

                        if (batchSensors.Count > 0)
                        {
                            string[] sensorIds = new string[batchSensors.Count];
                            double[][] rawBatches = new double[batchSensors.Count][];

                            for (int i = 0; i < batchSensors.Count; i++)
                            {
                                var sensor = batchSensors[i];
                                // Using dynamic ranges to validate simulated boundaries (StackOverflow, 2024)
                                var config = _seedConfigs.FirstOrDefault(c => c.DeviceIdentifier == sensor.DeviceIdentifier);
                                
                                double min = config?.MinValue ?? 10.0;
                                double max = config?.MaxValue ?? 100.0;
                                
                                if (!_lastValues.ContainsKey(sensor.DeviceIdentifier))
                                {
                                    _lastValues[sensor.DeviceIdentifier] = (min + max) / 2; 
                                }

                                double current = _lastValues[sensor.DeviceIdentifier];
                                
                                double range = max - min;
                                double drift = (_random.NextDouble() - 0.5) * (range * 0.10);
                                
                                double nextValue = current + drift;
                                
                                nextValue = Math.Max(min, Math.Min(max, nextValue));
                                nextValue = Math.Round(nextValue, 2);

                                _lastValues[sensor.DeviceIdentifier] = nextValue;

                                sensorIds[i] = sensor.DeviceIdentifier;
                                rawBatches[i] = new double[] { nextValue }; 
                            }

                            var payload = new 
                            {
                                SensorIds = sensorIds,
                                RawBatches = rawBatches
                            };

                            var response = await client.PostAsJsonAsync("/api/Telemetry/ingest-batch", payload, stoppingToken);
                            
                            if (response.IsSuccessStatusCode)
                            {
                                // Only log new data (StackOverflow, 2012)
                                _logger.LogInformation($"[SIMULATOR] Successfully pushed {batchSensors.Count} readings to the API.");
                            }
                            else
                            {
                                _logger.LogWarning($"[SIMULATOR] API rejected data: {response.StatusCode}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in telemetry data background service");
                }

                await Task.Delay(PushInterval, stoppingToken);
            }
        }

        private async Task LoadSeedSensorsAsync()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "SeedData", "TelemetryData.json");
            if (!File.Exists(path))
            {
                _logger.LogError($"TelemetryData.json not found at {path}");
                return;
            }

            string json = await File.ReadAllTextAsync(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<SeedDataRoot>(json, options);
            
            if (data?.Sensors != null)
            {
                _seedConfigs = data.Sensors;
                _logger.LogInformation($"Loaded {_seedConfigs.Count} sensor configurations.");
            }
        }

        private class SeedDataRoot
        {
            public List<SeedSensorConfig> Sensors { get; set; } = new();
        }

        private class SeedSensorConfig
        {
            public string DeviceIdentifier { get; set; } = "";
            public double MinValue { get; set; }
            public double MaxValue { get; set; }
        }
    }
}

/** 
References
Huan, J., L., 2025. Background tasks with hosted services in ASP.NET Core. (Version 10.0) [Source Code].Available at: < https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0&tabs=visual-studio > [Accessed 12 September 2026]
Home Assistant Community, 2025. Webhook, JSON & Sensors to store the data received. (Version 10.0) [Source Code]. Available at: < https://community.home-assistant.io/t/webhook-json-sensors-to-store-the-data-received/781228 > [Accessed 11 September 2026]
LearnSmartCoding, 2025. SmartCertify API clean architecture dotnet9. (Version 10.0) [Source Code].Available at: < https://github.com/learnsmartcoding/smartcertify-api-clean-architecture-dotnet9/blob/main/LSC.SmartCertify.Infrastructure/BackgroundServices/NotificationBackgroundService.cs > [Accessed 12 September 2026]
StackOverflow, 2024. Fluentvalidation inclusiveBetween dynamically set range. (Version 10.0) [Source Code]. Available at: < https://stackoverflow.com/questions/23938127/fluentvalidation-inclusivebetween-dynamically-set-range > [Accessed 14 October 2026]
StackOverflow, 2012. Outputing getJSON results once and they update every x seconds without the results repeating over and over.(Version 10.0) [Source Code]. Available at: < https://stackoverflow.com/questions/11558060/outputing-getjson-results-once-and-they-update-every-x-seconds-without-the-resul > [Accessed 11 September 2026]
*/