using Serilog;
using System.Data;
using ClosedXML.Excel;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.Common.Helpers;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.BlockDefinition;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class ExcelDefinition: IDefinition
{
    public bool IsActive { get; set; } = true;
    public string BlockName{ get; set; } = String.Empty;
    public eOnError OnError { get; set; } = eOnError.AbortTaskAndReportError;
    public eTaskStatus Status { get; set; }

    public List<Spreadsheet> Spreadsheets { get; set; } = new();

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance, IServiceProvider serviceProvider, TaskContext taskContext)
    {
        Log.Debug($"RunDefinitionBlockAsync() - RunId: {taskInstance.RunId}  Running {GetType().Name}..");
        foreach (var ss in Spreadsheets)
        {
            Console.Write($"RunId: {taskInstance.RunId}  Running {GetType().Name}..");
            await ProcessSpreadsheet(ss, taskContext);
        }
    }

    private async Task ProcessSpreadsheet(Spreadsheet ss, TaskContext taskContext)
    {
        await Task.Run(() =>
        {
            switch (ss.Operation)
            {
                case eSpreadsheetOperation.Read:
                    ReadSpreadsheet(ss, taskContext);
                    break;
                case eSpreadsheetOperation.Write:
                    WriteSpreadsheet(ss, taskContext);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ss.Operation), $"Unknown spreadsheet operation: {ss.Operation}");
            }
        });
    }

    private static void ReadSpreadsheet(Spreadsheet ss, TaskContext taskContext)
    {
        using var workbook = new XLWorkbook(ss.Filename);
        foreach (var ws in ss.Worksheets)
        {
            var xlWorksheet = workbook.Worksheet(ws.WorksheetName);
            var startCell = CommonExcelHelper.ParseTopLeft(ws.TopLeft);

            var rangeUsed = xlWorksheet.RangeUsed();
            if (rangeUsed == null)
            {
                taskContext.AddDataTable(new DataTable(ws.DSTable));
                continue;
            }

            int lastRow = rangeUsed.LastRow().RowNumber();
            int lastCol = rangeUsed.LastColumn().ColumnNumber();

            if (startCell.Row > lastRow || startCell.Column > lastCol)
            {
                taskContext.AddDataTable(new DataTable(ws.DSTable));
                continue;
            }

            var dt = new DataTable();
            int dataStartRow = startCell.Row;

            // Build columns
            for (int col = startCell.Column; col <= lastCol; col++)
            {
                if (ws.IncludeHeader)
                {
                    var headerValue = xlWorksheet.Cell(startCell.Row, col).GetString();
                    dt.Columns.Add(string.IsNullOrWhiteSpace(headerValue) ? $"Column{col - startCell.Column + 1}" : headerValue);
                }
                else
                {
                    dt.Columns.Add($"Column{col - startCell.Column + 1}");
                }
            }

            if (ws.IncludeHeader)
                dataStartRow = startCell.Row + 1;

            // Read data rows
            for (int row = dataStartRow; row <= lastRow; row++)
            {
                var dataRow = dt.NewRow();
                for (int col = startCell.Column; col <= lastCol; col++)
                    dataRow[col - startCell.Column] = xlWorksheet.Cell(row, col).Value.ToString();
                dt.Rows.Add(dataRow);
            }

            dt.TableName = ws.DSTable;
            taskContext.AddDataTable(dt);
        }
    }

    private static void WriteSpreadsheet(Spreadsheet ss, TaskContext taskContext)
    {
        using var workbook = File.Exists(ss.Filename) ? new XLWorkbook(ss.Filename) : new XLWorkbook();

        foreach (var ws in ss.Worksheets)
        {
            var dt = taskContext.GetDataTable(ws.DSTable)
                ?? throw new InvalidOperationException($"DataTable '{ws.DSTable}' not found in TaskContext.");

            var xlWorksheet = workbook.Worksheets.Any(s => s.Name == ws.WorksheetName)
                ? workbook.Worksheet(ws.WorksheetName)
                : workbook.Worksheets.Add(ws.WorksheetName);

            var startCell = CommonExcelHelper.ParseTopLeft(ws.TopLeft);
            var cell = xlWorksheet.Cell(startCell.Row, startCell.Column);

            if (ws.IncludeHeader)
                cell.InsertTable(dt);
            else
                cell.InsertData(dt);
        }

        workbook.SaveAs(ss.Filename);
    }
}
