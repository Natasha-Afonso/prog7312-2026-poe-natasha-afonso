using backendAPI.Models;
using backendAPI.Services;
using Microsoft.AspNetCore.Mvc;

//Emeris School of Computer Science, 2025
namespace backendAPI.Controllers
{
    // Exposes prediction endpoints for anomaly detection (Microsoft Learn, 2026)
    [Route("api/[controller]")]
    [ApiController]
    public class PredictionController : ControllerBase
    {
        private readonly AnomalyDetectionService _anomalyDetector;
        private readonly DeviceRegistryService _registry;
        private readonly PredictiveActionEngine _predictiveEngine;

        public PredictionController(AnomalyDetectionService anomalyDetector, DeviceRegistryService registry, PredictiveActionEngine predictiveEngine)
        {
            _anomalyDetector = anomalyDetector;
            _registry = registry;
            _predictiveEngine = predictiveEngine;
        }

        // GET: api/Prediction
        // Returns every sensor heading for its safe limit (Microsoft Learn, 2021)
        [HttpGet]
        public ActionResult<IEnumerable<PredictiveAction>> GetAll()
        {
            return Ok(_predictiveEngine.GetPredictiveActions());
        }

  

        // GET: api/Prediction/TEMP:SRV
        // Returns the anomaly prediction for one specific sensor (Microsoft Learn, 2021)
        [HttpGet("{deviceIdentifier}")]
        public ActionResult<AnomalyPrediction> GetForDevice(string deviceIdentifier)
        {
            if (!_registry.TryGet(deviceIdentifier, out DeviceState? device) || device == null)
            {
                return NotFound("No device with that identifier is registered");
            }

            var anomaly = _anomalyDetector.DetectAnomaly(device);

            if (anomaly == null)
            {
                return NotFound("No anomaly is predicted for this device right now");
            }

            return Ok(anomaly);
        }
    }
}

/** 
References
Microsoft Learn, 2026. Create web APIs with ASP.NET Core. [online] Available at: <https://learn.microsoft.com/en-us/aspnet/core/web-api/> [Accessed 12 October 2026]
*/