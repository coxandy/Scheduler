namespace TaskWorkflow.Common.Models.BlockDefinition;

public class Worksheet
{
    public string WorksheetName { get; set; }
    public string DSTable { get; set; }
    public string TopLeft { get; set; }
    public bool IncludeHeader { get; set; }
}