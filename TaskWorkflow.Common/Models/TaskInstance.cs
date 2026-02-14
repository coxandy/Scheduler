using TaskWorkflow.Common.Models.Enums;

namespace TaskWorkflow.Common.Models;

public class TaskInstance
{
    public ScheduledTask Instance { get; set; } = new();
    public string? RunId { get; set; }    
    public eTaskStatus Status { get; set; }    
    public DateTime dtEffective { get; set; }
    public bool IsManual { get; set; }
}