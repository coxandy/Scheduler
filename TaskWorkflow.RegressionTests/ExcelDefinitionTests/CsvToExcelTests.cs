using ClosedXML.Excel;
using TaskWorkflow.TaskFactory.Tasks;

namespace TaskWorkflow.RegressionTests.ExcelDefinitionTests;

public class CsvToExcelTests : IDisposable
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

    private string CreateTempFile(string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
        _tempFiles.Add(path);
        return path;
    }

    [Fact]
    public async Task CsvToExcel_DataIsPreserved_RegressionTest()
    {
        // Arrange — dynamically create temp files
        var csvPath = CreateTempFile(".csv");
        var xlsxPath = CreateTempFile(".xlsx");

        // Write test .csv file
        var csvContent = "Name,Age,City\nAlice,30,London\nBob,25,Manchester\nCharlie,35,Birmingham";
        await File.WriteAllTextAsync(csvPath, csvContent);

        // Build task JSON replicating real workflow
        var taskJson = $$"""
            {
                "DatasourceDefinition": {
                    "DataSources": [
                        {
                            "Type": "CsvFile",
                            "DSTableName": "CsvData",
                            "CsvFilePath": "{{Path.GetDirectoryName(csvPath)!.Replace("\\", "\\\\")}}",
                            "CsvFileName": "{{Path.GetFileName(csvPath)}}",
                            "CsvFileHeader": true,
                            "CsvFileDelimiter": ","
                        }
                    ]
                },
                "ExcelDefinition": {
                    "Spreadsheets": [
                        {
                            "Filename": "{{xlsxPath.Replace("\\", "\\\\")}}",
                            "Operation": "Write",
                            "Worksheets": [
                                {
                                    "WorksheetName": "CsvData",
                                    "DSTable": "CsvData",
                                    "TopLeft": "A1",
                                    "IncludeHeader": true
                                }
                            ]
                        }
                    ]
                },
                "ExitDefinition": {
                    "Success": { "Email": false, "To": [], "CC": [], "BCC": [], "Subject": "", "Body": "", "Priority": "Normal", "Attachments": [] },
                    "Failure": { "Email": false, "To": [], "CC": [], "BCC": [], "Subject": "", "Body": "", "Priority": "Normal", "Attachments": [] }
                }
            }
            """;

        // Act — run the full task workflow (parse JSON, deserialize blocks, execute each)
        var taskInstance = TestHelper.GetTaskInstance();
        var task = new GenericWorkflowTask(taskJson, taskInstance, TestHelper.GetServiceProvider());
        bool success = await task.Run();

        // Assert — read back the Excel file and verify data matches the CSV
        Assert.True(File.Exists(xlsxPath), "Excel file should have been created");

        using var workbook = new XLWorkbook(xlsxPath);
        var ws = workbook.Worksheet("CsvData");

        // Verify headers
        Assert.Equal("Name", ws.Cell("A1").GetString());
        Assert.Equal("Age", ws.Cell("B1").GetString());
        Assert.Equal("City", ws.Cell("C1").GetString());

        // Verify data rows
        Assert.Equal("Alice", ws.Cell("A2").GetString());
        Assert.Equal("30", ws.Cell("B2").GetString());
        Assert.Equal("London", ws.Cell("C2").GetString());

        Assert.Equal("Bob", ws.Cell("A3").GetString());
        Assert.Equal("25", ws.Cell("B3").GetString());
        Assert.Equal("Manchester", ws.Cell("C3").GetString());

        Assert.Equal("Charlie", ws.Cell("A4").GetString());
        Assert.Equal("35", ws.Cell("B4").GetString());
        Assert.Equal("Birmingham", ws.Cell("C4").GetString());

        // Verify no extra rows
        Assert.True(ws.Cell("A5").IsEmpty());
    }
}
