using Moq;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.TaskFactory.Tasks;
using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

namespace TaskWorkflow.UnitTests.TaskFactory;

public class BaseTaskTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider = new();

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
        var variables = task.GetTaskContext().GetAllVariables();

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
        Assert.Equal(3, task.GetDefinitionBlocks().Count);
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
