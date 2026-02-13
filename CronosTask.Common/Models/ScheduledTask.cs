namespace CronosTask.Common.Models;

public class ScheduledTask
{
    public string CronExpression { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public DateTime LastRunTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string WebService { get; set; } = string.Empty;
}
