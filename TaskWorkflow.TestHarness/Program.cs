using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using TaskWorkflow.Scheduler.Interfaces;
using TaskWorkflow.Scheduler.Services;
using TaskWorkflow.Common.TestRunData;

namespace TaskWorkflow.TestHarness;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var webServers = configuration.GetSection("Scheduler:WebServers").Get<List<string>>() ?? new();
        var port = configuration.GetValue<int>("Scheduler:Port");
        CommonUriHelper.Initialize(webServers, port);

        var environmentName = configuration.GetValue<string>("Environment") ?? "Development";

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = environmentName });
        services.AddHttpClient();
        services.AddTransient<ITaskExecutionService, TaskExecutionService>();
        var serviceProvider = services.BuildServiceProvider();

        var taskExecutionService = serviceProvider.GetRequiredService<ITaskExecutionService>();

        long taskId = args.Length > 0 && long.TryParse(args[0], out var id) ? id : 1;

        await RunTask(taskId, taskExecutionService);
    }

    public static async Task RunTask(long taskId, ITaskExecutionService taskExecutionService)
    {
        Log.Information("Looking up task with TaskId {TaskId}", taskId);

        var tasks = await TestDataHelper.GetTestTasks();
        var scheduledTask = tasks.FirstOrDefault(t => t.TaskId == taskId);

        if (scheduledTask == null)
        {
            Log.Error("Task with TaskId {TaskId} not found", taskId);
            return;
        }

        Log.Information("Found task '{TaskName}' (WebService: {WebService}). Executing...",
            scheduledTask.TaskName, scheduledTask.WebService);

        await taskExecutionService.ExecuteTask(scheduledTask);

        Log.Information("RunTask completed for TaskId {TaskId}", taskId);
    }
}
