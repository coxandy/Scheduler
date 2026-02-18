using TaskWorkflow.TaskFactory.Tasks;

namespace TaskWorkflow.RegressionTests.ExitDefinitionTests;

public class TaskFailureTests
{
    [Fact]
    public async Task DatasourceError_AbortTaskAndReportError_SendsFailureEmail()
    {
        // Arrange — DatasourceDefinition points to a non-existent CSV file
        // OnError defaults to AbortTaskAndReportError, which triggers error email then re-throws
        var bannerFilePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Images");

        var taskJson = $$"""
            {
                "DatasourceDefinition": {
                    "OnError": "AbortTaskAndReportError",
                    "DataSources": [
                        {
                            "Type": "CsvFile",
                            "DSTableName": "CsvData",
                            "CsvFilePath": "C:\\NonExistent\\Path",
                            "CsvFileName": "missing.csv",
                            "CsvFileHeader": true,
                            "CsvFileDelimiter": ","
                        }
                    ]
                },
                "ExitDefinition": {
                    "Success": { "SendEmail": false, "To": [], "CC": [], "BCC": [], "Subject": "", "Body": "", "Priority": "Normal", "Attachments": [] },
                    "Failure": {
                        "SendEmail": true,
                        "IncludeBanner": true,
                        "BannerFilePath": "{{bannerFilePath.Replace("\\", "\\\\")}}",
                        "BannerFileName": "erroremailbanner.png",
                        "BannerOverlayText": "Task Failure",
                        "To": ["coxandy@yahoo.com"],
                        "CC": [],
                        "BCC": [],
                        "Subject": "Regression Test — Task Failure",
                        "Body": "\nThis error email was sent by the TaskFailureTests regression test.\n\nThe DatasourceDefinition block attempted to read a non-existent CSV file.",
                        "Priority": "High",
                        "Attachments": []
                    }
                }
            }
            """;

        // Act & Assert — task throws after sending the failure email
        var taskInstance = TestHelper.GetTaskInstance();
        taskInstance.Instance.TaskName = "TaskFailureTests";
        taskInstance.Instance.TaskId = 999;

        var task = new GenericWorkflowTask(taskJson, taskInstance, TestHelper.GetServiceProvider());
        await Assert.ThrowsAsync<FileNotFoundException>(async () => await task.Run());
    }
}
