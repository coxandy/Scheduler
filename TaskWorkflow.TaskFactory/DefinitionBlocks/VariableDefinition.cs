using System.Text.Json.Serialization;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Models;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class VariableDefinition : IDefinition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance)
    {
        Console.Write($"RunId: {taskInstance.RunId}  Running {GetType().Name}..");
    }
}