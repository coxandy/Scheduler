using ClosedXML.Excel;
using TaskWorkflow.TaskFactory.Tasks;

namespace TaskWorkflow.RegressionTests.GroqDefinitionTests;

public class GroqEthereumAnalysisTests : IDisposable
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
    public async Task GroqEthereumAnalysis_EmailWithDataTable_RegressionTest()
    {
        // Arrange
        var xlsxPath = CreateTempFile(".xlsx");
        var bannerFilePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Images");

        var prompt = "Provide a market analysis for Ethereum (ETH). " +
                     "Return a JSON object with a 'data' property containing an array of objects. " +
                     "Each object should have these columns: " +
                     "indicator (string - the market indicator name), " +
                     "value (string - the current indicative value or status), " +
                     "signal (string - one of: Buy, Sell, Hold, Neutral), " +
                     "commentary (string - brief explanation of what this indicator suggests). " +
                     "The first three rows MUST be: " +
                     "1) 'Current Price (USD)' with the current ETH price in US dollars, " +
                     "2) 'Current Price (GBP)' with the current ETH price in British pounds, " +
                     "3) 'VWAP' (Volume Weighted Average Price) with its current value. " +
                     "Then include at least 6 additional common technical indicators such as " +
                     "RSI, MACD, Moving Average 50d, Moving Average 200d, Bollinger Bands, Volume Trend, Support Level, Resistance Level. " +
                     "Base your response on general market knowledge.";

        var taskJson = $$"""
            {
                "GroqDefinition": {
                    "Questions": [
                        {
                            "Prompt": "{{prompt.Replace("\"", "\\\"")}}",
                            "DSTableName": "EthMarketIndicators",
                            "Temperature": 0.3,
                            "MaxTokens": 2048
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
                                    "WorksheetName": "ETH Indicators",
                                    "DSTable": "EthMarketIndicators",
                                    "TopLeft": "A1",
                                    "IncludeHeader": true
                                }
                            ]
                        }
                    ]
                },
                "EmailDefinition": {
                    "Messages": [
                        {
                            "IncludeBanner": true,
                            "BannerFilePath": "{{bannerFilePath.Replace("\\", "\\\\")}}",
                            "BannerFileName": "emailbanner.png",
                            "BannerOverlayText": "Ethereum Analysis",
                            "To": ["coxandy@yahoo.com"],
                            "Subject": "Ethereum (ETH) Market Indicator Analysis",
                            "Body": "<h3>Ethereum Market Analysis</h3><p><b>Question asked:</b> {{prompt.Replace("\"", "&quot;")}}</p><p>The following market indicators were returned by Groq AI:</p><<DATATABLE: EthMarketIndicators>><br/><p><i>Data is also attached as a spreadsheet.</i></p>",
                            "Priority": "Normal",
                            "Attachments": ["{{xlsxPath.Replace("\\", "\\\\")}}"]
                        }
                    ]
                },
                "ExitDefinition": {
                    "Success": { "SendEmail": false, "To": [], "CC": [], "BCC": [], "Subject": "", "Body": "", "Priority": "Normal", "Attachments": [] },
                    "Failure": { "SendEmail": false, "To": [], "CC": [], "BCC": [], "Subject": "", "Body": "", "Priority": "Normal", "Attachments": [] }
                }
            }
            """;

        // Act — full pipeline: Groq API -> DataTable -> Excel -> Email with inline table + attachment
        var taskInstance = TestHelper.GetTaskInstance();
        var task = new GenericWorkflowTask(taskJson, taskInstance, TestHelper.GetServiceProvider());
        bool success = await task.Run();

        // Assert — task completed successfully
        Assert.True(success);

        // Assert — Excel file was created with data
        Assert.True(File.Exists(xlsxPath), "Excel file should have been created");

        using var workbook = new XLWorkbook(xlsxPath);
        var ws = workbook.Worksheet("ETH Indicators");
        Assert.False(ws.Cell("A1").IsEmpty(), "Worksheet should contain header data");
        Assert.False(ws.Cell("A2").IsEmpty(), "Worksheet should contain at least one data row");
    }
}
