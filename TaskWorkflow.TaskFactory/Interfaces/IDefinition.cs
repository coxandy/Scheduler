using TaskWorkflow.Common.Models;

namespace TaskWorkflow.TaskFactory.Interfaces;

public interface IDefinition
{
    public bool IsActive { get; set; }
    Task RunDefinitionBlockAsync(TaskInstance taskInstance);
}