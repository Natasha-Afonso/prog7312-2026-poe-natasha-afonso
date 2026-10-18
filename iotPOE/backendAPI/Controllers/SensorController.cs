using backendAPI.Data;
using backendAPI.Models;
using backendAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

//Emeris School of Computer Science, 2025
namespace backendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SensorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DeploymentTreeService _deploymentTreeService;
        private readonly DeviceRegistryService _registry;
        private readonly IWebHostEnvironment _env;

        // only these file types and sizes are accepted for the attachment (Microsoft Learn, 2026)
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".png", ".jpg", ".jpeg", ".txt", ".csv", ".log"
        };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public SensorsController(ApplicationDbContext context, DeploymentTreeService deploymentTreeService, DeviceRegistryService registry, IWebHostEnvironment env)
        {
            _context = context;
            _deploymentTreeService = deploymentTreeService;
            _registry = registry;
            _env = env;
        }

        // GET: api/Sensors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sensor>>> GetAll()
        {
            // AsNoTracking because the live values below are only for showing, they must never be saved back
            var sensors = await _context.Sensors
                .AsNoTracking()
                .Include(s => s.DeploymentNode)
                .ToListAsync();

            foreach (var sensor in sensors)
            {
                ApplyLiveValues(sensor);
            }

            return sensors;
        }

        // GET: api/Sensors/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Sensor>> GetById(int id)
        {
            var sensor = await _context.Sensors
                .AsNoTracking()
                .Include(s => s.DeploymentNode)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sensor == null)
            {
                return NotFound();
            }

            ApplyLiveValues(sensor);

            return sensor;
        }

        // POST: api/Sensors
        [HttpPost]
        public async Task<ActionResult<Sensor>> Create([FromForm] Sensor sensor, IFormFile? attachment)
        {
            var deploymentNode = await _context.DeploymentNodes
                .Include(n => n.Children)
                .FirstOrDefaultAsync(n => n.Id == sensor.DeploymentNodeId);

            if (deploymentNode == null)
            {
                return BadRequest("The specified deployment location does not exist");
            }

            var rootNode = await GetRootNode(deploymentNode);
            bool isValid = _deploymentTreeService.ValidateDeploymentPath(rootNode, deploymentNode.Name, "", out string fullPath);

            if (!isValid)
            {
                return BadRequest("The deployment location could not be validated against the facility tree");
            }

            // a second device with the same identifier would overwrite the first one in the registry
            if (_registry.TryGet(sensor.DeviceIdentifier, out _))
            {
                return BadRequest("A device with that identifier is already registered");
            }

            if (attachment != null && attachment.Length > 0)
            {
                // check the file type and size before anything is saved to disk
                if (!AllowedExtensions.Contains(Path.GetExtension(attachment.FileName)))
                {
                    return BadRequest("File type not allowed. Upload a pdf, image, txt, csv or log file");
                }

                if (attachment.Length > MaxFileSize)
                {
                    return BadRequest("File is too large. The maximum size is 5MB");
                }

                var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads", "Sensors");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // same collision-safe naming as the reference project
                // Path.GetFileName strips any folder parts from the uploaded name
                var fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(attachment.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await attachment.CopyToAsync(stream);
                }

                sensor.FilePath = "/Uploads/Sensors/" + fileName;
            }

            // the sensor is saved whether or not a file was attached
            sensor.RegisteredAt = DateTime.UtcNow;
            sensor.LastSeenAt = DateTime.UtcNow;
            _context.Sensors.Add(sensor);
            await _context.SaveChangesAsync();

            // the new device is available for instant lookups straight away
            _registry.Upsert(sensor);

            return CreatedAtAction(nameof(GetById), new { id = sensor.Id }, sensor);
        }

        // PUT: api/Sensors/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Sensor sensor)
        {
            if (id != sensor.Id)
            {
                return BadRequest("ID mismatch");
            }

            var existingSensor = await _context.Sensors.FindAsync(id);
            if (existingSensor == null)
            {
                return NotFound();
            }

            //mapping the updated fields over the DB
            existingSensor.Status = sensor.Status;
            existingSensor.LastReading = sensor.LastReading;
            existingSensor.Category = sensor.Category;
            existingSensor.DeploymentNodeId = sensor.DeploymentNodeId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SensorExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            _registry.Upsert(existingSensor);

            return NoContent();
        }

        // DELETE: api/Sensors/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var sensor = await _context.Sensors.FindAsync(id);
            if (sensor == null)
            {
                return NotFound();
            }

            _context.Sensors.Remove(sensor);
            await _context.SaveChangesAsync();

            _registry.Remove(sensor.DeviceIdentifier);

            return NoContent();
        }

        private bool SensorExists(int id)
        {
            return _context.Sensors.Any(e => e.Id == id);
        }

        // the newest reading, state and last seen time live in the registry, so show those instead of the old database values
        private void ApplyLiveValues(Sensor sensor)
        {
            if (_registry.TryGet(sensor.DeviceIdentifier, out DeviceState? device) && device != null)
            {
                sensor.LastReading = device.LastReading;
                sensor.State = device.State;
                sensor.LastSeenAt = device.LastSeenAt;
                sensor.Status = device.Status;
            }
        }

        private async Task<DeploymentNode> GetRootNode(DeploymentNode node)
        {
            var current = node;

            while (current.ParentId != null)
            {
                current = await _context.DeploymentNodes
                    .Include(n => n.Children)
                    .FirstAsync(n => n.Id == current.ParentId);
            }

            return current;
        }
    }
}

/** 
References
CodeWithGopi. 2025. Create and Consume Web API in ASP.NET Core MVC | Full CRUD Operatins & API Consumption. (Version 10.0) [Source Code]. Available at:<https://youtu.be/knTcwvFWOQM?si=b3q3ksLg6HNnBjDg> [Accessed 4 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 1 - Getting started. (Version 10.0) [Source Code]. Available at: <https://youtu.be/ldfC-rTzHPY?si=znsmPz-ZSPaY-4Ii> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 2- connecting between the two!. (Version 10.0) [Source Code]. Available at: <https://youtu.be/_0EhC3WauAc?si=nEazkCpBTV1YpHBp> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 3- databases in docker. (Version 10.0) [Source Code]. Available at: <https://youtu.be/6tYEbngWY-E?si=10rSEEpfSNL5Kg8k> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 4- connecting to database from the API.(Version 10.0) [Source Code]. Available at: <https://youtu.be/_PwpPMoeqzw?si=Q0ELlPZdN42dFU9q> [Accessed 5 June 2026]
Emeris School of Computer Science. 2025. PROG7311 Docker part 5- connecting them all together- DB -API -MVC. (Version 10.0) [Source Code]. Available at: <https://youtu.be/YlIajRMdLPk?si=D75XZKO-L6nQhR-3> [Accessed 5 June 2026]
Microsoft Learn, 2026. Upload files in ASP.NET Core. (Version 10.0) [Source Code]. Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads> [Accessed 16 Month 2026] 
Microsoft Learn, 2026. HashSet<T> Class. (Version 10.0) [Source Code]. Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1> [Accessed 16 Month 2026] 

*/