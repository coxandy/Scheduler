using System.Text.Json.Serialization;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Models;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class ClassDefinition: IDefinition
{
    [JsonPropertyName("classname")]
    public string ClassName { get; set; }

    [JsonPropertyName("methodname")]
    public string MethodName { get; set; }

    [JsonPropertyName("parameters")]
    public List<string> Parameters { get; set; }

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance)
    {
        Console.Write($"RunId: {taskInstance.RunId}  Running {GetType().Name}..");
    }
}