using backendAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace backendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SimulatorController : ControllerBase
    {
        private readonly SeedDataBackgroundService _simulator;

        
        public SimulatorController(IEnumerable<IHostedService> hostedServices)
        {
            _simulator = hostedServices.OfType<SeedDataBackgroundService>().First();
        }

        [HttpPost("ramp")]
        public IActionResult Ramp(string device)
        {
            _simulator.StartRamp(device);
            return Ok($"{device} has started heating up.");
        }

        [HttpPost("cool")]
        public IActionResult Cool(string device)
        {
            _simulator.StopRamp(device);
            return Ok($"{device} is cooling down.");
        }
    }
}