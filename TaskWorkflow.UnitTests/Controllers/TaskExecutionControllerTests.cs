using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using TaskWorkflow.Api.Controllers;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.Enums;
using TaskWorkflow.TaskFactory.Tasks;
using Xunit;
using Microsoft.Extensions.Hosting;
using TaskWorkflow.Api.Interfaces;

namespace TaskWorkflow.UnitTests.Controllers;

public class TaskExecutionControllerTests
{
    private readonly TaskExecutionController _controller;

    private static readonly string _validJson = """
        {
            "VariableDefinition": {
                "id": 101,
                "role": "Administrator",
                "permissions": ["read", "write", "delete"],
                "isActive": true
            },
            "ClassDefinition": {
                "classname": "Web3.Api.GetBalancesByEpoch",
                "methodname": "GetBalancesByEpoch",
                "parameters": ["arrayval1", "arrayval2"]
            },
            "SchemaDefinition": {
                "version": "v2.1",
                "lastUpdated": "2024-05-20T14:30:00Z",
                "isDeprecated": false,
                "author": "DevOps Team"
            }
        }
        """;

    public TaskExecutionControllerTests()
    {
        var mockConfig = new Mock<IConfiguration>();
        var mockHostingEnvironment = new Mock<IHostEnvironment>();
        mockHostingEnvironment.Setup(h => h.EnvironmentName).Returns("Development");

        var mockTaskObjectCreationService = new Mock<ITaskObjectCreationService>();
        mockTaskObjectCreationService.SetupAllProperties();
        mockTaskObjectCreationService
            .Setup(s => s.CreateTaskObjectAsync())
            .ReturnsAsync(() => new GenericWorkflowTask(_validJson, mockTaskObjectCreationService.Object.Instance));

        _controller = new TaskExecutionController(mockConfig.Object, mockHostingEnvironment.Object, mockTaskObjectCreationService.Object);
    }

    private static TaskInstance CreateValidTaskInstance()
    {
        return new TaskInstance
        {
            RunId = Guid.NewGuid().ToString(),
            Status = eTaskStatus.ReadyToRun,
            EffectiveDate = DateTime.Today,
            IsManual = false,
            Instance = new ScheduledTask
            {
                TaskId = 1,
                TaskName = "TestTask",
                WebService = "WebService1",
                CronExpression = "*/5 * * * *",
                Description = "Test task description",
                LastRunTime = DateTime.Now.AddMinutes(-10),
                Status = "Completed",
                DayOffset = -1,
                TaskJsonDefinitionId = 100
            }
        };
    }

    [Fact]
    public async Task ExecuteTask_ValidTaskInstance_ReturnsOkResult()
    {
        var taskInstance = CreateValidTaskInstance();

        var result = await _controller.ExecuteTask(taskInstance);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ExecuteTask_ValidTaskInstance_ReturnsCorrectMessage()
    {
        var taskInstance = CreateValidTaskInstance();

        var result = await _controller.ExecuteTask(taskInstance) as OkObjectResult;

        Assert.NotNull(result);
        var value = result.Value!;
        var message = value.GetType().GetProperty("message")!.GetValue(value) as string;
        Assert.Contains("TestTask", message!);
    }

    [Fact]
    public async Task ExecuteTask_DefaultTaskInstance_ReturnsOk()
    {
        var taskInstance = new TaskInstance();

        var result = await _controller.ExecuteTask(taskInstance);

        Assert.IsType<OkObjectResult>(result);
    }
}
