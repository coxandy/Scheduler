using Cronos;
using CronosTask.Common.Models;
using Serilog;

namespace CronosTask.Common.Helpers;

public static class CronosHelper
{

    public static async Task <List<ScheduledTask>> GetTasksReadyToTriggerAsync(List<ScheduledTask> scheduledTasks)
    {
        List<ScheduledTask> readyToTrigger = new List<ScheduledTask>();
        foreach (var scheduledTask in scheduledTasks)
        {                
            if (await ReadyToTriggerAsync(scheduledTask))
            {
                Log.Information($"Task: {scheduledTask.TaskName} ready to trigger");
                readyToTrigger.Add(scheduledTask);
            }
        }  
        return readyToTrigger;
    }

    public static async Task <bool> ReadyToTriggerAsync(ScheduledTask scheduledTask)
    {
        var cronExpression = CronExpression.Parse(scheduledTask.CronExpression);
        var nextRun = cronExpression.GetNextOccurrence(scheduledTask.LastRunTime.ToUniversalTime());
        if (nextRun == null)
            return false;
        return DateTime.UtcNow >= nextRun.Value;
    }
}