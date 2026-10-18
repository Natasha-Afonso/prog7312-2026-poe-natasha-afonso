using backendAPI.Services;
using Microsoft.AspNetCore.Mvc;

//Emeris School of Computer Science, 2025
namespace backendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectivityController : ControllerBase
    {
        private readonly DisconnectTrackerService _tracker;

        public ConnectivityController(DisconnectTrackerService tracker)
        {
            _tracker = tracker;
        }

        // GET: api/Connectivity/status
        // the sensors that are disconnected right now (the HashSet)
        // and how many duplicate disconnect alerts the set has stopped
        [HttpGet("status")]
        public ActionResult<ConnectivityStatus> GetStatus()
        {
            return Ok(_tracker.GetStatus());
        }
    }
}