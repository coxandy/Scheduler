using Serilog;
using System.Text.RegularExpressions;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.BlockDefinition;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using TaskWorkflow.Common.Helpers;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class IfDefinition: IDefinition
{
    public bool IsActive { get; set; } = true;
    public string BlockName{ get; set; } = String.Empty;
    public eOnError OnError { get; set; } = eOnError.AbortTaskAndReportError;

    public List<Condition> Conditions { get; set; } = [];

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance, IServiceProvider serviceProvider, TaskContext taskContext)
    {
        Log.Debug($"RunDefinitionBlockAsync() - RunId: {taskInstance.RunId}  Running {GetType().Name}..");
        foreach (var condition in Conditions)
        {
            Log.Debug($"Running condition: {condition.ConditionName}");

            string leftOperand = ResolveOperand(condition.LeftOperand, taskContext);
            string rightOperand = ResolveOperand(condition.RightOperand, taskContext);

            bool outcome = await CommonIfConditionHelper.ProcessIfConditionAsync(condition.Operator, leftOperand, rightOperand);

            eConditionOutcome action = outcome ? condition.OnTrueAction : condition.OnFalseAction;
            Log.Debug($"Condition '{condition.ConditionName}' evaluated to {outcome}. Action: {action}");

            if (action == eConditionOutcome.AbortTaskAndReportError)
            {
                // abort task & report error
                this.OnError = eOnError.AbortTaskAndReportError;
                throw new OperationCanceledException($"Condition '{condition.ConditionName}' result was {outcome}. Task aborted.");
            }
            if (action == eConditionOutcome.AbortTask)
            {
                // just abort task without reporting error
                this.OnError = eOnError.AbortTask;
                throw new OperationCanceledException($"Condition '{condition.ConditionName}' result was {outcome}. Task aborted.");
            }
            // else eConditionOutcome.Proceed - continue to next condition
        }
    }

    private static string ResolveOperand(string operand, TaskContext taskContext)
    {
        if (operand != null && CommonRuntimeFunctionHelper.IsRuntimeFunction(operand))
            return CommonRuntimeFunctionHelper.ResolveRuntimeFunction(operand, taskContext);

        if (operand != null && Regex.IsMatch(operand, CommonJsonParsingHelper.VariableNamePattern))
        {
            var value = taskContext.GetVariable(operand);
            return value?.ToString() ?? operand;
        }
        return operand ?? string.Empty;
    }
}
