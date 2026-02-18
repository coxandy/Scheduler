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
}