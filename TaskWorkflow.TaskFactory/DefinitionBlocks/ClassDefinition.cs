using System.Text.Json.Serialization;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.Enums;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class ClassDefinition: IDefinition
{
    public bool IsActive { get; set; } = true;
    public string BlockName{ get; set; } = String.Empty;
    public eOnError OnError { get; set; } = eOnError.AbortTask;
    public eTaskStatus Status { get; set; }

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