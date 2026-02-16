using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.Enums;
using TaskWorkflow.TaskFactory.Tasks.Base;


namespace TaskWorkflow.TaskFactory.Tasks;

public class GenericWorkflowTask: BaseTask
{
    public GenericWorkflowTask(string json, TaskInstance taskInstance, IServiceProvider serviceProvider): base(json, taskInstance, serviceProvider)
    {}

    public override async Task Run()
    {
        foreach(var defBlock in DefinitionBlocks)
        {
            try
            {
                if (defBlock.IsActive)
                {
                    await defBlock.RunDefinitionBlockAsync(this.Instance);
                }
            }
            catch (Exception ex)
            {
                await ProcessTaskErrorAsync(ex, defBlock);
                //throw exception to terminate task execution if def block is configured to eOnError.AbortTask
                if (defBlock.OnError == eOnError.AbortTask) throw;
            }
        }
    }
}