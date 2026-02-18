using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Encodings.Web;
using TaskWorkflow.Common.Models.BlockDefinition;



public static partial class CommonGraphDataHelper  //needs to be partial to allow GeneratedRegex to work at runtime
{       
    [GeneratedRegex(@"""(getGradientFillHelper\([^""]*\))""")]
    private static partial Regex QuickChartHelperRegex();

    private static readonly JsonSerializerOptions _chartJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static List<object> GenerateGraph(List<GraphContent> graphContents)
    {
        return graphContents
            .Select(gc => new
            {
                type = gc.Type,
                data = new
                {
                    labels = gc.Labels,
                    datasets = gc.Datasets.Select(ds => new
                    {
                        label = ds.Label,
                        data = ds.Data,
                        backgroundColor = ds.BackgroundColor
                    }).ToArray()
                },
                options = new
                {
                    legend = new { display = gc.ShowLegend },
                    title = new
                    {
                        display = true,
                        text = gc.Title,
                        fontStyle = gc.TitleFontStyle,
                        fontSize = gc.TitleFontSize
                    }
                }
            })
            .Cast<object>()
            .ToList();
    }

    public static async Task<byte[]> CreateGraphImage(object? chartConfig, int width = 500, int height = 300)
    {
        string json = JsonSerializer.Serialize(chartConfig, _chartJsonOptions);

        // Unquote QuickChart helper functions so they are evaluated as JavaScript
        json = QuickChartHelperRegex().Replace(json, "$1");

        string url = $"https://quickchart.io/chart?c={Uri.EscapeDataString(json)}&w={width}&h={height}";

        using HttpClient client = new HttpClient();
        return await client.GetByteArrayAsync(url);
    }
}
