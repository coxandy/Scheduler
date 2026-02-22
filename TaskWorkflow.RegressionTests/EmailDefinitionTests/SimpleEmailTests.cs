using TaskWorkflow.TaskFactory.Tasks;

namespace TaskWorkflow.RegressionTests.ExcelDefinitionTests;

public class SimpleEmailTests : IDisposable
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

    // Write test .csv file
    private string CreateTempFile(string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
        _tempFiles.Add(path);
        return path;
    }
    
    [Fact]
    public async Task SimpleEmail_With_Banner_RegressionTest()
    {
        // Arrange — dynamically create temp file
        var csvPath = CreateTempFile(".csv");

        var csvContent = "Name,Age,City\nAlice,30,London\nBob,25,Manchester\nCharlie,35,Birmingham";
        await File.WriteAllTextAsync(csvPath, csvContent);

        // Build task JSON replicating real workflow
        var bannerFilePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Images");

        var taskJson = $$"""
            {
                "DatasourceDefinition": {
                    "DataSources": [
                        {
                            "Type": "CsvFile",
                            "DSTableName": "CsvData",
                            "CsvFilePath": "{{Path.GetDirectoryName(csvPath)!.Replace("\\", "\\\\")}}",
                            "CsvFileName": "{{Path.GetFileName(csvPath)}}",
                            "HasFileHeader": true,
                            "CsvFileDelimiter": ","
                        }
                    ]
                },
                "EmailDefinition": {
                    "Messages": [
                        {
                            "IncludeBanner": true,
                            "BannerFilePath": "{{bannerFilePath.Replace("\\", "\\\\")}}",
                            "BannerFileName": "emailbanner.png",
                            "BannerOverlayText": "Test email overlay",
                            "To": ["coxandy@yahoo.com"],
                            "Subject": "Test email",
                            "Body": "\nThis is a test email.\n\nRgds, Andrew Cox",
                            "Priority": "High",
                            "Attachments": ["{{csvPath.Replace("\\", "\\\\")}}"]
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

        // Assert — task executed succssfully
        Assert.True(success);
    }
}
