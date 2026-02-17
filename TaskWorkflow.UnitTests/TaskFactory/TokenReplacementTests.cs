using TaskWorkflow.Common.Helpers;
using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

namespace TaskWorkflow.UnitTests.TaskFactory;

public class TokenReplacementTests
{
    private string _json = """
    {
            "VariableDefinition": {
                "Variables": {
                "<@@Test1@@>": 13,
                "<@@Test2@@>": 15,
                "<@@Test3@@>": "andy",
                "<@@Test4@@>": "58"
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
    public void Json_Token_ReturnsCorrectTokenReplacementValue()
    {
        var instance = GetTaskInstance();
        string json = JsonParsingHelper.ReplaceToken(_json, instance.EffectiveDate, instance.EnvironmentName);
        var result = ParseAndDeserialize(json);
        Assert.Equal(3, result.Count);
    }
}
