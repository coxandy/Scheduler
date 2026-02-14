using TaskWorkflow.Common.Models;

namespace TaskWorkflow.TaskFactory.Tasks;

public class GenericWorkflowTask: BaseTask
{
    public GenericWorkflowTask(string json, TaskInstance taskInstance): base(json, taskInstance)
    {
        
    }

    public override async Task Run()
    {
    }
}