using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskWorkflow.Common.Models;

namespace TaskWorkflow.RegressionTests;

public static class TestHelper
{
    public static TaskInstance GetTaskInstance() => new()
    {
        EffectiveDate = new DateTime(2026, 10, 5),
        RunId = Guid.CreateVersion7().ToString(),
        IsManual = false,
        EnvironmentName = "Development"
    };

    public static IServiceProvider GetServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        return services.BuildServiceProvider();
    }
}
