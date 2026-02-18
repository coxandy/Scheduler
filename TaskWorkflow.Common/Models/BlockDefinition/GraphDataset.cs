namespace TaskWorkflow.Common.Models.BlockDefinition;

/// <summary>
/// Represents a single dataset within a graph.
/// </summary>
public class GraphDataset
{
    /// <summary>Dataset label shown in legend</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Data values</summary>
    public long[] Data { get; set; } = [];

    /// <summary>Background color or gradient expression</summary>
    public string BackgroundColor { get; set; } = string.Empty;
}