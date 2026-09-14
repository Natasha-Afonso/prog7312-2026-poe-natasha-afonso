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
     private readonly IWebHostEnvironment _env;

        public SensorsController(ApplicationDbContext context, DeploymentTreeService deploymentTreeService,  IWebHostEnvironment env)
        {
            _context = context;
            _deploymentTreeService = deploymentTreeService;
             _env = env;
        }

        // GET: api/Sensors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sensor>>> GetAll()
        {
            return await _context.Sensors.Include(s => s.DeploymentNode).ToListAsync();
        }

        // GET: api/Sensors/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Sensor>> GetById(int id)
        {
            var sensor = await _context.Sensors
                .Include(s => s.DeploymentNode)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sensor == null)
            {
                return NotFound();
            }

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
        return BadRequest("The specified deployment location does not exist");

    var rootNode = await GetRootNode(deploymentNode);
    bool isValid = _deploymentTreeService.ValidateDeploymentPath(rootNode, deploymentNode.Name, "", out string fullPath);

    if (!isValid)
        return BadRequest("The deployment location could not be validated against the facility tree");

    if (attachment != null && attachment.Length > 0)
    {
        var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads", "Sensors");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // same collision-safe naming as the reference project
        var fileName = Guid.NewGuid().ToString() + "_" + attachment.FileName;
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await attachment.CopyToAsync(stream);
        }

        sensor.FilePath = "/Uploads/Sensors/" + fileName;
    }

    sensor.RegisteredAt = DateTime.UtcNow;
    _context.Sensors.Add(sensor);
    await _context.SaveChangesAsync();

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

            return NoContent();
        }

        private bool SensorExists(int id)
        {
            return _context.Sensors.Any(e => e.Id == id);
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