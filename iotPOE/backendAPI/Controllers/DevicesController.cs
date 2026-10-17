using backendAPI.Models;
using backendAPI.Services;
using Microsoft.AspNetCore.Mvc;

//Emeris School of Computer Science, 2025
namespace backendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private readonly DeviceRegistryService _registry;

        public DevicesController(DeviceRegistryService registry)
        {
            _registry = registry;
        }

        // GET: api/Devices
        // every live device straight from the in-memory registry (no database query)
        [HttpGet]
        public ActionResult<IEnumerable<DeviceState>> GetAll()
        {
            return Ok(_registry.GetAll());
        }

        // GET: api/Devices/SS:01
        // one device by its identifier, O(1) Dictionary lookup
        [HttpGet("{deviceIdentifier}")]
        public ActionResult<DeviceState> GetByIdentifier(string deviceIdentifier)
        {
            if (!_registry.TryGet(deviceIdentifier, out DeviceState? device) || device == null)
            {
                return NotFound("No device with that identifier is registered");
            }

            return Ok(device);
        }
    }
}