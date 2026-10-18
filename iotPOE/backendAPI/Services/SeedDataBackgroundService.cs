using backendAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    public class SeedDataBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<SeedDataBackgroundService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly Random _random = new Random();
        
        // Fires every 5 seconds (learnsmartcoding / Microsoft Learn pattern)
        private static readonly TimeSpan PushInterval = TimeSpan.FromSeconds(5); 

        private List<SeedSensorConfig> _seedConfigs = new();

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
            await LoadSeedSensorsAsync();

            var client = _httpClientFactory.CreateClient();
            // ⚠️ CHECK YOUR LAUNCHSETTINGS.JSON FOR YOUR ACTUAL PORT (e.g. 5000, 5001, 7123)
            client.BaseAddress = new Uri("http://localhost:8080"); 

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        var allSensors = await dbContext.Sensors.ToListAsync(stoppingToken);
                        if (allSensors.Count == 0) 
                        { 
                            await Task.Delay(PushInterval, stoppingToken); 
                            continue; 
                        }

                        // 1. Keep Actuators "alive" so the DisconnectWatcher doesn't mark them offline
                        var actuators = allSensors.Where(s => s.Category == "Actuator").ToList();
                        foreach (var act in actuators)
                        {
                            act.LastSeenAt = DateTime.UtcNow;
                        }
                        await dbContext.SaveChangesAsync(stoppingToken);

                        // 2. Pick 1 or 2 random sensors to send data for
                        var telemetrySensors = allSensors.Where(s => s.Category != "Actuator").ToList();
                        var batchSensors = telemetrySensors.OrderBy(x => _random.Next()).Take(_random.Next(1, 3)).ToList();

                        if (batchSensors.Count > 0)
                        {
                            // 3. Build the jagged array (double[][]) required by TelemetryController
                            string[] sensorIds = new string[batchSensors.Count];
                            double[][] rawBatches = new double[batchSensors.Count][];

                            for (int i = 0; i < batchSensors.Count; i++)
                            {
                                var sensor = batchSensors[i];
                                var config = _seedConfigs.FirstOrDefault(c => c.DeviceIdentifier == sensor.DeviceIdentifier);
                                
                                double min = config?.MinValue ?? 10.0;
                                double max = config?.MaxValue ?? 100.0;
                                double randomValue = Math.Round(_random.NextDouble() * (max - min) + min, 2);

                                sensorIds[i] = sensor.DeviceIdentifier;
                                rawBatches[i] = new double[] { randomValue }; 
                            }

                            var payload = new 
                            {
                                SensorIds = sensorIds,
                                RawBatches = rawBatches
                            };

                            // 4. POST to your actual TelemetryController endpoint
                            var response = await client.PostAsJsonAsync("/api/Telemetry/ingest-batch", payload, stoppingToken);
                            
                            if (response.IsSuccessStatusCode)
                            {
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

        // LearnSmartCoding / Home Assistant reference pattern for loading JSON
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