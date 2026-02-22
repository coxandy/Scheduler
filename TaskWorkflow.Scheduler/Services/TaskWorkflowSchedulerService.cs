using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Helpers;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using TaskWorkflow.Scheduler.Interfaces;
using Serilog;

namespace TaskWorkflow.Scheduler.Services;

public class TaskWorkflowSchedulerService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly SemaphoreSlim _triggerSemaphore;
    private readonly ITaskExecutionService _taskExecutionService;
    private readonly ITaskDatabaseService _taskDatabaseService;

    public TaskWorkflowSchedulerService(IConfiguration congig, ITaskExecutionService taskExecutionService, ITaskDatabaseService taskDatabaseService)
    {
        _config = congig;
        _taskExecutionService = taskExecutionService;
        _taskDatabaseService = taskDatabaseService;
        var maxConcurrentTasks = _config.GetValue<int>("Scheduler:MaxConcurrentTasks", 3);
        _triggerSemaphore = new SemaphoreSlim(maxConcurrentTasks);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var scheduledTasks = await _taskDatabaseService.GetScheduledTasksAsync();          

            // determine which tasks are ready to trigger
            var tasksToTrigger = await CommonCronosHelper.GetTasksReadyToTriggerAsync(scheduledTasks);

            // run those tasks that are scheduled
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

    private async Task<bool> TriggerAsync(ScheduledTask scheduledTask)
    {
        Log.Information("'{TaskName}' using Cron: '{CronExpression}' has been triggered", scheduledTask.TaskName, scheduledTask.CronExpression);
        scheduledTask.LastRunTime = DateTime.Now;
        scheduledTask.Status = eTaskStatus.Running;
        await _taskDatabaseService.UpdateTaskStatusAsync(scheduledTask);
        await _taskExecutionService.ExecuteTask(scheduledTask);
        return true;
    }
}
