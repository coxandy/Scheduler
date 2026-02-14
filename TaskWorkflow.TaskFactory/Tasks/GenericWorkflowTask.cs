using TaskWorkflow.Common.Models;
using TaskWorkflow.TaskFactory.Tasks.Base;

namespace TaskWorkflow.TaskFactory.Tasks;

public class GenericWorkflowTask: BaseTask
{
    public GenericWorkflowTask(string json, TaskInstance taskInstance): base(json, taskInstance)
    {}

    public override async Task Run()
    {
        foreach(var block in DefinitionBlocks)
        {
            await block.RunDefinitionBlockAsync(this.Instance);
        }
    }
}