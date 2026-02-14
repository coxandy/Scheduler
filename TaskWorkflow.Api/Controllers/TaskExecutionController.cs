using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.TestRunData;
using TaskWorkflow.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace TaskWorkflow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskExecutionController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _hostingEnvironment;
    private readonly ITaskObjectCreationService _taskObjectCreationService;
    public TaskExecutionController(IConfiguration config, IHostEnvironment hostingEnvironment, ITaskObjectCreationService taskObjectCreationService)
    {
        _config = config;
        _hostingEnvironment = hostingEnvironment;
        _taskObjectCreationService = taskObjectCreationService;
    }
    
    [HttpPost("ExecuteTask")]
    public async Task<IActionResult> ExecuteTask([FromBody] TaskInstance taskInstance)
    {
        Log.Information("TaskExecutionController - Received task '{TaskName}' for WebService '{WebService}'", taskInstance.Instance.TaskName, taskInstance.Instance.WebService);
        Log.Information("======================================================================================================");
        Log.Information($"RunId: '{taskInstance.RunId}'");
        Log.Information($"EffectiveDate: '{taskInstance.EffectiveDate}'");
        Log.Information($"IsManual: '{taskInstance.IsManual}'");
        Log.Information($"EnvName: '{taskInstance.EnvironmentName}'");
        Log.Information("======================================================================================================");

        long taskJsonDefinitionId = 1; //taskInstance.Instance.TaskJsonDefinitionId;
        string json = await TestDataHelper.GetTestTaskWorkflowJson(taskJsonDefinitionId);

        //===============================================================================
        // Use _taskObjectCreationService to create object to execute
        //===============================================================================
        _taskObjectCreationService.Instance = taskInstance;
        _taskObjectCreationService.Json = json;
        var task = await _taskObjectCreationService.CreateTaskObjectAsync();
        await task.Run();

        return Ok(new { message = $"Task '{taskInstance.Instance.TaskName}' received successfully" });
    }
}
