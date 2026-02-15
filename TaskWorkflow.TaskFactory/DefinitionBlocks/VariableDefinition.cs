using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Models;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class VariableDefinition : IDefinition
{
    public bool IsActive { get; set; }

    public Dictionary<string, object> Variables { get; set; } = new();
    
    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance)
    {
        Console.Write($"RunId: {taskInstance.RunId}  Running {GetType().Name}..");
    }
}