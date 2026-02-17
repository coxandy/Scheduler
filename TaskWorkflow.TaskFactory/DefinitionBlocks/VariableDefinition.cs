using Serilog;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.TaskFactory.Tasks;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class VariableDefinition : IDefinition
{
    public bool IsActive { get; set; } = true;
    public string BlockName{ get; set; } = String.Empty;
    public eOnError OnError { get; set; } = eOnError.AbortTask;

    public Dictionary<string, object> Variables { get; set; } = new();

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance, IServiceProvider serviceProvider, TaskContext taskContext)
    {
        Log.Debug($"RunDefinitionBlockAsync() - RunId: {taskInstance.RunId}  Running {GetType().Name}..");
        foreach (var variable in Variables)
        {
            taskContext.SetVariable(variable.Key, variable.Value);
        }
    }
}