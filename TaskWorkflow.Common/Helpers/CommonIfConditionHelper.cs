namespace TaskWorkflow.Common.Helpers;

public static class CommonIfConditionHelper
{
    public static async Task<bool> ProcessIfConditionAsync(string conditionOperator, string leftOperand, string rightOperand)
    {
        return await Task.Run (() => {
                // Determine outcome of condition 
                switch (conditionOperator.ToLowerInvariant().Trim())
                {
                    case "equals":
                    case "==":
                    case "=":
                        return leftOperand == rightOperand;
                    case "not_equals":
                    case "!=":
                    case "<>":
                        return leftOperand != rightOperand;
                    case "less_than":                    
                    case "<":
                        return double.TryParse(leftOperand, out var leftValue) && double.TryParse(rightOperand, out var rightValue) && leftValue < rightValue;
                    case "greater_than":
                    case ">":
                        return double.TryParse(leftOperand, out var leftValue2) && double.TryParse(rightOperand, out var rightValue2) && leftValue2 > rightValue2;
                    default:
                        throw new ArgumentException($"Unknown operator: {conditionOperator}");
                }
        });
    }
}