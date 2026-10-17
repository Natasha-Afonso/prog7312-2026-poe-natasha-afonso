using Microsoft.EntityFrameworkCore;
using backendAPI.Models;

namespace backendAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Sensor> Sensors { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<DeploymentNode> DeploymentNodes { get; set; }
        public DbSet<CommandLog> CommandLogs { get; set; }
        public DbSet<SensorReading> SensorReadings { get; set; }
    }
}