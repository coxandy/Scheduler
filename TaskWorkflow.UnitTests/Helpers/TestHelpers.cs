using TaskWorkflow.Common.Models;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.TaskFactory.Tasks;

namespace TaskWorkflow.UnitTests.Helpers;

internal static class TestHelpers
{
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
                    "success": { "email": true, "to": ["admin@test.com"], "subject": "Task Succeeded", "body": "Completed", "priority": "Normal", "attachments": [] },
                    "failure": { "email": true, "to": ["admin@test.com"], "subject": "Task Failed", "body": "Error", "priority": "High", "attachments": [] }
                }
        """;

    internal static List<IDefinition> ParseAndDeserialize(string json)
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
}
