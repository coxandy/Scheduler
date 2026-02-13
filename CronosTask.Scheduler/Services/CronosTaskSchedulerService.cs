using Cronos;
using CronosTask.Common.Models;
using CronosTask.Common.Helpers;
using Serilog;

namespace CronosTask.Scheduler.Services;

public class CronosTaskSchedulerService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly SemaphoreSlim _triggerSemaphore;

    public CronosTaskSchedulerService(IConfiguration congig)
    {
        _config = congig;
        var maxConcurrentTasks = _config.GetValue<int>("Scheduler:MaxConcurrentTasks", 3);
        _triggerSemaphore = new SemaphoreSlim(maxConcurrentTasks);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool ShowOnce = true;
        while (!stoppingToken.IsCancellationRequested)
        {
            var scheduledTasks = await CommonFileHelper.ReadCronosTaskScheduleAsync();
            if (ShowOnce)
            {
                await ConsoleDisplay(scheduledTasks);
                ShowOnce = false;
            }

            //determine which tasks are ready to trigger
            var tasksToTrigger = await CronosHelper.GetTasksReadyToTriggerAsync(scheduledTasks);

            var triggerTasks = tasksToTrigger.Select(async scheduledTask =>
            {
                await _triggerSemaphore.WaitAsync(stoppingToken);
                try
                {
                    await TriggerAsync(scheduledTask);
                }
                finally
                {
                    _triggerSemaphore.Release();
                }
            });

            await Task.WhenAll(triggerTasks);
            
            await Task.Delay(5000, stoppingToken);
        }
    }

    private static async Task ConsoleDisplay(List<ScheduledTask> scheduledTasks)
    {
        foreach (var scheduledTask in scheduledTasks)
        {
            Log.Information($"Task: {scheduledTask.TaskName}, Cron: {scheduledTask.CronExpression}, LastRun: {scheduledTask.LastRunTime}, Status: {scheduledTask.Status}");
        }
    }

    private static async Task<bool> TriggerAsync(ScheduledTask scheduledTask)
    {
        Log.Information("'{TaskName}' using Cron: '{CronExpression}' has been triggered", scheduledTask.TaskName, scheduledTask.CronExpression);

        scheduledTask.LastRunTime = DateTime.Now;

        await CommonFileHelper.WriteCronosTaskScheduleAsync(scheduledTask);

        return true;
    }
}
