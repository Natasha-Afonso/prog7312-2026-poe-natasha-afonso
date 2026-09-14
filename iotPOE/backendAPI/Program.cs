
using Microsoft.EntityFrameworkCore;
using backendAPI.Data;
using backendAPI.Services;
using System.Text.Json.Serialization;

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

            var app = builder.Build();

            if (true)
            {
                app.MapOpenApi(); // Generates /openapi/v1.json

                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/openapi/v1.json", "Smart-X API v1");
                    options.RoutePrefix = "swagger";
                });
            }
//(Rudman,2026)
            app.UseHttpsRedirection();
             // Enable CORS middleware (Must be between UseHttpsRedirection and UseAuthorization)
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
