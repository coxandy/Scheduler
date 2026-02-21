using System.Reflection;
using System.Text.RegularExpressions;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.Functions;

namespace TaskWorkflow.Common.Helpers;

public static class CommonFunctionHelper
{
    // Matches <fn_...()> tokens but deliberately excludes <fn_runtime...> tokens which are
    // resolved at block-execution time by CommonRuntimeFunctionHelper, not at parse time.
    // Note: no \b after 'runtime' — fn_runtime_xxx has no word boundary after 'runtime'.
    private static string FunctionNamePattern = @"<fn_(?!runtime).*?\(.*?\)>";

    public static string ResolveAndReplaceFunctionVariables(string json, TaskInstance taskInstance)
    {
        if (String.IsNullOrEmpty(json)) return json;
    
        var functionsPresent = MatchFunctionVariables(json, taskInstance);
        foreach (var replacement in functionsPresent)
        {
            // Escape backslashes for JSON compatibility (e.g. Windows file paths)
            var escapedValue = replacement.ResolvedValue.Replace("\\", "\\\\");
            json = json.Replace(replacement.InitialMatch, escapedValue);
        }
        return json;
    }

    public static List<FunctionVariableParams> MatchFunctionVariables(string json, TaskInstance taskInstance)
    {
        return Regex.Matches(json, FunctionNamePattern)
            .Cast<Match>()
            .Select(m => m.Value)
            .Distinct()
            .Select(ParseFunctionVariable)
            .ToList();
    }

    internal static FunctionVariableParams ParseFunctionVariable(string match)
    {
        // match format: <fn_FunctionName(param1, param2, ...)>
        var inner = match.TrimStart('<').TrimEnd('>');
        var parenStart = inner.IndexOf('(');
        var parenEnd = inner.LastIndexOf(')');

        var functionName = inner[..parenStart];
        var paramString = inner[(parenStart + 1)..parenEnd];

        // Unescape JSON-style backslashes since function tokens are parsed from raw JSON text
        var paramValues = string.IsNullOrWhiteSpace(paramString)
            ? new List<string>()
            : paramString.Split(',').Select(p => p.Trim().Replace("\\\\", "\\")).ToList();

        var result = new FunctionVariableParams
        {
            InitialMatch = match,
            FunctionName = functionName,
            ParamValues = paramValues,
            ResolvedValue = String.Empty
        };

        result.ResolvedValue = InvokeFunction(result);

        return result;
    }

    internal static string InvokeFunction(FunctionVariableParams funcParams)
    {
        var method = typeof(CommonFunctionHelper).GetMethod(funcParams.FunctionName,
            BindingFlags.Public | BindingFlags.Static);

        if (method == null)
            throw new NotSupportedException($"Function '{funcParams.FunctionName}' is not supported.");

        var expectedParams = method.GetParameters().Length;
        if (funcParams.ParamValues.Count != expectedParams)
            throw new ArgumentException(
                $"Function '{funcParams.FunctionName}' expects {expectedParams} parameter(s) but received {funcParams.ParamValues.Count}.");

        return funcParams.FunctionName switch
        {
            "fn_GetLatestFile" => fn_GetLatestFile(funcParams.ParamValues[0]),
            "fn_GetOldestFile" => fn_GetOldestFile(funcParams.ParamValues[0]),
            _ => throw new NotSupportedException($"Function '{funcParams.FunctionName}' is not supported.")
        };
    }

    //Real functions
    public static string fn_GetLatestFile(string fullWildcardPath)
    {
        if (string.IsNullOrWhiteSpace(fullWildcardPath))
            throw new ArgumentException("Path cannot be null or empty.");

        string directory = Path.GetDirectoryName(fullWildcardPath);
        string searchPattern = Path.GetFileName(fullWildcardPath);

        if (string.IsNullOrEmpty(directory))
        {
            throw new DirectoryNotFoundException($"fn_GetOldestFile() - Requires the full path with wildcarded filename");
        }

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"fn_GetOldestFile() - The path '{directory}' does not exist.");
        }

        // 2. Get files, sort by LastWriteTime, and take the first one
        var file = new DirectoryInfo(directory).GetFiles(searchPattern)
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
        if (file == null) throw new FileNotFoundException($"File not found matching wildcard value: {fullWildcardPath}");

        return file.FullName;
    }

    public static string fn_GetOldestFile(string fullWildcardPath)
    {
        if (string.IsNullOrWhiteSpace(fullWildcardPath))
            throw new ArgumentException("Path cannot be null or empty.");

        string directory = Path.GetDirectoryName(fullWildcardPath);
        string searchPattern = Path.GetFileName(fullWildcardPath);

        if (string.IsNullOrEmpty(directory))
        {
            throw new DirectoryNotFoundException($"fn_GetOldestFile() - Requires the full path with wildcarded filename");
        }

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"fn_GetOldestFile() - The path '{directory}' does not exist.");
        }

        var file = new DirectoryInfo(directory).GetFiles(searchPattern)
                    .OrderBy(f => f.LastWriteTime)
                    .FirstOrDefault();
        if (file == null) throw new FileNotFoundException($"File not found matching wildcard value: {fullWildcardPath}");

        return file.FullName;
    }
}
