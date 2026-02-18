using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Data;

namespace TaskWorkflow.Common.Helpers;

public static class CommonHtmlHelper
{

    public static readonly JsonSerializerOptions _chartJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string ChartConfigToHtml(object chartConfig)
    {
        var json = JsonSerializer.Serialize(chartConfig, _chartJsonOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var labels = root.GetProperty("data").GetProperty("labels").EnumerateArray().Select(e => e.GetString() ?? "").ToList();
        var data = root.GetProperty("data").GetProperty("datasets")[0].GetProperty("data").EnumerateArray().Select(e => e.GetInt64()).ToList();
        var title = root.GetProperty("options").GetProperty("title").GetProperty("text").GetString() ?? "";

        var columns = new List<(string Name, bool RightAlign)>
        {
            ("Validator", false),
            ("Monthly Increase (Gwei)", true)
        };

        var rows = new List<string[]>();
        for (int i = 0; i < labels.Count; i++)
        {
            rows.Add([labels[i], data[i].ToString("N0")]);
        }

        return BuildHtmlTable(title, columns, rows);
    }

    public static string DataTableToHtml(DataTable dt, bool showColumnHeaders = true)
    {
        var columns = new List<(string Name, bool RightAlign)>();
        foreach (DataColumn col in dt.Columns)
        {
            bool rightAlign = IsNumericType(col.DataType);
            columns.Add((col.ColumnName, rightAlign));
        }

        var rows = new List<string[]>();
        foreach (DataRow row in dt.Rows)
        {
            var cells = new string[dt.Columns.Count];
            for (int i = 0; i < row.ItemArray.Length; i++)
            {
                var item = row.ItemArray[i];
                cells[i] = item switch
                {
                    double d => d.ToString("N2"),
                    float f => f.ToString("N2"),
                    decimal m => m.ToString("N2"),
                    long l => l.ToString("N0"),
                    int n => n.ToString("N0"),
                    _ => item?.ToString() ?? string.Empty
                };
            }
            rows.Add(cells);
        }

        string? title = string.IsNullOrWhiteSpace(dt.TableName) ? null : dt.TableName;
        return BuildHtmlTable(title, showColumnHeaders ? columns : null, rows);
    }

    private static string BuildHtmlTable(string? title, List<(string Name, bool RightAlign)>? columns, List<string[]> rows)
    {
        var sb = new StringBuilder();
        int colCount = columns?.Count ?? (rows.Count > 0 ? rows[0].Length : 1);

        sb.Append(@"<table style=""border-collapse:collapse; margin:10px 0 20px 0; font-family:Segoe UI,Arial,sans-serif; font-size:13px; width:100%;"">");

        if (title != null)
        {
            sb.Append($@"<tr><th colspan=""{colCount}"" style=""padding:6px 12px; background:#404040; color:white; text-align:left;"">{title}</th></tr>");
        }

        if (columns != null)
        {
            sb.Append(@"<tr style=""background:#e0e0e0;"">");
            foreach (var (name, rightAlign) in columns)
            {
                var align = rightAlign ? "right" : "left";
                sb.Append($@"<th style=""padding:4px 12px; text-align:{align}; border:1px solid #ccc;"">{name}</th>");
            }
            sb.Append("</tr>");
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var bg = i % 2 == 0 ? "#f9f9f9" : "#ffffff";
            sb.Append($@"<tr style=""background:{bg};"">");
            for (int j = 0; j < rows[i].Length; j++)
            {
                var align = columns != null && j < columns.Count && columns[j].RightAlign ? "right" : "left";
                sb.Append($@"<td style=""padding:4px 12px; text-align:{align}; border:1px solid #ccc;"">{rows[i][j]}</td>");
            }
            sb.Append("</tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(short) ||
               type == typeof(double) || type == typeof(float) || type == typeof(decimal) ||
               type == typeof(byte) || type == typeof(uint) || type == typeof(ulong);
    }
}
