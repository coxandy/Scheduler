using TaskWorkflow.Common.Models;

namespace TaskWorkflow.Scheduler.Interfaces;

public interface ITaskExecutionService
{
    Task ExecuteTask(ScheduledTask scheduledTask);
}
