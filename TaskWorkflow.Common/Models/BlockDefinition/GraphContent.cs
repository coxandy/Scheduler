namespace TaskWorkflow.Common.Models.BlockDefinition;

/// <summary>
/// General-purpose graph configuration for chart generation.
/// </summary>
public class GraphContent
{
    /// <summary>Chart type (e.g. "bar", "line", "pie")</summary>
    public string Type { get; set; } = "bar";

    /// <summary>Chart title text</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Title font size</summary>
    public int TitleFontSize { get; set; } = 14;

    /// <summary>Title font style (e.g. "bold", "normal")</summary>
    public string TitleFontStyle { get; set; } = "bold";

    /// <summary>Whether to display the legend</summary>
    public bool ShowLegend { get; set; } = false;

    /// <summary>X-axis labels</summary>
    public string[] Labels { get; set; } = [];

    /// <summary>Datasets for the chart</summary>
    public List<GraphDataset> Datasets { get; set; } = [];
}