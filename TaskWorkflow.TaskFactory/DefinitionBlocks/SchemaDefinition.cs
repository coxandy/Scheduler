using System.Text.Json.Serialization;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Models;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class SchemaDefinition : IDefinition
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }

    [JsonPropertyName("isDeprecated")]
    public bool IsDeprecated { get; set; }

    [JsonPropertyName("author")]
    public string Author { get; set; }

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance)
    {
        Console.Write($"RunId: {taskInstance.RunId}  Running {GetType().Name}..");
    }
}