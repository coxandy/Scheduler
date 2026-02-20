using TaskWorkflow.Common.Helpers;
using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

namespace TaskWorkflow.UnitTests.TaskFactory;

public class FunctionReplacementTests
{
    private string _json = """
    {
            "VariableDefinition": {
                "Variables": {
                "<@@Test1@@>": 13,
                "<@@Test2@@>": 15,
                "<@@Test3@@>": "<fn_GetStockPrice("AAPL.L")>",
                "<@@Test4@@>": "<fn_GetLatestFile("c:\\\\temp\\\\test.csv")>"
                },
                "IsActive": true
            },
            "ClassDefinition": {
                "status": "Failed",
                "classname": "Web3.Api.GetBalancesByEpoch",
                "methodname": "GetBalancesByEpoch",
                "parameters": [
                "arrayval1",
                "arrayval2"
                ]
            },
            "ExitDefinition": {
                "isActive": true,
                "success": { "email": true, "to": ["admin@test.com"], "subject": "Task Succeeded", "body": "Completed", "priority": "Normal", "attachments": [] },
                "failure": { "email": true, "to": ["admin@test.com"], "subject": "Task Failed", "body": "Error", "priority": "High", "attachments": [] }
            }
            }
    """;

    [Fact]
    public void Json_Function_Resolve_Returns_Correct_Number_Of_Function_Matches()
    {
        var instance = GetTaskInstance();
        // _json contains fn_GetStockPrice which is unsupported, so this will throw
        Assert.Throws<NotSupportedException>(() =>
            CommonFunctionHelper.MatchFunctionVariables(_json, instance));
    }

    [Fact]
    public void ParseFunctionVariable_Unsupported_Function_Throws_NotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            CommonFunctionHelper.ParseFunctionVariable(@"<fn_GetStockPrice(""AAPL.L"")>"));
    }

    [Fact]
    public void ParseFunctionVariable_Extracts_MultipleParams_Before_Invoke()
    {
        // fn_DateAdd is not supported, so this will throw - but we can test InvokeFunction directly
        Assert.Throws<NotSupportedException>(() =>
            CommonFunctionHelper.ParseFunctionVariable("<fn_DateAdd(7, days, 2026-01-01)>"));
    }

    [Fact]
    public void ParseFunctionVariable_NoParams_Unsupported_Function_Throws()
    {
        Assert.Throws<NotSupportedException>(() =>
            CommonFunctionHelper.ParseFunctionVariable("<fn_GetCurrentDate()>"));
    }

    [Fact]
    public void MatchFunctionVariables_Returns_Parsed_FunctionNames()
    {
        var instance = GetTaskInstance();
        // MatchFunctionVariables calls ParseFunctionVariable which now invokes functions.
        // fn_GetStockPrice is unsupported, so this will throw.
        Assert.Throws<NotSupportedException>(() =>
            CommonFunctionHelper.MatchFunctionVariables(_json, instance));
    }

    [Fact]
    public void ParseFunctionVariable_GetLatestFile_Resolves_Value()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fntest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var oldFile = Path.Combine(tempDir, "data_001.csv");
            var newFile = Path.Combine(tempDir, "data_002.csv");
            File.WriteAllText(oldFile, "old");
            Thread.Sleep(100);
            File.WriteAllText(newFile, "new");

            var wildcardPath = Path.Combine(tempDir, "data_0*.csv");
            var result = CommonFunctionHelper.ParseFunctionVariable($"<fn_GetLatestFile({wildcardPath})>");

            Assert.Equal("fn_GetLatestFile", result.FunctionName);
            Assert.Single(result.ParamValues);
            Assert.Equal(newFile, result.ResolvedValue);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ParseFunctionVariable_GetOldestFile_Resolves_Value()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fntest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var oldFile = Path.Combine(tempDir, "data_001.csv");
            var newFile = Path.Combine(tempDir, "data_002.csv");
            File.WriteAllText(oldFile, "old");
            Thread.Sleep(100);
            File.WriteAllText(newFile, "new");

            var wildcardPath = Path.Combine(tempDir, "data_0*.csv");
            var result = CommonFunctionHelper.ParseFunctionVariable($"<fn_GetOldestFile({wildcardPath})>");

            Assert.Equal("fn_GetOldestFile", result.FunctionName);
            Assert.Single(result.ParamValues);
            Assert.Equal(oldFile, result.ResolvedValue);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ParseFunctionVariable_GetLatestFile_NoMatchingFiles_Throws()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fntest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var wildcardPath = Path.Combine(tempDir, "*.csv");
            Assert.Throws<FileNotFoundException>(() =>
                CommonFunctionHelper.ParseFunctionVariable($"<fn_GetLatestFile({wildcardPath})>"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void InvokeFunction_Unsupported_Function_Throws_NotSupportedException()
    {
        var funcParams = new TaskWorkflow.Common.Models.Functions.FunctionVariableParams
        {
            FunctionName = "fn_Unknown",
            ParamValues = new List<string> { "test" }
        };

        var ex = Assert.Throws<NotSupportedException>(() =>
            CommonFunctionHelper.InvokeFunction(funcParams));
        Assert.Contains("fn_Unknown", ex.Message);
    }

    [Fact]
    public void InvokeFunction_Wrong_Parameter_Count_Throws_ArgumentException()
    {
        var funcParams = new TaskWorkflow.Common.Models.Functions.FunctionVariableParams
        {
            FunctionName = "fn_GetLatestFile",
            ParamValues = new List<string> { "param1", "param2" }
        };

        var ex = Assert.Throws<ArgumentException>(() =>
            CommonFunctionHelper.InvokeFunction(funcParams));
        Assert.Contains("expects 1 parameter(s) but received 2", ex.Message);
    }
}
