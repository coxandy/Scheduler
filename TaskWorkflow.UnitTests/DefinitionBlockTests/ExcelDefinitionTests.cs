using System.Data;
using ClosedXML.Excel;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.Common.Models.BlockDefinition;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

namespace TaskWorkflow.UnitTests.DefinitionBlockTests;

public class ExcelDefinitionTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    private string CreateTempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");
        _tempFiles.Add(path);
        return path;
    }

    private static string GetExcelDefinitionJson() => """
                "ExcelDefinition": {
                    "Spreadsheets": [
                        {
                            "Filename": "c:\\sheets\\file.xlsx",
                            "Operation": "Write",
                            "Worksheets": [
                                {
                                    "WorksheetName": "ws 1",
                                    "DSTable": "users",
                                    "TopLeft": "A1"
                                },
                                {
                                    "WorksheetName": "ws 2",
                                    "DSTable": "usersuppliers",
                                    "TopLeft": "A1"
                                }
                            ]
                        },
                        {
                            "Filename": "c:\\sheets\\file2.xlsx",
                            "Operation": "Read",
                            "Worksheets": [
                                {
                                    "WorksheetName": "ws 1",
                                    "DSTable": "users",
                                    "TopLeft": "A1"
                                },
                                {
                                    "WorksheetName": "ws 2",
                                    "DSTable": "usersuppliers",
                                    "TopLeft": "A1"
                                }
                            ]
                        }
                    ]
                }
        """;

    // --- Deserialization tests ---

    [Fact]
    public void ExcelDefinition_DeserializesCorrectly()
    {
        var json = $$"""
            {
                {{GetExcelDefinitionJson()}},
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.Equal(2, result.Count);
        var excel = Assert.IsType<ExcelDefinition>(result[0]);
        Assert.NotNull(excel.Spreadsheets);
        Assert.Equal(2, excel.Spreadsheets.Count);
    }

    [Fact]
    public void ExcelDefinition_Spreadsheet_PropertiesAreCorrect()
    {
        var json = $$"""
            {
                {{GetExcelDefinitionJson()}},
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var excel = Assert.IsType<ExcelDefinition>(result[0]);

        Assert.Equal("c:\\sheets\\file.xlsx", excel.Spreadsheets[0].Filename);
        Assert.Equal(eSpreadsheetOperation.Write, excel.Spreadsheets[0].Operation);

        Assert.Equal("c:\\sheets\\file2.xlsx", excel.Spreadsheets[1].Filename);
        Assert.Equal(eSpreadsheetOperation.Read, excel.Spreadsheets[1].Operation);
    }

    [Fact]
    public void ExcelDefinition_Worksheets_DeserializeCorrectly()
    {
        var json = $$"""
            {
                {{GetExcelDefinitionJson()}},
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var excel = Assert.IsType<ExcelDefinition>(result[0]);

        Assert.Equal(2, excel.Spreadsheets[0].Worksheets.Count);
        Assert.Equal(2, excel.Spreadsheets[1].Worksheets.Count);

        Assert.Equal("ws 1", excel.Spreadsheets[0].Worksheets[0].WorksheetName);
        Assert.Equal("users", excel.Spreadsheets[0].Worksheets[0].DSTable);
        Assert.Equal("A1", excel.Spreadsheets[0].Worksheets[0].TopLeft);

        Assert.Equal("ws 2", excel.Spreadsheets[0].Worksheets[1].WorksheetName);
        Assert.Equal("usersuppliers", excel.Spreadsheets[0].Worksheets[1].DSTable);
        Assert.Equal("A1", excel.Spreadsheets[0].Worksheets[1].TopLeft);
    }

    [Fact]
    public void ExcelDefinition_WithNumericSuffix_SupportsMultiple()
    {
        var json = $$"""
            {
                "ExcelDefinition1": {
                    "Spreadsheets": [
                        {
                            "Filename": "c:\\sheets\\file.xlsx",
                            "Operation": "Write",
                            "Worksheets": [
                                { "WorksheetName": "ws 1", "DSTable": "users", "TopLeft": "A1" }
                            ]
                        }
                    ]
                },
                "ExcelDefinition2": {
                    "Spreadsheets": [
                        {
                            "Filename": "c:\\sheets\\file2.xlsx",
                            "Operation": "Read",
                            "Worksheets": [
                                { "WorksheetName": "ws 1", "DSTable": "orders", "TopLeft": "B2" }
                            ]
                        }
                    ]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.Equal(3, result.Count);
        var excel1 = Assert.IsType<ExcelDefinition>(result[0]);
        var excel2 = Assert.IsType<ExcelDefinition>(result[1]);
        Assert.Equal("c:\\sheets\\file.xlsx", excel1.Spreadsheets[0].Filename);
        Assert.Equal("c:\\sheets\\file2.xlsx", excel2.Spreadsheets[0].Filename);
        Assert.Equal("ExcelDefinition1", excel1.BlockName);
        Assert.Equal("ExcelDefinition2", excel2.BlockName);
    }

    [Fact]
    public void ExcelDefinition_WithVariableReplacement_ReplacesTokens()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": {
                        "<@@FilePath@@>": "c:/output/report.xlsx",
                        "<@@SheetName@@>": "Summary"
                    },
                    "IsActive": true
                },
                "ExcelDefinition": {
                    "Spreadsheets": [
                        {
                            "Filename": "<@@FilePath@@>",
                            "Operation": "Write",
                            "Worksheets": [
                                { "WorksheetName": "<@@SheetName@@>", "DSTable": "report_data", "TopLeft": "A1" }
                            ]
                        }
                    ]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var excel = result[1] as ExcelDefinition;

        Assert.NotNull(excel);
        Assert.Equal("c:/output/report.xlsx", excel.Spreadsheets[0].Filename);
        Assert.Equal("Summary", excel.Spreadsheets[0].Worksheets[0].WorksheetName);
    }

    [Fact]
    public void ExcelDefinition_IsActiveDefaultsToTrue()
    {
        var excel = new ExcelDefinition();
        Assert.True(excel.IsActive);
    }

    [Fact]
    public void ExcelDefinition_IncludeHeader_DeserializesCorrectly()
    {
        var json = $$"""
            {
                "ExcelDefinition": {
                    "Spreadsheets": [
                        {
                            "Filename": "test.xlsx",
                            "Operation": "Read",
                            "Worksheets": [
                                { "WorksheetName": "Sheet1", "DSTable": "data", "TopLeft": "A1", "IncludeHeader": true }
                            ]
                        }
                    ]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var excel = Assert.IsType<ExcelDefinition>(result[0]);
        Assert.True(excel.Spreadsheets[0].Worksheets[0].IncludeHeader);
    }

    // --- Read operation tests ---

    [Fact]
    public async Task ReadSpreadsheet_WithHeader_UsesHeaderAsColumnNames()
    {
        var filePath = CreateTempFile();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add("Sheet1");
            ws.Cell("A1").Value = "Name";
            ws.Cell("B1").Value = "Age";
            ws.Cell("A2").Value = "Alice";
            ws.Cell("B2").Value = "30";
            ws.Cell("A3").Value = "Bob";
            ws.Cell("B3").Value = "25";
            wb.SaveAs(filePath);
        }

        var taskContext = new TaskContext();
        var excelDef = new ExcelDefinition
        {
            Spreadsheets =
            [
                new Spreadsheet
                {
                    Filename = filePath,
                    Operation = eSpreadsheetOperation.Read,
                    Worksheets = [new Worksheet { WorksheetName = "Sheet1", DSTable = "People", TopLeft = "A1", IncludeHeader = true }]
                }
            ]
        };

        var taskInstance = GetTaskInstance();
        await excelDef.RunDefinitionBlockAsync(taskInstance, null!, taskContext);

        var dt = taskContext.GetDataTable("People");
        Assert.NotNull(dt);
        Assert.Equal(2, dt.Columns.Count);
        Assert.Equal("Name", dt.Columns[0].ColumnName);
        Assert.Equal("Age", dt.Columns[1].ColumnName);
        Assert.Equal(2, dt.Rows.Count);
        Assert.Equal("Alice", dt.Rows[0]["Name"]);
        Assert.Equal("30", dt.Rows[0]["Age"]);
        Assert.Equal("Bob", dt.Rows[1]["Name"]);
    }

    [Fact]
    public async Task ReadSpreadsheet_WithoutHeader_GeneratesColumnNames()
    {
        var filePath = CreateTempFile();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add("Sheet1");
            ws.Cell("A1").Value = "Alice";
            ws.Cell("B1").Value = "30";
            ws.Cell("A2").Value = "Bob";
            ws.Cell("B2").Value = "25";
            wb.SaveAs(filePath);
        }

        var taskContext = new TaskContext();
        var excelDef = new ExcelDefinition
        {
            Spreadsheets =
            [
                new Spreadsheet
                {
                    Filename = filePath,
                    Operation = eSpreadsheetOperation.Read,
                    Worksheets = [new Worksheet { WorksheetName = "Sheet1", DSTable = "People", TopLeft = "A1", IncludeHeader = false }]
                }
            ]
        };

        var taskInstance = GetTaskInstance();
        await excelDef.RunDefinitionBlockAsync(taskInstance, null!, taskContext);

        var dt = taskContext.GetDataTable("People");
        Assert.NotNull(dt);
        Assert.Equal("Column1", dt.Columns[0].ColumnName);
        Assert.Equal("Column2", dt.Columns[1].ColumnName);
        Assert.Equal(2, dt.Rows.Count);
        Assert.Equal("Alice", dt.Rows[0][0]);
    }

    [Fact]
    public async Task ReadSpreadsheet_WithTopLeftOffset_ReadsFromCorrectPosition()
    {
        var filePath = CreateTempFile();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add("Sheet1");
            ws.Cell("A1").Value = "Ignored";
            ws.Cell("B2").Value = "Name";
            ws.Cell("C2").Value = "Value";
            ws.Cell("B3").Value = "Item1";
            ws.Cell("C3").Value = "100";
            wb.SaveAs(filePath);
        }

        var taskContext = new TaskContext();
        var excelDef = new ExcelDefinition
        {
            Spreadsheets =
            [
                new Spreadsheet
                {
                    Filename = filePath,
                    Operation = eSpreadsheetOperation.Read,
                    Worksheets = [new Worksheet { WorksheetName = "Sheet1", DSTable = "Items", TopLeft = "B2", IncludeHeader = true }]
                }
            ]
        };

        var taskInstance = GetTaskInstance();
        await excelDef.RunDefinitionBlockAsync(taskInstance, null!, taskContext);

        var dt = taskContext.GetDataTable("Items");
        Assert.NotNull(dt);
        Assert.Equal("Name", dt.Columns[0].ColumnName);
        Assert.Equal("Value", dt.Columns[1].ColumnName);
        Assert.Equal(1, dt.Rows.Count);
        Assert.Equal("Item1", dt.Rows[0]["Name"]);
        Assert.Equal("100", dt.Rows[0]["Value"]);
    }

    [Fact]
    public async Task ReadSpreadsheet_EmptyWorksheet_ReturnsEmptyDataTable()
    {
        var filePath = CreateTempFile();
        using (var wb = new XLWorkbook())
        {
            wb.Worksheets.Add("Sheet1");
            wb.SaveAs(filePath);
        }

        var taskContext = new TaskContext();
        var excelDef = new ExcelDefinition
        {
            Spreadsheets =
            [
                new Spreadsheet
                {
                    Filename = filePath,
                    Operation = eSpreadsheetOperation.Read,
                    Worksheets = [new Worksheet { WorksheetName = "Sheet1", DSTable = "Empty", TopLeft = "A1", IncludeHeader = true }]
                }
            ]
        };

        var taskInstance = GetTaskInstance();
        await excelDef.RunDefinitionBlockAsync(taskInstance, null!, taskContext);

        var dt = taskContext.GetDataTable("Empty");
        Assert.NotNull(dt);
        Assert.Equal(0, dt.Rows.Count);
        Assert.Equal(0, dt.Columns.Count);
    }

    // --- Write operation tests ---

    [Fact]
    public async Task WriteSpreadsheet_WithHeader_WritesHeaderAndData()
    {
        var filePath = CreateTempFile();
        var taskContext = new TaskContext();

        var dt = new DataTable("Orders");
        dt.Columns.Add("Product");
        dt.Columns.Add("Qty");
        dt.Rows.Add("Widget", "10");
        dt.Rows.Add("Gadget", "5");
        taskContext.AddDataTable(dt);

        var excelDef = new ExcelDefinition
        {
            Spreadsheets =
            [
                new Spreadsheet
                {
                    Filename = filePath,
                    Operation = eSpreadsheetOperation.Write,
                    Worksheets = [new Worksheet { WorksheetName = "OrderSheet", DSTable = "Orders", TopLeft = "A1", IncludeHeader = true }]
                }
            ]
        };

        var taskInstance = GetTaskInstance();
        await excelDef.RunDefinitionBlockAsync(taskInstance, null!, taskContext);

        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheet("OrderSheet");
        Assert.Equal("Product", ws.Cell("A1").GetString());
        Assert.Equal("Qty", ws.Cell("B1").GetString());
        Assert.Equal("Widget", ws.Cell("A2").GetString());
        Assert.Equal("10", ws.Cell("B2").GetString());
        Assert.Equal("Gadget", ws.Cell("A3").GetString());
        Assert.Equal("5", ws.Cell("B3").GetString());
    }

    [Fact]
    public async Task WriteSpreadsheet_WithoutHeader_WritesDataOnly()
    {
        var filePath = CreateTempFile();
        var taskContext = new TaskContext();

        var dt = new DataTable("Data");
        dt.Columns.Add("Col1");
        dt.Columns.Add("Col2");
        dt.Rows.Add("A", "B");
        taskContext.AddDataTable(dt);

        var excelDef = new ExcelDefinition
        {
            Spreadsheets =
            [
                new Spreadsheet
                {
                    Filename = filePath,
                    Operation = eSpreadsheetOperation.Write,
                    Worksheets = [new Worksheet { WorksheetName = "Sheet1", DSTable = "Data", TopLeft = "A1", IncludeHeader = false }]
                }
            ]
        };

        var taskInstance = GetTaskInstance();
        await excelDef.RunDefinitionBlockAsync(taskInstance, null!, taskContext);

        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheet("Sheet1");
        Assert.Equal("A", ws.Cell("A1").GetString());
        Assert.Equal("B", ws.Cell("B1").GetString());
    }

    [Fact]
    public async Task WriteSpreadsheet_WithTopLeftOffset_WritesAtCorrectPosition()
    {
        var filePath = CreateTempFile();
        var taskContext = new TaskContext();

        var dt = new DataTable("Data");
        dt.Columns.Add("Name");
        dt.Rows.Add("Test");
        taskContext.AddDataTable(dt);

        var excelDef = new ExcelDefinition
        {
            Spreadsheets =
            [
                new Spreadsheet
                {
                    Filename = filePath,
                    Operation = eSpreadsheetOperation.Write,
                    Worksheets = [new Worksheet { WorksheetName = "Sheet1", DSTable = "Data", TopLeft = "C3", IncludeHeader = true }]
                }
            ]
        };

        var taskInstance = GetTaskInstance();
        await excelDef.RunDefinitionBlockAsync(taskInstance, null!, taskContext);

        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheet("Sheet1");
        Assert.Equal("Name", ws.Cell("C3").GetString());
        Assert.Equal("Test", ws.Cell("C4").GetString());
        Assert.True(ws.Cell("A1").IsEmpty());
    }

    [Fact]
    public async Task WriteSpreadsheet_MissingDataTable_ThrowsInvalidOperationException()
    {
        var filePath = CreateTempFile();
        var taskContext = new TaskContext();

        var excelDef = new ExcelDefinition
        {
            Spreadsheets =
            [
                new Spreadsheet
                {
                    Filename = filePath,
                    Operation = eSpreadsheetOperation.Write,
                    Worksheets = [new Worksheet { WorksheetName = "Sheet1", DSTable = "NonExistent", TopLeft = "A1", IncludeHeader = false }]
                }
            ]
        };

        var taskInstance = GetTaskInstance();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            excelDef.RunDefinitionBlockAsync(taskInstance, null!, taskContext));
    }

    // --- Round-trip test ---

    [Fact]
    public async Task WriteAndRead_RoundTrip_DataIsPreserved()
    {
        var filePath = CreateTempFile();

        // Write
        var writeContext = new TaskContext();
        var dt = new DataTable("RoundTrip");
        dt.Columns.Add("Id");
        dt.Columns.Add("Value");
        dt.Rows.Add("1", "Alpha");
        dt.Rows.Add("2", "Beta");
        writeContext.AddDataTable(dt);

        var writeDef = new ExcelDefinition
        {
            Spreadsheets =
            [
                new Spreadsheet
                {
                    Filename = filePath,
                    Operation = eSpreadsheetOperation.Write,
                    Worksheets = [new Worksheet { WorksheetName = "Data", DSTable = "RoundTrip", TopLeft = "A1", IncludeHeader = true }]
                }
            ]
        };

        var taskInstance = GetTaskInstance();
        await writeDef.RunDefinitionBlockAsync(taskInstance, null!, writeContext);

        // Read
        var readContext = new TaskContext();
        var readDef = new ExcelDefinition
        {
            Spreadsheets =
            [
                new Spreadsheet
                {
                    Filename = filePath,
                    Operation = eSpreadsheetOperation.Read,
                    Worksheets = [new Worksheet { WorksheetName = "Data", DSTable = "ReadBack", TopLeft = "A1", IncludeHeader = true }]
                }
            ]
        };

        await readDef.RunDefinitionBlockAsync(taskInstance, null!, readContext);

        var readDt = readContext.GetDataTable("ReadBack");
        Assert.NotNull(readDt);
        Assert.Equal(2, readDt.Columns.Count);
        Assert.Equal("Id", readDt.Columns[0].ColumnName);
        Assert.Equal("Value", readDt.Columns[1].ColumnName);
        Assert.Equal(2, readDt.Rows.Count);
        Assert.Equal("1", readDt.Rows[0]["Id"]);
        Assert.Equal("Alpha", readDt.Rows[0]["Value"]);
        Assert.Equal("2", readDt.Rows[1]["Id"]);
        Assert.Equal("Beta", readDt.Rows[1]["Value"]);
    }
}
