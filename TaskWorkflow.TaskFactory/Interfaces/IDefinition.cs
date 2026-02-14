using TaskWorkflow.Common.Models;

namespace TaskWorkflow.TaskFactory.Interfaces;

public interface IDefinition
{
    Task RunDefinitionBlockAsync(TaskInstance taskInstance);
}