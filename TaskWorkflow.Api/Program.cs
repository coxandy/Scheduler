using TaskWorkflow.Api.Interfaces;
using TaskWorkflow.Api.Services;
using TaskWorkflow.Common.Helpers;
using Serilog;

namespace TaskWorkflow.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "taskworkflow-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

        Log.Information("Starting in {Environment} environment", builder.Environment.EnvironmentName);

        builder.Services.AddSerilog();
        builder.Services.AddWindowsService();
        builder.Services.AddControllers();

        // Add app services to DI
        builder.Services.AddScoped<ITaskObjectCreationService, TaskObjectCreationService>();

        builder.WebHost.UseUrls("https://localhost:5010");

        var app = builder.Build();

        // Initialize static connection string helper from configuration
        var connectionStrings = builder.Configuration.GetSection("ConnectionStrings")
            .GetChildren()
            .ToDictionary(x => x.Key, x => x.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        ConnectionStringHelper.Initialize(connectionStrings);

        app.MapControllers();

        app.Run();
    }
}
