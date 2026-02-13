using CronosTask.Common.Models;

namespace CronosTask.Scheduler.Interfaces;

public interface ITaskExecutionService
{
    Task ExecuteTask(ScheduledTask scheduledTask);
}
