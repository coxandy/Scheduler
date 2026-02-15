using System.Text.RegularExpressions;

namespace TaskWorkflow.Common.Helpers;

public static class JsonParsingHelper
{
    
    public static string GetBaseDefinitionName(string propertyName)
    {
        return Regex.Replace(propertyName, @"\d+$", "");
    }
}