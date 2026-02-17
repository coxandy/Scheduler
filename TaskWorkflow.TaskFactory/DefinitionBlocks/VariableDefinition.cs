using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class VariableDefinition : IDefinition
{
    public bool IsActive { get; set; } = true;
    public string BlockName{ get; set; } = String.Empty;
    public eOnError OnError { get; set; } = eOnError.AbortTask;

    public Dictionary<string, object> Variables { get; set; } = new();
    
    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance, IServiceProvider serviceProvider)
    {
        Console.Write($"RunId: {taskInstance.RunId}  Running {GetType().Name}..");
    }
}