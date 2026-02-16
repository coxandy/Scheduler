using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Tasks;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Helpers;
using Xunit;

namespace TaskWorkflow.UnitTests.TaskFactory;

public class TokenReplacementTests
{

    private readonly TaskInstance _instance;
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
            "SchemaDefinition": {
                "version": "v2.1",
                "lastUpdated": "<yyyy-MM-dd>",
                "isDeprecated": false,
                "author": "DevOps Team"
            },
            "ExitDefinition": {
                "isActive": true,
                "success": { "email": true, "to": ["admin@test.com"], "subject": "Task Succeeded", "body": "Completed", "priority": "Normal", "attachments": [] },
                "failure": { "email": true, "to": ["admin@test.com"], "subject": "Task Failed", "body": "Error", "priority": "High", "attachments": [] }
            }
            }
    """;

    
    public TokenReplacementTests()
    {
        _instance = new TaskInstance 
        { 
            EffectiveDate = new DateTime(2026, 10, 5),
            RunId = Guid.CreateVersion7().ToString(),
            IsManual = false,
            EnvironmentName = "Development"
        };
    }

    
    // Helper: ParseJson + DeserializeDefinitionBlocks
    private List<IDefinition> ParseAndDeserialize(string json)
    {
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json, _instance.EffectiveDate, _instance.EnvironmentName);
        VariableDefinition VariableDefinitionBlock = JsonParser.VerifyJson();
        if (VariableDefinitionBlock != null)
        {
            // Assign variables to class
            var variables = VariableDefinitionBlock.Variables;

            // Apply variable replacement to Json
            json = JsonParser.ApplyVariableReplacementsToJson(json, VariableDefinitionBlock);
        }

        // Get final block definition
        return JsonParser.DeserializeDefinitionBlocks(json);
    }

    [Fact]
    public void Json_Token_ReturnsCorrectTokenReplacementValue()
    {
        string json = JsonParsingHelper.ReplaceToken(_json, _instance.EffectiveDate, _instance.EnvironmentName);
        var result = ParseAndDeserialize(json);
        var schemaDef = result[2] as SchemaDefinition;
        Assert.Equal(_instance.EffectiveDate.ToString("yyyy-MM-dd"), schemaDef?.LastUpdated.ToString("yyyy-MM-dd"));
    }
}
