using System.Text.RegularExpressions;

namespace TaskWorkflow.Common.Helpers;

public static class JsonParsingHelper
{
    public static string GetBaseDefinitionName(string propertyName)
    {
        return Regex.Replace(propertyName, @"\d+$", "");
    }

    public static string ReplaceVariablesInJson(string json, string key, string value)
    {
       json = json.Replace(key.ToString(), value.ToString());
       return json;
    }
}