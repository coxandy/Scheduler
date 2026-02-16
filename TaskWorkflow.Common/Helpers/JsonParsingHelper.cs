using System.Text.RegularExpressions;

namespace TaskWorkflow.Common.Helpers;

public static class JsonParsingHelper
{
    public static string VariablNamePattern = @"^<@@.*?@@>$";

    public static string GetBaseDefinitionName(string propertyName)
    {
        return Regex.Replace(propertyName, @"\d+$", "");
    }

    public static string ReplaceVariablesInJson(string json, string key, string value)
    {
       json = json.Replace(key.ToString(), value.ToString());
       return json;
    }

    public static string ReplaceToken(string json, DateTime effectiveDate, string environmentName)
    {
        if (String.IsNullOrEmpty(json)) return json;
        Dictionary<string, string> tokens = new Dictionary<string, string>();
        
        tokens.Add("<yyyy-MM-dd>", effectiveDate.ToString("yyyy-MM-dd"));
        tokens.Add("<yyyy-MMM-dd>", effectiveDate.ToString("yyyy-MMM-dd"));
        tokens.Add("<yyyy MM dd>", effectiveDate.ToString("yyyy MM dd"));
        tokens.Add("<yyyy MMM dd>", effectiveDate.ToString("yyyy MMM dd"));
        tokens.Add("<dd MMM yyyy>", effectiveDate.ToString("dd MMM yyyy"));
        tokens.Add("<MMdd>", effectiveDate.ToString("MMdd"));

        foreach(var token in tokens)
        {
            json = json.Replace(token.Key.ToString(), token.Value.ToString());
        }
       return json;
    }
}