using backendAPI.Data;
using backendAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

//Emeris School of Computer Science, 2025
namespace backendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlertsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AlertsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Alerts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Alert>>> GetAll()
        {
            return await _context.Alerts.Include(a => a.Sensor).ToListAsync();
        }

        // GET: api/Alerts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Alert>> GetById(int id)
        {
            var alert = await _context.Alerts
                .Include(a => a.Sensor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (alert == null)
            {
                return NotFound();
            }

            return alert;
        }

        // POST: api/Alerts
        [HttpPost]
        public async Task<ActionResult<Alert>> Create(Alert alert)
        {
            var sensor = await _context.Sensors.FindAsync(alert.SensorId);
            if (sensor == null)
            {
                return BadRequest("The specified sensor does not exist");
            }

            alert.Status = "unread";
            alert.Timestamp = DateTime.UtcNow;

            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = alert.Id }, alert);
        }

        // PUT: api/Alerts/5
        // also used to acknowledge/resolve an alert by updating its Status
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Alert alert)
        {
            if (id != alert.Id)
            {
                return BadRequest("ID mismatch");
            }

            var existingAlert = await _context.Alerts.FindAsync(id);
            if (existingAlert == null)
            {
                return NotFound();
            }

            existingAlert.Status = alert.Status;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AlertExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Alerts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null)
            {
                return NotFound();
            }

            _context.Alerts.Remove(alert);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AlertExists(int id)
        {
            return _context.Alerts.Any(e => e.Id == id);
        }
    }
}