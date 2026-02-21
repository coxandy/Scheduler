using System.Text.Json;

namespace TaskWorkflow.Common.Helpers;

public static class CommonDefinitionBlockHelper
{

    public static string GetDefinitionBlockJson(string json, string DefinitionBlockName, bool WithPropertyName = false)
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
            var property = document.RootElement.EnumerateObject().FirstOrDefault(p => string.Equals(p.Name, DefinitionBlockName, StringComparison.OrdinalIgnoreCase));

            if (property.Value.ValueKind != JsonValueKind.Undefined) 
            {
                return WithPropertyName ? $"\"{property.Name}\":{property.Value.GetRawText()}" : property.Value.GetRawText();
            }
        }
        return null;
    }
}