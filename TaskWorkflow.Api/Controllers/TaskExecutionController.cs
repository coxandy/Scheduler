using TaskWorkflow.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace TaskWorkflow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskExecutionController : ControllerBase
{
    private readonly IConfiguration _config;
    public TaskExecutionController(IConfiguration config)
    {
        _config = config;
    }
    
    [HttpPost("ExecuteTask")]
    public IActionResult ExecuteTask([FromBody] TaskInstance taskInstance)
    {
        Log.Information("Received task '{TaskName}' for WebService '{WebService}'", taskInstance.Instance.TaskName, taskInstance.Instance.WebService);
        Log.Information($"RunId: '{taskInstance.RunId}'");
        Log.Information($"EffectiveDate: '{taskInstance.EffectiveDate}'");
        Log.Information($"IsManual: '{taskInstance.IsManual}'");
        return Ok(new { message = $"Task '{taskInstance.Instance.TaskName}' received successfully" });
    }
}
