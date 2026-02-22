using TaskWorkflow.Common.Models.BlockDefinition.Enums;

namespace TaskWorkflow.Common.Models;

public class ScheduledTask
{
    public long TaskId { get; set; }
    public bool IsActive { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public DateTime LastRunTime { get; set; }
    public eTaskStatus Status { get; set; }
    public string WebService { get; set; } = string.Empty;
    public int DayOffset { get; set; }
    public long TaskJsonDefinitionId { get; set; }
    
}
