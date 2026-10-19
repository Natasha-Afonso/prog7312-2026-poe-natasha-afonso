using Microsoft.ML;
using Microsoft.Extensions.Logging;
using backendAPI.Models;

//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    // Defines the prediction data class to hold anomaly detection results (Microsoft Learn, 2021)
    public record AnomalyPrediction(
        string DeviceIdentifier,
        string Category,
        double CurrentValue,
        double Limit,
        bool AlertWhenAbove,
        double ChangePerSecond,
        int SecondsToBreach,
        double FitQuality,
        int PointsUsed,
        string Method);

    // Specifies an input data class for the time-series (Microsoft Learn, 2021)
    public class AnomalyData
    {
        public float TimeSeconds { get; set; }
        public float Value { get; set; }
    }

    // Holds the score predicted by the model (Gami, 2025)
    public class AnomalyScore
    {
        public float Score { get; set; }
    }

    // Detects anomalies in time-series data by training a regression model on historical readings (Microsoft Learn, 2021)
    public class AnomalyDetectionService
    {
        private const int WindowSeconds = 60;
        private const int MinPoints = 8;
        private const int MinSpanSeconds = 10;
        private const int MaxHorizonSeconds = 180;
        private const double MinFitQuality = 0.6;
        private static readonly TimeSpan CacheTime = TimeSpan.FromSeconds(2);

        private readonly DeviceRegistryService _registry;
        private readonly TimelineService _timeline;
        private readonly ILogger<AnomalyDetectionService> _logger;

        // Initialize the mlContext variable (gonna have to install ML package) (Microsoft Learn, 2021)
        private readonly MLContext _mlContext = new MLContext(seed: 1);

        private readonly Dictionary<string, (DateTime ComputedAt, AnomalyPrediction? Result)> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly object _cacheLock = new();
        private readonly object _mlLock = new();

        public AnomalyDetectionService(
            DeviceRegistryService registry,
            TimelineService timeline,
            ILogger<AnomalyDetectionService> logger)
        {
            _registry = registry;
            _timeline = timeline;
            _logger = logger;
        }

        public AnomalyPrediction? DetectAnomaly(DeviceState device)
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(device.DeviceIdentifier, out var cached) &&
                    DateTime.UtcNow - cached.ComputedAt < CacheTime)
                {
                    return cached.Result;
                }
            }

            AnomalyPrediction? result = null;

            try
            {
                lock (_mlLock)
                {
                    result = Calculate(device);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Anomaly detection failed for {Id}: {Message}", device.DeviceIdentifier, ex.Message);
            }

            lock (_cacheLock)
            {
                _cache[device.DeviceIdentifier] = (DateTime.UtcNow, result);
            }

            return result;
        }

        public List<AnomalyPrediction> GetAllAnomalies()
        {
            var anomalies = new List<AnomalyPrediction>();

            foreach (var device in _registry.GetAll())
            {
                var anomaly = DetectAnomaly(device);

                if (anomaly != null)
                {
                    anomalies.Add(anomaly);
                }
            }

            return anomalies.OrderBy(a => a.SecondsToBreach).ToList();
        }

        private AnomalyPrediction? Calculate(DeviceState device)
        {
            if (device.IsActuator) return null;

            if (!AlertRules.TryGetRule(device.Category, out AlertRule? rule) || rule == null)
            {
                return null;
            }

            DateTime cutoff = DateTime.UtcNow.AddSeconds(-WindowSeconds);
            var points = _timeline.GetPoints(device.DeviceIdentifier, 60)
                .Where(p => p.Timestamp >= cutoff)
                .ToList();

            if (points.Count < MinPoints) return null;

            DateTime first = points[0].Timestamp;

            // Load the data into an IDataView (Microsoft Learn, 2021)
            var data = points
                .Select(p => new AnomalyData
                {
                    TimeSeconds = (float)(p.Timestamp - first).TotalSeconds,
                    Value = (float)p.Value
                })
                .ToList();

            float lastTime = data[data.Count - 1].TimeSeconds;

            if (lastTime < MinSpanSeconds) return null;

            double current = points[points.Count - 1].Value;
            bool pastLimit = rule.AlertWhenAbove ? current > rule.Limit : current < rule.Limit;

            if (pastLimit) return null;

            double mean = data.Average(r => r.Value);
            double totalVariation = data.Sum(r => Math.Pow(r.Value - mean, 2));

            if (totalVariation < 1e-9) return null;

            // Convert data into an IDataView for easier display and training (Microsoft Learn, 2021)
            IDataView dataView = _mlContext.Data.LoadFromEnumerable(data);

            // Define the training pipeline: concatenate, normalize, and append SDCA trainer (Microsoft Learn, 2021)
            var pipeline = _mlContext.Transforms.Concatenate("Features", nameof(AnomalyData.TimeSeconds))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.Regression.Trainers.Sdca(
                    labelColumnName: nameof(AnomalyData.Value),
                    featureColumnName: "Features",
                    maximumNumberOfIterations: 200));

            ITransformer model = pipeline.Fit(dataView);
            var engine = _mlContext.Model.CreatePredictionEngine<AnomalyData, AnomalyScore>(model);

            double leftOver = data.Sum(r => Math.Pow(r.Value - engine.Predict(r).Score, 2));
            double fitQuality = 1 - leftOver / totalVariation;

            if (fitQuality < MinFitQuality) return null;

            double valueNow = engine.Predict(new AnomalyData { TimeSeconds = lastTime }).Score;
            double valueLater = engine.Predict(new AnomalyData { TimeSeconds = lastTime + 60 }).Score;
            double changePerSecond = (valueLater - valueNow) / 60.0;

            double towardLimit = rule.AlertWhenAbove ? changePerSecond : -changePerSecond;

            if (towardLimit <= 0) return null;

            double remaining = rule.AlertWhenAbove ? rule.Limit - current : current - rule.Limit;
            int secondsToBreach = (int)Math.Round(remaining / towardLimit);

            if (secondsToBreach > MaxHorizonSeconds) return null;

            return new AnomalyPrediction(
                device.DeviceIdentifier,
                device.Category,
                current,
                rule.Limit,
                rule.AlertWhenAbove,
                changePerSecond,
                Math.Max(1, secondsToBreach),
                fitQuality,
                data.Count,
                "ML.NET SDCA Regression (Time-Series)");
        }
    }
}

/** 
References
Gami, R., 2025. Building Predictive Analytics Dashboards with ML.NET and Chart.js: Turning Data into Real-Time Insights. [online] C# Corner. Available at: <https://www.c-sharpcorner.com/article/building-predictive-analytics-dashboards-with-ml-net-and-chart-js-turning-data> [Accessed 16 October 2026]
Microsoft Learn, 2021. Tutorial: Detect anomalies in time series with ML.NET. [online] Available at: <https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/phone-calls-anomaly-detection> [Accessed 16 October 2026]
Microsoft Learn, 2026. ML.NET documentation. [online] Available at: <https://learn.microsoft.com/en-us/dotnet/machine-learning/> [Accessed 16 October 2026]
*/