

public static class UriHelper
{
    public static List<string> AvailableWebServers { get; private set; } = new();
    public static int Port { get; private set; }

    public static void Initialize(List<string> webServers, int port)
    {
        AvailableWebServers = webServers ?? throw new ArgumentNullException(nameof(webServers));
        Port = port;
    }

    public static string GetUriForServer(int serverIndex)
    {
        return $"https://{AvailableWebServers[serverIndex]}:{Port}/api";
    }

    public static int GetServerIndex(string webService) => webService switch
    {
        "WebService1" => 0,
        "WebService2" => 1,
        "WebService3" => 2,
        "WebService4" => 3,
        _ => throw new Exception($"Unknown WebService: {webService}")
    };
}
