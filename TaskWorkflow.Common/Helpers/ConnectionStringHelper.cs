namespace TaskWorkflow.Common.Helpers;

public static class ConnectionStringHelper
{
    private static Dictionary<string, string> _connectionStrings = new(StringComparer.OrdinalIgnoreCase);

    public static void Initialize(Dictionary<string, string> connectionStrings)
    {
        _connectionStrings = connectionStrings ?? throw new ArgumentNullException(nameof(connectionStrings));
    }

    public static Dictionary<string, string> GetAllConnectionStrings() => _connectionStrings;

    public static string GetConnectionString(string name)
    {
        if (_connectionStrings.TryGetValue(name, out var connectionString))
            return connectionString;

        throw new KeyNotFoundException($"Connection string '{name}' not found.");
    }
}
