using System.Text.RegularExpressions;
using TaskWorkflow.Common.Models;

namespace TaskWorkflow.Common.Helpers;

public static class CommonJsonParsingHelper
{
    public static string VariableNamePattern = @"^<@@.*?@@>$";
    

    public static string GetBaseDefinitionName(string propertyName)
    {
        return Regex.Replace(propertyName, @"\d+$", "");
    }

    public static string ReplaceVariablesInJson(string json, string key, string value)
    {
       json = json.Replace(key.ToString(), value.ToString());
       return json;
    }

    public static string ReplaceTokens(string json, TaskInstance taskInstance)
    {
        if (String.IsNullOrEmpty(json)) return json;
        Dictionary<string, string> tokens = new Dictionary<string, string>();
        
        tokens.Add("<yyyy-MM-dd>", taskInstance.EffectiveDate.ToString("yyyy-MM-dd"));
        tokens.Add("<yyyy-MMM-dd>", taskInstance.EffectiveDate.ToString("yyyy-MMM-dd"));
        tokens.Add("<yyyy MM dd>", taskInstance.EffectiveDate.ToString("yyyy MM dd"));
        tokens.Add("<yyyy MMM dd>", taskInstance.EffectiveDate.ToString("yyyy MMM dd"));
        tokens.Add("<dd MMM yyyy>", taskInstance.EffectiveDate.ToString("dd MMM yyyy"));
        tokens.Add("<MMdd>", taskInstance.EffectiveDate.ToString("MMdd"));

        foreach(var token in tokens)
        {
            json = json.Replace(token.Key.ToString(), token.Value.ToString());
        }
       return json;
    }
}
