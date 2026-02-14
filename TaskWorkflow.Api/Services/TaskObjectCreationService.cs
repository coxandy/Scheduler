using TaskWorkflow.Api.Interfaces;
using TaskWorkflow.TaskFactory.Tasks;
using TaskWorkflow.Common.Models;
using System.Text.Json;
using TaskWorkflow.TaskFactory.Tasks.Base;

namespace TaskWorkflow.Api.Services;

public class TaskObjectCreationService: ITaskObjectCreationService
{
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _hostingEnvironment;

    public string Json { get; set; } = String.Empty;
    public TaskInstance Instance { get; set; } = new(); 

    public TaskObjectCreationService(IConfiguration config, IHostEnvironment hostingEnvironment)
    {
        _config = config;
        _hostingEnvironment = hostingEnvironment;
    }

    public async Task<BaseTask> CreateTaskObjectAsync()
    {
        if (String.IsNullOrEmpty(Json)) throw new JsonException("TaskObjectCreationService - Task Json is empty");
        return new GenericWorkflowTask(Json, Instance);
    }
}