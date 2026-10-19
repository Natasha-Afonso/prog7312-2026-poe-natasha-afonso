using backendAPI.Services;
using Microsoft.AspNetCore.Mvc;

//Emeris School of Computer Science, 2025
namespace backendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueueController : ControllerBase
    {
        private readonly TelemetryQueueService _queues;

        public QueueController(TelemetryQueueService queues)
        {
            _queues = queues;
        }

        // GET: api/Queue/status
        // how many readings are waiting in each queue, how many have been processed
        // and the last 20  (shows priority readings being processed before normal ones ones)
        [HttpGet("status")]
        public ActionResult<QueueStatus> GetStatus()
        {
            return Ok(_queues.GetStatus());
        }
    }
}