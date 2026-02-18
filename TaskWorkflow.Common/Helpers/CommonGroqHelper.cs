using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TaskWorkflow.Common.Models.BlockDefinition;

namespace TaskWorkflow.Common.Helpers;

public static class CommonGroqHelper
{
    private static readonly HttpClient _httpClient = new();
    private const string GroqApiUrl = "https://api.groq.com/openai/v1/chat/completions";

    public static async Task<DataTable> SendGroqRequestAsync(GroqQuestion question, string? configApiKey = null)
    {
        // Use question-level API key first, fall back to config-level key
        var apiKey = !string.IsNullOrWhiteSpace(question.ApiKey) ? question.ApiKey : configApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Groq API key is not configured.");

        if (string.IsNullOrWhiteSpace(question.Prompt))
            throw new InvalidOperationException("Groq prompt cannot be empty.");

        if (string.IsNullOrWhiteSpace(question.DSTableName))
            throw new InvalidOperationException("DSTableName must be specified for Groq question.");

        var requestBody = new
        {
            model = question.Model,
            messages = new[]
            {
                new { role = "system", content = question.SystemPrompt },
                new { role = "user", content = question.Prompt }
            },
            response_format = new { type = "json_object" },
            temperature = question.Temperature,
            max_tokens = question.MaxTokens
        };

        var jsonPayload = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, GroqApiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Groq API returned {(int)response.StatusCode} {response.StatusCode}: {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var content = ExtractContentFromResponse(responseJson);
        var dataTable = ParseJsonToDataTable(content, question.DSTableName);
        return dataTable;
    }

    internal static string ExtractContentFromResponse(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var choices = root.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
            throw new InvalidOperationException("Groq API returned no choices.");

        var messageContent = choices[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(messageContent))
            throw new InvalidOperationException("Groq API returned empty content.");

        return messageContent;
    }

    internal static DataTable ParseJsonToDataTable(string jsonContent, string tableName)
    {
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        JsonElement arrayElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            arrayElement = root;
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            arrayElement = default;
            bool found = false;
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = prop.Value;
                    found = true;
                    break;
                }
            }
            if (!found)
                throw new FormatException(
                    "Groq JSON response does not contain a JSON array. " +
                    "Ensure the SystemPrompt instructs the model to return a JSON array of objects.");
        }
        else
        {
            throw new FormatException($"Expected JSON array or object but got {root.ValueKind}.");
        }

        var dt = new DataTable(tableName);

        if (arrayElement.GetArrayLength() == 0)
            return dt;

        var firstItem = arrayElement[0];
        if (firstItem.ValueKind != JsonValueKind.Object)
            throw new FormatException("Expected each array element to be a JSON object.");

        foreach (var prop in firstItem.EnumerateObject())
        {
            dt.Columns.Add(prop.Name, typeof(string));
        }

        foreach (var item in arrayElement.EnumerateArray())
        {
            var row = dt.NewRow();
            foreach (DataColumn col in dt.Columns)
            {
                if (item.TryGetProperty(col.ColumnName, out var val))
                {
                    row[col.ColumnName] = val.ValueKind == JsonValueKind.Null
                        ? DBNull.Value
                        : val.ToString();
                }
            }
            dt.Rows.Add(row);
        }

        return dt;
    }
}
