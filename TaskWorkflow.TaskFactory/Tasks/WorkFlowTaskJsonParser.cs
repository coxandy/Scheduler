using System.Text; 
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskWorkflow.Common.Helpers;
using TaskWorkflow.Common.Models;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Interfaces;
using System.Text.RegularExpressions;
namespace TaskWorkflow.TaskFactory.Tasks;

public class WorkflowTaskJsonParser
{
    private string _json = String.Empty;
    private readonly TaskInstance _taskInstance;
    private  readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(), new SqlParameterJsonConverter() }
    };

    private readonly Dictionary<string, Type> _definitionBlockTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "VariableDefinition", typeof(VariableDefinition) },
        { "IfDefinition", typeof(IfDefinition) },
        { "ClassDefinition", typeof(ClassDefinition) },
        { "DatasourceDefinition", typeof(DatasourceDefinition) },
        { "ExcelDefinition", typeof(ExcelDefinition) },
        { "PivotDefinition", typeof(PivotDefinition) },
        { "GroqDefinition", typeof(GroqDefinition) },
        { "EmailDefinition", typeof(EmailDefinition) },
        { "ExitDefinition", typeof(ExitDefinition) }
    };

    public WorkflowTaskJsonParser (string json, TaskInstance taskInstance)
    {
        _json = json;
        _taskInstance = taskInstance;
        VerifyJson();
    }

    public List<IDefinition> GetDefinitionBlocks()
    {
        var definitionBlockList = DeserializeDefinitionBlocks(_json);
        return definitionBlockList;
    }

    public void VerifyJson()
    {
        if (string.IsNullOrWhiteSpace(_json))
            throw new ArgumentException("JSON cannot be null or empty.", nameof(_json));

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(_json);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Invalid JSON format: {ex.Message}", ex);
        }

        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            var kind = root.ValueKind;
            document.Dispose();
            throw new FormatException($"Expected a JSON object but got {kind}.");
        }

        if (!root.EnumerateObject().Any())
        {
            document.Dispose();
            throw new FormatException("JSON contains no definition blocks.");
        }

        var blocklist = root.EnumerateObject().Select(x => x.Name).ToList();
        if (blocklist.Contains("VariableDefinition", StringComparer.OrdinalIgnoreCase))
        {
            if (String.Compare(blocklist[0], "VariableDefinition", StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new FormatException("VariableDefinition should always be the first definition block");
            }
        }

        if (blocklist.Contains("ExitDefinition", StringComparer.OrdinalIgnoreCase))
        {
            if (String.Compare(blocklist.LastOrDefault(), "ExitDefinition", StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new FormatException("ExitDefinition should always be the last definition block");
            }
        }
        else
        {
            throw new FormatException("ExitDefinition missing - it should always be the last definition block");
        }

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in root.EnumerateObject())
        {
            var propertyName = property.Name;

            if (!seenKeys.Add(propertyName))
            {
                document.Dispose();
                throw new FormatException($"Duplicate definition block '{propertyName}'.");
            }

            var baseName = CommonJsonParsingHelper.GetBaseDefinitionName(propertyName);

            if (!_definitionBlockTypeMap.TryGetValue(baseName, out _))
            {
                document.Dispose();
                throw new KeyNotFoundException($"Unknown definition block '{propertyName}'. Valid blocks: {string.Join(", ", _definitionBlockTypeMap.Keys)}");
            }

            if (baseName.Equals("VariableDefinition", StringComparison.OrdinalIgnoreCase)
                && !propertyName.Equals("VariableDefinition", StringComparison.OrdinalIgnoreCase))
            {
                document.Dispose();
                throw new FormatException($"Only one 'VariableDefinition' block permitted. Therefore it must not have a numeric suffix. Found '{propertyName}'.");
            }

            if (baseName.Equals("ExitDefinition", StringComparison.OrdinalIgnoreCase)
                && !propertyName.Equals("ExitDefinition", StringComparison.OrdinalIgnoreCase))
            {
                document.Dispose();
                throw new FormatException($"Only one 'ExitDefinition' block permitted. Therefore it must not have a numeric suffix. Found '{propertyName}'.");
            }
        }
    }

    //Special case
    public VariableDefinition DeserializeVariableDefinitionBlock(TaskInstance taskInstance)
    {
        string propertyJson = CommonDefinitionBlockHelper.GetDefinitionBlockJson(_json, "VariableDefinition");
        if (!String.IsNullOrEmpty(propertyJson))
        {
            var definitionType = _definitionBlockTypeMap["VariableDefinition"];
            var definition = JsonSerializer.Deserialize(propertyJson, definitionType, _jsonOptions) as VariableDefinition ?? throw new FormatException($"Failed to deserialize 'VariableDefinition' into {definitionType.Name}.");
            return definition;               
        }
        return null;
    }


    public string ApplyVariableReplacementsToJson(string json, VariableDefinition variableDefinition)
    {
        //Reconstruct Json, applying string replacements
        StringBuilder newJson = new StringBuilder("{ ");
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Invalid JSON format: {ex.Message}", ex);
        }
        using (document)
        {
            var root = document.RootElement;
            var definitions = new List<IDefinition>();

            foreach (var property in root.EnumerateObject())
            {
                var baseName = CommonJsonParsingHelper.GetBaseDefinitionName(property.Name);
                string PropertyJson = $"\"{property.Name}\":{property.Value.GetRawText()},";

                if (String.Compare(baseName, "VariableDefinition", StringComparison.OrdinalIgnoreCase) != 0)
                {
                    PropertyJson = ReplaceVariables(PropertyJson, variableDefinition);
                }
                newJson.Append(PropertyJson);
            }

            json = newJson.ToString().TrimEnd(',') + "}";
            using var newdocument = JsonDocument.Parse(json);
            string reformattedJson = JsonSerializer.Serialize(newdocument, new JsonSerializerOptions { WriteIndented = true });
            return reformattedJson;
        }
    }

    private string ReplaceVariables(string json, VariableDefinition variableDefinition)
    {
        var variablesToReplace = variableDefinition.Variables;

        if (variableDefinition.Variables.Keys.Any(x => !Regex.IsMatch(x, CommonJsonParsingHelper.VariableNamePattern)))
        {
            throw new FormatException($"Variable names should be formatted with '<@@' + name + '@@>'  (e.g. '<@@ProductId@@>')");
        }

        if (((VariableDefinition)variableDefinition).Variables.Any())
        {
            foreach(var variable in variablesToReplace)
            {
                if (variable.Value != null)
                {
                    // Escape backslashes for JSON compatibility (e.g. Windows file paths)
                    var value = variable.Value.ToString().Replace("\\", "\\\\");
                    json = CommonJsonParsingHelper.ReplaceVariablesInJson(json, variable.Key.ToString(), value);
                }
            }
        }
        return json;
    }

    public List<IDefinition> DeserializeDefinitionBlocks(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Invalid JSON format: {ex.Message}", ex);
        }
        using (document)
        {
            var root = document.RootElement;
            var definitions = new List<IDefinition>();

            foreach (var property in root.EnumerateObject())
            {
                var baseName = CommonJsonParsingHelper.GetBaseDefinitionName(property.Name);
                var definitionType = _definitionBlockTypeMap[baseName];

                var rawJson = property.Value.GetRawText();
                var definition = JsonSerializer.Deserialize(rawJson, definitionType, _jsonOptions) as IDefinition
                    ?? throw new FormatException($"Failed to deserialize '{property.Name}' into {definitionType.Name}.");

                definition.BlockName = property.Name;
                definitions.Add(definition);
            }

            return definitions;
        }
    }
}