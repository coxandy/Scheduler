namespace TaskWorkflow.Common.Models.BlockDefinition;

public class PivotSource
{
    public string DSTableSource { get; set; }
    public string DSTableTarget { get; set; }
    public List<string> Rows { get; set; } = new();
    public List<string> Columns { get; set; } = new();
    public List<string> Data { get; set; } = new();
    public string AggregateFunction { get; set; }
}