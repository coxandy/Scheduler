using System.Text.Json;
using TaskWorkflow.Common.Models;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Interfaces;

namespace TaskWorkflow.TaskFactory.Tasks;

public abstract class BaseTask
{

    private static readonly Dictionary<string, Type> _definitionBlockTypeMap = new()
    {
        { "VariableDefinition", typeof(VariableDefinition) },
        { "ClassDefinition", typeof(ClassDefinition) },
        { "SchemaDefinition", typeof(SchemaDefinition) }
    };

    private string _json;
    private TaskInstance _taskInstance;
    public List<IDefinition> DefinitionBlocks;

    public abstract Task Run();

    public BaseTask(string json, TaskInstance taskInstance)
    {
        _json = json;
        _taskInstance = taskInstance;
        DefinitionBlocks = DeserializeDefinitionBlocks(json);
    }


    internal static List<IDefinition> DeserializeDefinitionBlocks(string json)
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

        using (document)
        {
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                throw new FormatException($"Expected a JSON object but got {root.ValueKind}.");

            var definitions = new List<IDefinition>();

            foreach (var property in root.EnumerateObject())
            {
                if (!_definitionBlockTypeMap.TryGetValue(property.Name, out var definitionType))
                    throw new KeyNotFoundException($"Unknown definition block '{property.Name}'. Valid blocks: {string.Join(", ", _definitionBlockTypeMap.Keys)}");

                var rawJson = property.Value.GetRawText();
                var definition = JsonSerializer.Deserialize(rawJson, definitionType) as IDefinition
                    ?? throw new FormatException($"Failed to deserialize '{property.Name}' into {definitionType.Name}.");

                definitions.Add(definition);
            }

            if (definitions.Count == 0)
                throw new FormatException("JSON contains no definition blocks.");

            return definitions;
        }
    }
}