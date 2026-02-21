using System.Text.RegularExpressions;
using TaskWorkflow.Common.Tasks;

namespace TaskWorkflow.Common.Helpers;

/// <summary>
/// Resolves runtime function tokens that appear in IfDefinition operands.
/// Token format: <fn_runtime_functionname("param1", "param2", ...)>
/// The full function name from '&lt;' to '(' is a single identifier with no spaces.
/// Parameters are comma-separated quoted strings.
/// Functions are evaluated at block-execution time against the live TaskContext.
/// </summary>
public static partial class CommonRuntimeFunctionHelper
{
    // Captures the full function name (group 1, e.g. fn_runtime_get_datatable_count)
    // and the raw parameter list inside parens (group 2).
    public static readonly string RuntimeFunctionPattern = @"^<(fn_runtime\w+)\s*\((.*?)\)>$";

    // Extracts each individual quoted parameter value from the raw parameter list.
    [GeneratedRegex(@"""([^""]*)""")]
    private static partial Regex ParamRegex();

    public static bool IsRuntimeFunction(string operand)
    {
        return operand != null && Regex.IsMatch(operand, RuntimeFunctionPattern);
    }

    public static string ResolveRuntimeFunction(string operand, TaskContext taskContext)
    {
        var match = Regex.Match(operand, RuntimeFunctionPattern);
        if (!match.Success)
            throw new ArgumentException($"Invalid runtime function expression: '{operand}'");

        string functionName = match.Groups[1].Value;
        var parameters = ParseParameters(match.Groups[2].Value);

        return functionName switch
        {
            "fn_runtime_get_datatable_count"        => RequireParams(functionName, parameters, 1,
                                                           () => CommonDataTableHelper.GetDataTableCount(parameters[0], taskContext)),
            "fn_runtime_get_datatable_hash"         => RequireParams(functionName, parameters, 1,
                                                           () => CommonDataTableHelper.GetDataTableHash(parameters[0], taskContext)),
            "fn_runtime_get_datatable_column_total" => RequireParams(functionName, parameters, 2,
                                                           () => CommonDataTableHelper.GetDataTableColumnTotal(parameters[0], parameters[1], taskContext)),
            _ => throw new NotSupportedException($"Unknown runtime function: '{functionName}'")
        };
    }

    private static List<string> ParseParameters(string rawParamList)
    {
        return [..ParamRegex().Matches(rawParamList).Select(m => m.Groups[1].Value)];
    }

    private static string RequireParams(string functionName, List<string> parameters, int expected, Func<string> fn)
    {
        if (parameters.Count != expected)
            throw new ArgumentException(
                $"Runtime function '{functionName}' requires {expected} parameter(s) but received {parameters.Count}.");
        return fn();
    }
}
