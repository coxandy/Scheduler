using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using TaskWorkflow.Api.Controllers;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.Enums;
using Xunit;

namespace TaskWorkflow.UnitTests.Controllers;

public class TaskExecutionControllerTests
{
    private readonly TaskExecutionController _controller;

    public TaskExecutionControllerTests()
    {
        var mockConfig = new Mock<IConfiguration>();
        _controller = new TaskExecutionController(mockConfig.Object);
    }

    private static TaskInstance CreateValidTaskInstance()
    {
        return new TaskInstance
        {
            RunId = Guid.NewGuid().ToString(),
            Status = eTaskStatus.ReadyToRun,
            dtEffective = DateTime.Today,
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
    public void ExecuteTask_ValidTaskInstance_ReturnsOkResult()
    {
        var taskInstance = CreateValidTaskInstance();

        var result = _controller.ExecuteTask(taskInstance);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void ExecuteTask_ValidTaskInstance_ReturnsCorrectMessage()
    {
        var taskInstance = CreateValidTaskInstance();

        var result = _controller.ExecuteTask(taskInstance) as OkObjectResult;

        Assert.NotNull(result);
        var value = result.Value!;
        var message = value.GetType().GetProperty("message")!.GetValue(value) as string;
        Assert.Contains("TestTask", message!);
    }

    [Fact]
    public void ExecuteTask_DefaultTaskInstance_ReturnsOk()
    {
        var taskInstance = new TaskInstance();

        var result = _controller.ExecuteTask(taskInstance);

        Assert.IsType<OkObjectResult>(result);
    }
}
