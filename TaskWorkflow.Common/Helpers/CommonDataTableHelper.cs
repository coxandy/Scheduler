using System.Data;

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
}