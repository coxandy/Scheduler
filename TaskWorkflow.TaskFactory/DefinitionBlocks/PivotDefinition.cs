using Serilog;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using TaskWorkflow.Common.Models.BlockDefinition;
using TaskWorkflow.Common.Helpers;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class PivotDefinition: IDefinition
{
    public bool IsActive { get; set; } = true;
    public string BlockName{ get; set; } = String.Empty;
    public eOnError OnError { get; set; } = eOnError.AbortTaskAndReportError;
        

    public List<PivotSource> PivotSources { get; set; }

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance, IServiceProvider serviceProvider, TaskContext taskContext)
    {
        Log.Debug($"RunDefinitionBlockAsync() - RunId: {taskInstance.RunId}  Running {GetType().Name}..");

        foreach (var pivotSource in PivotSources)
        {
            await Task.Run(() =>
            {
                var sourceTable = taskContext.GetDataTable(pivotSource.DSTableSource);
                if (sourceTable == null)
                    throw new ArgumentException($"Source DataTable '{pivotSource.DSTableSource}' not found in TaskContext.");

                var pivotedTable = CommonPivotHelper.PivotDataTable(
                    sourceTable,
                    pivotSource.Rows,
                    pivotSource.Columns,
                    pivotSource.Data,
                    pivotSource.DSTableTarget,
                    pivotSource.AggregateFunction ?? "Sum");

                taskContext.AddDataTable(pivotedTable);
            });
        }
    }
}