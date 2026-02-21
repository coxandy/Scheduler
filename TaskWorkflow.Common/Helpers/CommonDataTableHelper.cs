using System.Data;
using System.Text;
using TaskWorkflow.Common.Tasks;
using System.Security.Cryptography;

namespace TaskWorkflow.Common.Helpers;

public static class CommonDataTableHelper
{
    public static async Task<DataTable> LimitColumns(DataTable dtOriginal, List<string> finalColumns)
    {
        
        return await Task.Run(() =>
        {
            DataTable dtNew = new DataTable(dtOriginal.TableName);

            if (dtOriginal.Columns.Count > 0 && finalColumns != null && finalColumns.Any())
            {
                foreach (var columnName in finalColumns)
                {
                    if (dtOriginal.Columns.Contains(columnName))
                    {
                        var originalCol = dtOriginal.Columns[columnName]!;
                        dtNew.Columns.Add(columnName, originalCol.DataType);
                    }
                }

                // Import the data rows
                foreach (DataRow row in dtOriginal.Rows)
                {
                    DataRow newRow = dtNew.NewRow();
                    foreach (string colName in finalColumns)
                    {
                        if (dtNew.Columns.Contains(colName))
                        {
                            newRow[colName] = row[colName];
                        }
                    }
                    dtNew.Rows.Add(newRow);
                }
            }

            return dtNew;
        });
    }

    public async static Task<DataTable> WhereFilter(DataTable dtOriginal, string filterExpression)
    {
        return await Task.Run(() => {
            DataRow[] rowsToKeep = dtOriginal.Select(filterExpression);
            DataTable dtNew = rowsToKeep.CopyToDataTable();
            dtNew.TableName = dtOriginal.TableName;
            return dtNew;
        });
    }

    
    /// <summary>
    /// Returns the row count of the named DataTable as a string.
    /// Usage in JSON: <fn_runtime _get_datatable_count("MyTable")>
    /// </summary>
    public static string GetDataTableCount(string tableName, TaskContext taskContext)
    {
        var table = taskContext.GetDataTable(tableName)
            ?? throw new KeyNotFoundException($"DataTable '{tableName}' not found in TaskContext.");

        return table.Rows.Count.ToString();
    }

    /// <summary>
    /// Sums all values in the specified numeric column of the named DataTable.
    /// Throws InvalidOperationException if any non-null cell cannot be parsed as a number.
    /// Usage in JSON: <fn_runtime _get_datatable_column_total("MyTable", "MyColumn")>
    /// </summary>
    public static string GetDataTableColumnTotal(string tableName, string columnName, TaskContext taskContext)
    {
        var table = taskContext.GetDataTable(tableName)
            ?? throw new KeyNotFoundException($"DataTable '{tableName}' not found in TaskContext.");

        if (!table.Columns.Contains(columnName))
            throw new ArgumentException($"Column '{columnName}' not found in DataTable '{tableName}'.");

        decimal total = 0;
        foreach (DataRow row in table.Rows)
        {
            var cell = row[columnName];
            if (cell is DBNull || cell is null) continue;

            var cellStr = cell.ToString() ?? string.Empty;
            if (!decimal.TryParse(cellStr, out decimal value))
                throw new InvalidOperationException(
                    $"Column '{columnName}' in DataTable '{tableName}' contains non-numeric value: '{cellStr}'.");

            total += value;
        }

        return total.ToString();
    }

    /// <summary>
    /// Returns a SHA-256 hex hash of all cell values in the named DataTable,
    /// iterating row by row, column by column.
    /// Usage in JSON: <fn_runtime _get_datatable_hash("MyTable")>
    /// </summary>
    public static string GetDataTableHash(string tableName, TaskContext taskContext)
    {
        var table = taskContext.GetDataTable(tableName)
            ?? throw new KeyNotFoundException($"DataTable '{tableName}' not found in TaskContext.");

        var sb = new StringBuilder();
        foreach (DataRow row in table.Rows)
        {
            foreach (var cell in row.ItemArray)
            {
                sb.Append(cell is DBNull ? "NULL" : (cell?.ToString() ?? string.Empty));
                sb.Append('|');
            }
            sb.Append('\n');
        }

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}