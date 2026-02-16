using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;

namespace TaskWorkflow.Common.Helpers;

public class SqlParameterJsonConverter : JsonConverter<SqlParameter>
{
    public override SqlParameter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token.");

        string parameterName = string.Empty;
        SqlDbType sqlDbType = SqlDbType.NVarChar;
        int? size = null;
        object? value = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName token.");

            var propertyName = reader.GetString()!;
            reader.Read();

            switch (propertyName.ToLowerInvariant())
            {
                case "parametername":
                    parameterName = reader.GetString() ?? string.Empty;
                    break;
                case "sqldbtype":
                    var typeString = reader.GetString() ?? throw new JsonException("SqlDbType value cannot be null.");
                    if (!Enum.TryParse<SqlDbType>(typeString, ignoreCase: true, out sqlDbType))
                        throw new JsonException($"Unknown SqlDbType '{typeString}'.");
                    break;
                case "size":
                    size = reader.GetInt32();
                    break;
                case "value":
                    value = reader.TokenType switch
                    {
                        JsonTokenType.Null => DBNull.Value,
                        JsonTokenType.String => reader.GetString(),
                        JsonTokenType.Number when reader.TryGetInt64(out var l) => l,
                        JsonTokenType.Number => reader.GetDouble(),
                        JsonTokenType.True => true,
                        JsonTokenType.False => false,
                        _ => throw new JsonException($"Unsupported value token type: {reader.TokenType}")
                    };
                    break;
            }
        }

        var param = new SqlParameter
        {
            ParameterName = parameterName,
            SqlDbType = sqlDbType,
            Value = value ?? DBNull.Value
        };

        if (size.HasValue)
            param.Size = size.Value;

        return param;
    }

    public override void Write(Utf8JsonWriter writer, SqlParameter value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("ParameterName", value.ParameterName);
        writer.WriteString("SqlDbType", value.SqlDbType.ToString());
        if (value.Size > 0)
            writer.WriteNumber("Size", value.Size);
        if (value.Value == null || value.Value == DBNull.Value)
            writer.WriteNull("Value");
        else
            writer.WriteString("Value", value.Value.ToString());
        writer.WriteEndObject();
    }
}
