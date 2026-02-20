namespace TaskWorkflow.Common.Models.Functions;

public class FunctionVariableParams
{
    public string InitialMatch { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public List<string> ParamValues { get; set; } = new();
    public string ResolvedValue { get; set; } = string.Empty;
}
