using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using TaskWorkflow.Common.Tasks;

namespace TaskWorkflow.TaskFactory.Interfaces;

public interface IDefinition
{
    public bool IsActive { get; set; }
    public string BlockName { get; set; }
    public eOnError OnError { get; set; }
    Task RunDefinitionBlockAsync(TaskInstance taskInstance, IServiceProvider serviceProvider, TaskContext taskContext);
}