using System.Text.Json.Serialization;
using TaskWorkflow.TaskFactory.Interfaces;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;
public class VariableDefinition : IDefinition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    public async Task RunDefinitionBlock()
    {
        
    }
}