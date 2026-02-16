namespace TaskWorkflow.Common.Models.TaskDefinition;
public class EmailOutcome
{
    public bool Email { get; set; }
    public List<string> To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public string Priority { get; set; }
    public List<string> Attachments { get; set; }
}