using CronosTask.Scheduler.Services;
using Serilog;

namespace CronosTask.Scheduler;

public class Program
{
    public static void Main(string[] args)
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "ticketapi-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var builder = Host.CreateApplicationBuilder(args);


        // Configure environment-specific settings
        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        Log.Information("Starting in {Environment} environment", builder.Environment.EnvironmentName);

        builder.Services.AddSerilog();
        builder.Services.AddWindowsService();
        builder.Services.AddHostedService<CronosTaskSchedulerService>();

        var host = builder.Build();
        host.Run();
    }
}
