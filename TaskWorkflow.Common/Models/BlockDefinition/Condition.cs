using TaskWorkflow.Common.Models.BlockDefinition.Enums;

namespace TaskWorkflow.Common.Models.BlockDefinition;

public class Condition
{
    public string ConditionName { get; set; }
    public string Operator { get; set; }
    public string LeftOperand { get; set; }
    public string RightOperand { get; set; }
    public eConditionOutcome OnTrueAction { get; set; }
    public eConditionOutcome OnFalseAction { get; set; }
}