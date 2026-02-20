using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.TaskFactory.Tasks;
using TaskWorkflow.Common.Models;
using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

namespace TaskWorkflow.UnitTests.DefinitionBlockTests;

public class ExitDefinitionTests
{

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
        Assert.True(exitDef.Success.SendEmail);
        Assert.Contains("admin@test.com", exitDef.Success.To);
        Assert.Equal("Task Succeeded", exitDef.Success.Subject);
        Assert.Equal("Completed", exitDef.Success.Body);
        Assert.Equal("Normal", exitDef.Success.Priority);
        Assert.NotNull(exitDef.Failure);
        Assert.True(exitDef.Failure.SendEmail);
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
        var ex = Assert.Throws<FormatException>(() => new WorkflowTaskJsonParser(json, instance));
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
        var ex = Assert.Throws<FormatException>(() => new WorkflowTaskJsonParser(json, instance));
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
                    "success": { "email": false, "to": [], "cc": [], "bcc": [], "subject": "", "body": "", "priority": "Normal", "attachments": [] },
                    "failure": { "email": false, "to": [], "cc": [], "bcc": [], "subject": "", "body": "", "priority": "Normal", "attachments": [] }
                },
                "ExitDefinition": {
                    "isActive": true,
                    "success": { "email": false, "to": [], "cc": [], "bcc": [], "subject": "", "body": "", "priority": "Normal", "attachments": [] },
                    "failure": { "email": false, "to": [], "cc": [], "bcc": [], "subject": "", "body": "", "priority": "Normal", "attachments": [] }
                }
            }
            """;

        TaskInstance instance = GetTaskInstance();
        var ex = Assert.Throws<FormatException>(() => new WorkflowTaskJsonParser(json, instance));
        Assert.Contains("must not have a numeric suffix", ex.Message);
    }
}
