using System.Data;

namespace TaskWorkflow.Common.Helpers;

public static class CommonPivotHelper
{

    public static DataTable PivotDataTable(
        List<string> Rows,
        List<string> Cols,
        List<string> Data,
        string TableName,
        DataTable sourceTable,
        string AggregateFunction = "Sum",
        string WeightColumn = null) // Optional parameter for Weighted Average
    {
        DataTable pivotedTable = new DataTable(TableName);

        var columnValues = sourceTable.AsEnumerable()
            .Select(row => string.Join("_", Cols.Select(c => row[c].ToString())))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        foreach (string rowName in Rows)
            pivotedTable.Columns.Add(rowName, sourceTable.Columns[rowName].DataType);

        foreach (string colValue in columnValues)
        {
            foreach (string dataCol in Data)
                pivotedTable.Columns.Add($"{colValue}_{dataCol}", typeof(double));
        }

        var rowGroups = sourceTable.AsEnumerable()
            .GroupBy(row => string.Join("|", Rows.Select(r => row[r].ToString())));

        foreach (var group in rowGroups)
        {
            DataRow newRow = pivotedTable.NewRow();
            foreach (string rowName in Rows) newRow[rowName] = group.First()[rowName];

            var colGroups = group.GroupBy(row => string.Join("_", Cols.Select(c => row[c].ToString())));

            foreach (var colGroup in colGroups)
            {
                foreach (string dataCol in Data)
                {
                    string targetCol = $"{colGroup.Key}_{dataCol}";

                    // Extract values and weights
                    var entries = colGroup
                        .Where(r => r[dataCol] != DBNull.Value)
                        .Select(r => new {
                            Val = Convert.ToDouble(r[dataCol]),
                            Wgt = (WeightColumn != null && r.Table.Columns.Contains(WeightColumn)) 
                                ? Convert.ToDouble(r[WeightColumn]) : 1.0
                        }).ToList();

                    if (entries.Any())
                    {
                        newRow[targetCol] = CalculateAggregate(
                            entries.Select(e => e.Val).ToList(), 
                            entries.Select(e => e.Wgt).ToList(), 
                            AggregateFunction
                        );
                    }
                }
            }
            pivotedTable.Rows.Add(newRow);
        }
        return pivotedTable;
    }

    private static double CalculateAggregate(List<double> values, List<double> weights, string func)
    {
        switch (func.ToLower())
        {
            case "sum": return values.Sum();
            case "count": return values.Count;
            case "average": return values.Average();
            case "min": return values.Min();
            case "max": return values.Max();
            case "median":
                var sorted = values.OrderBy(n => n).ToList();
                int mid = sorted.Count / 2;
                return (sorted.Count % 2 != 0) ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
            case "weightedaverage":
                double totalWeight = weights.Sum();
                if (totalWeight == 0) return 0;
                return values.Zip(weights, (v, w) => v * w).Sum() / totalWeight;
            case "standarddeviation":
                double avg = values.Average();
                return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
            default: return values.Sum();
        }
    }
}

