using backendAPI.Services;
using Microsoft.AspNetCore.Mvc;

//Emeris School of Computer Science, 2025
namespace backendAPI.Controllers
{
    // Exposes endpoints to manually trigger the Simulator's heat ramp (Microsoft Learn, 2026)
    [Route("api/[controller]")]
    [ApiController]
    public class SimulatorController : ControllerBase
    {
        private readonly SeedDataBackgroundService _simulator;

        public SimulatorController(SeedDataBackgroundService simulator)
        {
            _simulator = simulator;
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

/** 
References
Microsoft Learn, 2026. Create web APIs with ASP.NET Core. [online] Available at: <https://learn.microsoft.com/en-us/aspnet/core/web-api/> [Accessed 12 November 2026]
*/