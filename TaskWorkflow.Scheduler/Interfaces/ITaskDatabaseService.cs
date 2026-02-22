using TaskWorkflow.Common.Models;

namespace TaskWorkflow.Scheduler.Interfaces;

public interface ITaskDatabaseService
{
    Task<List<ScheduledTask>> GetScheduledTasksAsync();
    Task UpdateTaskStatusAsync(ScheduledTask scheduledTask);
}
