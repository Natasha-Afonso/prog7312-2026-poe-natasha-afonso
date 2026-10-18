
using Microsoft.EntityFrameworkCore;
using backendAPI.Data;
using backendAPI.Services;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;


namespace backendAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Define CORS policy name
            var corsPolicyName = "AllowReactApp";

            // Add services to the container.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: corsPolicyName, policy =>
                {
                    policy.WithOrigins("http://localhost:8081", "http://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });


            // Add services to the container.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")));

            builder.Services.AddScoped<DeploymentTreeService>();
            builder.Services.AddScoped<TelemetryIngestionService>();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // This tells the serializer to ignore loops instead of crashing
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                });

            builder.Services.AddHttpClient();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi(); // Keeps the native engine


          

            // Registers the background loop
            builder.Services.AddHostedService<SeedDataBackgroundService>();
            builder.Services.AddSingleton<DeviceRegistryService>();
            builder.Services.AddSingleton<TelemetryQueueService>();
            builder.Services.AddHostedService<TelemetryQueueWorker>();
            builder.Services.AddSingleton<CommandHistoryService>();
            builder.Services.AddSingleton<DisconnectTrackerService>();
            builder.Services.AddHostedService<DisconnectWatcher>();
            builder.Services.AddSingleton<TimelineService>(); 

  builder.Services.AddSingleton<SearchHistoryService>(); 
  
            var app = builder.Build();

            // load every device into the registry once at startup (Dictionary, O(1) lookups after this)
            // SQL Server can start slower than the API in Docker, so it tries a few times
            var registry = app.Services.GetRequiredService<DeviceRegistryService>();
            var timeline = app.Services.GetRequiredService<TimelineService>();

            for (int attempt = 1; attempt <= 10; attempt++)
            {
                try
                {
                    using var scope = app.Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    registry.Load(db.Sensors.AsNoTracking().ToList());

                    // rebuild the sorted timelines from the newest saved readings, so charts have history after a restart
                    var recent = db.SensorReadings.AsNoTracking().OrderByDescending(r => r.Timestamp).Take(3000).ToList();
                    timeline.LoadFromReadings(recent, registry.GetAll());

                    Console.WriteLine("Device registry and timelines loaded");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Registry load attempt " + attempt + " failed: " + ex.Message);
                    Thread.Sleep(3000);
                }
            }

            // Swagger is always on
            app.MapOpenApi(); // Generates /openapi/v1.json

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "Smart-X API v1");
                options.RoutePrefix = "swagger";
            });

            //(Rudman,2026)
            app.UseHttpsRedirection();
            // Enable CORS middleware (Must be between UseHttpsRedirection and UseAuthorization)
            var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "Uploads");
            Directory.CreateDirectory(uploadsRoot); // PhysicalFileProvider needs the folder to exist

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadsRoot),
                RequestPath = "/Uploads"
            });
            app.UseCors(corsPolicyName);
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}

/** 
References
Rudman, G., 2026. insy7314-library. (Version 1.0) [Source Code] Available at: < https://github.com/rudderz243/insy7314-library > [Accessed 29 August 2026]
*/
