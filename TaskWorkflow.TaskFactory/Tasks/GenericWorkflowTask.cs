using TaskWorkflow.Common.Models;
using TaskWorkflow.TaskFactory.Tasks.Base;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;

namespace TaskWorkflow.TaskFactory.Tasks;

public class GenericWorkflowTask: BaseTask
{
    public GenericWorkflowTask(string json, TaskInstance taskInstance, IServiceProvider serviceProvider): base(json, taskInstance, serviceProvider)
    {}

    public override async Task<bool> Run()
    {
        foreach(var defBlock in DefinitionBlocks)
        {
            try
            {
                if (defBlock.IsActive)
                {
                    await defBlock.RunDefinitionBlockAsync(this.Instance, this.ServiceProvider, this.TaskContext);
                }
            }
            catch (Exception ex)
            {
                await ProcessTaskErrorAsync(ex, defBlock);

                // Throw exception to terminate task execution if def block is configured to AbortTask or AbortTaskAndReportError
                if ((defBlock.OnError == eOnError.AbortTaskAndReportError) || (defBlock.OnError == eOnError.AbortTask))
                {
                    throw;
                }
            }
        }
        return true;
    }
}