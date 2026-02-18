namespace TaskWorkflow.Common.Models.BlockDefinition;

public class GroqQuestion
{
    public string Prompt { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = "You are a data analyst. Always respond with valid JSON. Return a JSON object containing a 'data' property with an array of objects.";
    public string Model { get; set; } = "llama-3.1-8b-instant";
    public string DSTableName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1024;
}
