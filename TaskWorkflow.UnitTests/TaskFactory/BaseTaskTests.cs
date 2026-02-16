using Moq;
using TaskWorkflow.Common.Models;
using TaskWorkflow.TaskFactory.Tasks;
using Xunit;

namespace TaskWorkflow.UnitTests.TaskFactory;

public class BaseTaskTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider = new();

    private static TaskInstance GetTaskInstance() => new TaskInstance
    {
        EffectiveDate = new DateTime(2026, 10, 5),
        RunId = Guid.CreateVersion7().ToString(),
        IsManual = false,
        EnvironmentName = "Development"
    };

    private static string GetValidJson() => """
        {
            "VariableDefinition": {
                "Variables": {
                    "<@@Test1@@>": 13,
                    "<@@Test2@@>": 15,
                    "<@@Test3@@>": "andy",
                    "<@@Test4@@>": "58"
                },
                "IsActive": true
            },
            "ClassDefinition": {
                "classname": "Web3.Api.GetBalancesByEpoch",
                "methodname": "GetBalancesByEpoch",
                "parameters": [
                    "arrayval1",
                    "arrayval2"
                ]
            },
            "SchemaDefinition": {
                "version": "v2.1",
                "lastUpdated": "2024-05-20T14:30:00Z",
                "isDeprecated": false,
                "author": "DevOps Team"
            },
            "ExitDefinition": {
                "isActive": true,
                "success": { "email": true, "to": ["admin@test.com"], "subject": "Task Succeeded", "body": "Completed", "priority": "Normal", "attachments": [] },
                "failure": { "email": true, "to": ["admin@test.com"], "subject": "Task Failed", "body": "Error", "priority": "High", "attachments": [] }
            }
        }
        """;

    [Fact]
    public void Constructor_ValidJson_CreatesInstance()
    {
        var task = new GenericWorkflowTask(GetValidJson(), GetTaskInstance(), _mockServiceProvider.Object);

        Assert.NotNull(task);
    }

    [Fact]
    public void Constructor_ValidJson_ExtractsVariables()
    {
        var task = new GenericWorkflowTask(GetValidJson(), GetTaskInstance(), _mockServiceProvider.Object);
        var variables = task.GetVariables();

        Assert.Equal(4, variables.Count);
        Assert.Equal("13", variables["<@@Test1@@>"].ToString());
        Assert.Equal("andy", variables["<@@Test3@@>"].ToString());
    }

    [Fact]
    public async Task Run_ValidJson_ExecutesAllDefinitionBlocks()
    {
        var task = new GenericWorkflowTask(GetValidJson(), GetTaskInstance(), _mockServiceProvider.Object);

        await task.Run();

        // If Run completes without exception, all blocks were executed
        Assert.Equal(4, task.GetDefinitionBlocks().Count);
    }

    [Fact]
    public void Constructor_StoresServiceProvider()
    {
        var task = new GenericWorkflowTask(GetValidJson(), GetTaskInstance(), _mockServiceProvider.Object);

        Assert.Same(_mockServiceProvider.Object, task.GetServiceProvider());
    }

    [Fact]
    public void Constructor_StoresTaskInstance()
    {
        var instance = GetTaskInstance();
        var task = new GenericWorkflowTask(GetValidJson(), instance, _mockServiceProvider.Object);

        Assert.Same(instance, task.GetTaskInstance());
    }
}
