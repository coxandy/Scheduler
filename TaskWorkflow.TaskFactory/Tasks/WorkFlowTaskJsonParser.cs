using System.Text; 
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskWorkflow.Common.Helpers;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Interfaces;

namespace TaskWorkflow.TaskFactory.Tasks;

public class WorkflowTaskJsonParser
{

    private string _json = String.Empty;
    private  readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Dictionary<string, Type> _definitionBlockTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "VariableDefinition", typeof(VariableDefinition) },
        { "ClassDefinition", typeof(ClassDefinition) },
        { "SchemaDefinition", typeof(SchemaDefinition) }
    };

    public WorkflowTaskJsonParser (string json)
    {
        _json = json;
    }

    public VariableDefinition VerifyJson()
    {
        // Verify DefinitionBlock Json is valid
        VerifyJson(_json);
        // Return Variable Defintion Block if it exists
        return DeserializeVariableDefinitionBlock(_json);
    }


    public List<IDefinition> GetDefinitionBlocks()
    {
        var definitionBlockList = DeserializeDefinitionBlocks(_json);
        return definitionBlockList;
    }

    public void VerifyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be null or empty.", nameof(json));

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
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

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in root.EnumerateObject())
        {
            var propertyName = property.Name;

            if (!seenKeys.Add(propertyName))
            {
                document.Dispose();
                throw new FormatException($"Duplicate definition block '{propertyName}'.");
            }

            var baseName = JsonParsingHelper.GetBaseDefinitionName(propertyName);

            if (!_definitionBlockTypeMap.TryGetValue(baseName, out _))
            {
                document.Dispose();
                throw new KeyNotFoundException($"Unknown definition block '{propertyName}'. Valid blocks: {string.Join(", ", _definitionBlockTypeMap.Keys)}");
            }

            if (baseName.Equals("VariableDefinition", StringComparison.OrdinalIgnoreCase)
                && !propertyName.Equals("VariableDefinition", StringComparison.OrdinalIgnoreCase))
            {
                document.Dispose();
                throw new FormatException($"VariableDefinition must not have a numeric suffix. Found '{propertyName}'.");
            }
        }
    }

    //Special case
    public VariableDefinition DeserializeVariableDefinitionBlock(string json)
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
            var property = document.RootElement.EnumerateObject().FirstOrDefault(p => string.Equals(p.Name, "VariableDefinition", StringComparison.OrdinalIgnoreCase));

            if (property.Value.ValueKind != JsonValueKind.Undefined) 
            {
                var baseName = JsonParsingHelper.GetBaseDefinitionName(property.Name);
                var definitionType = _definitionBlockTypeMap[baseName];

                var rawJson = property.Value.GetRawText();
                var definition = JsonSerializer.Deserialize(rawJson, definitionType, _jsonOptions) as VariableDefinition ?? throw new FormatException($"Failed to deserialize '{property.Name}' into {definitionType.Name}.");
                return definition;               
            }
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
                var baseName = JsonParsingHelper.GetBaseDefinitionName(property.Name);
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
        var variablesToReplace = ((VariableDefinition)variableDefinition).Variables;
        if (((VariableDefinition)variableDefinition).Variables.Any())
        {
            foreach(var variable in variablesToReplace)
            {
                json = json.Replace(variable.Key.ToString(), variable.Value.ToString());
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
                var baseName = JsonParsingHelper.GetBaseDefinitionName(property.Name);
                var definitionType = _definitionBlockTypeMap[baseName];

                var rawJson = property.Value.GetRawText();
                var definition = JsonSerializer.Deserialize(rawJson, definitionType, _jsonOptions) as IDefinition
                    ?? throw new FormatException($"Failed to deserialize '{property.Name}' into {definitionType.Name}.");

                definitions.Add(definition);
            }

            return definitions;
        }
    }
}