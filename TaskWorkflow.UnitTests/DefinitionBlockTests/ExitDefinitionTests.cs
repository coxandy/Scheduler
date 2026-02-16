using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Tasks;
using TaskWorkflow.Common.Models;
using Xunit;

namespace TaskWorkflow.UnitTests.DefinitionBlockTests;

public class ExitDefinitionTests
{
    private static TaskInstance GetTaskInstance() => new TaskInstance
    {
        EffectiveDate = new DateTime(2026, 10, 5),
        RunId = Guid.CreateVersion7().ToString(),
        IsManual = false,
        EnvironmentName = "Development"
    };

    private static string GetExitDefinitionJson() => """
                "ExitDefinition": {
                    "isActive": true,
                    "success": { "email": true, "to": ["admin@test.com"], "subject": "Task Succeeded", "body": "Completed", "priority": "Normal", "attachments": [] },
                    "failure": { "email": true, "to": ["admin@test.com"], "subject": "Task Failed", "body": "Error", "priority": "High", "attachments": [] }
                }
        """;

    private static List<IDefinition> ParseAndDeserialize(string json)
    {
        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json, instance.EffectiveDate, instance.EnvironmentName);
        VariableDefinition VariableDefinitionBlock = JsonParser.VerifyJson();
        if (VariableDefinitionBlock != null)
        {
            var variables = VariableDefinitionBlock.Variables;
            json = JsonParser.ApplyVariableReplacementsToJson(json, VariableDefinitionBlock);
        }
        return JsonParser.DeserializeDefinitionBlocks(json);
    }

    [Fact]
    public void ExitDefinition_DeserializesCorrectly()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": { "<@@V1@@>": "val" },
                    "isActive": true
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var exitDef = result.Last() as ExitDefinition;

        Assert.NotNull(exitDef);
        Assert.True(exitDef.IsActive);
        Assert.NotNull(exitDef.Success);
        Assert.True(exitDef.Success.Email);
        Assert.Contains("admin@test.com", exitDef.Success.To);
        Assert.Equal("Task Succeeded", exitDef.Success.Subject);
        Assert.Equal("Completed", exitDef.Success.Body);
        Assert.Equal("Normal", exitDef.Success.Priority);
        Assert.NotNull(exitDef.Failure);
        Assert.True(exitDef.Failure.Email);
        Assert.Equal("Task Failed", exitDef.Failure.Subject);
        Assert.Equal("Error", exitDef.Failure.Body);
        Assert.Equal("High", exitDef.Failure.Priority);
    }

    [Fact]
    public void ExitDefinition_MustBePresent()
    {
        var json = """
            {
                "VariableDefinition": {
                    "Variables": { "<@@V1@@>": "val" },
                    "isActive": true
                },
                "ClassDefinition": {
                    "classname": "MyClass",
                    "methodname": "Run",
                    "parameters": []
                }
            }
            """;

        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json, instance.EffectiveDate, instance.EnvironmentName);
        var ex = Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
        Assert.Contains("ExitDefinition missing", ex.Message);
    }

    [Fact]
    public void ExitDefinition_MustBeLastBlock()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": { "<@@V1@@>": "val" },
                    "isActive": true
                },
                {{GetExitDefinitionJson()}},
                "ClassDefinition": {
                    "classname": "MyClass",
                    "methodname": "Run",
                    "parameters": []
                }
            }
            """;

        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json, instance.EffectiveDate, instance.EnvironmentName);
        var ex = Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
        Assert.Contains("ExitDefinition should always be the last definition block", ex.Message);
    }

    [Fact]
    public void ExitDefinition_MustNotHaveNumericSuffix()
    {
        var json = """
            {
                "VariableDefinition": {
                    "Variables": { "<@@V1@@>": "val" },
                    "isActive": true
                },
                "ExitDefinition1": {
                    "isActive": true,
                    "success": { "email": false, "to": [], "subject": "", "body": "", "priority": "Normal", "attachments": [] },
                    "failure": { "email": false, "to": [], "subject": "", "body": "", "priority": "Normal", "attachments": [] }
                },
                "ExitDefinition": {
                    "isActive": true,
                    "success": { "email": false, "to": [], "subject": "", "body": "", "priority": "Normal", "attachments": [] },
                    "failure": { "email": false, "to": [], "subject": "", "body": "", "priority": "Normal", "attachments": [] }
                }
            }
            """;

        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json, instance.EffectiveDate, instance.EnvironmentName);
        var ex = Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
        Assert.Contains("must not have a numeric suffix", ex.Message);
    }

    [Fact]
    public void ExitDefinition_IsAlwaysLastInDeserializedList()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": { "<@@V1@@>": "val" },
                    "isActive": true
                },
                "ClassDefinition": {
                    "classname": "MyClass",
                    "methodname": "Run",
                    "parameters": []
                },
                "SchemaDefinition": {
                    "version": "v1.0",
                    "lastUpdated": "2024-01-01T00:00:00Z",
                    "isDeprecated": false,
                    "author": "Test"
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.IsType<ExitDefinition>(result.Last());
        Assert.Equal(4, result.Count);
    }
}
