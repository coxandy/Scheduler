using Moq;
using TaskWorkflow.Common.Models;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.TaskFactory.Tasks;

namespace TaskWorkflow.UnitTests.Helpers;

internal static class TestHelpers
{
    private static readonly Mock<IServiceProvider> _mockServiceProvider = new();

    internal static TaskInstance GetTaskInstance() => new TaskInstance
    {
        EffectiveDate = new DateTime(2026, 10, 5),
        RunId = Guid.CreateVersion7().ToString(),
        IsManual = false,
        EnvironmentName = "Development"
    };

    internal static string GetExitDefinitionJson() => """
                "ExitDefinition": {
                    "isActive": true,
                    "success": { "email": true, "to": ["admin@test.com"], "cc": [], "bcc": [], "subject": "Task Succeeded", "body": "Completed", "priority": "Normal", "attachments": [] },
                    "failure": { "email": true, "to": ["admin@test.com"], "cc": [], "bcc": [], "subject": "Task Failed", "body": "Error", "priority": "High", "attachments": [] }
                }
        """;

    internal static GenericWorkflowTask CreateTask(string json)
    {
        var instance = GetTaskInstance();
        return new GenericWorkflowTask(json, instance, _mockServiceProvider.Object);
    }

    internal static List<IDefinition> ParseAndDeserialize(string json)
    {
        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json, instance);
        VariableDefinition variableBlock = JsonParser.DeserializeVariableDefinitionBlock(instance);
        if (variableBlock != null)
        {
            var variables = variableBlock.Variables;
            json = JsonParser.ApplyVariableReplacementsToJson(json, variableBlock);
        }
        return JsonParser.DeserializeDefinitionBlocks(json);
    }
}
