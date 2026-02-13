namespace TaskWorkflow.Common.Models;

public class ScheduledTask
{
    public long TaskId { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public DateTime LastRunTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string WebService { get; set; } = string.Empty;
    public int DayOffset { get; set; }
}
