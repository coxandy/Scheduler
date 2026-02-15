using TaskWorkflow.Common.Models;
using TaskWorkflow.TaskFactory.Tasks.Base;

namespace TaskWorkflow.Api.Interfaces;

public interface ITaskObjectCreationService
{
    Task<BaseTask> CreateTaskObjectAsync(string taskClassName = "GenericWorkflowTask");
    string Json { get; set; }
    TaskInstance Instance { get; set; } 
}