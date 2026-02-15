using TaskWorkflow.Api.Interfaces;
using TaskWorkflow.TaskFactory.Tasks;
using TaskWorkflow.Common.Models;
using System.Text.Json;
using System.Reflection;
using TaskWorkflow.TaskFactory.Tasks.Base;

namespace TaskWorkflow.Api.Services;

public class TaskObjectCreationService: ITaskObjectCreationService
{
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _hostingEnvironment;
    private readonly IServiceProvider _serviceProvider;

    public string Json { get; set; } = String.Empty;
    public TaskInstance Instance { get; set; } = new();

    public TaskObjectCreationService(IConfiguration config, IHostEnvironment hostingEnvironment, IServiceProvider serviceProvider)
    {
        _config = config;
        _hostingEnvironment = hostingEnvironment;
        _serviceProvider = serviceProvider;
    }

    public async Task<BaseTask> CreateTaskObjectAsync(string taskClassName = "GenericWorkflowTask")
    {
        if (String.IsNullOrEmpty(Json)) throw new JsonException("TaskObjectCreationService - Task Json is empty");

        // Find the task type by name in the TaskFactory assembly
        var taskFactoryAssembly = Assembly.GetAssembly(typeof(BaseTask))
            ?? throw new InvalidOperationException("Could not load TaskFactory assembly.");

        var taskType = taskFactoryAssembly.GetTypes()
            .FirstOrDefault(t => t.Name.Equals(taskClassName, StringComparison.OrdinalIgnoreCase)
                && t.IsSubclassOf(typeof(BaseTask))
                && !t.IsAbstract)
            ?? throw new TypeLoadException($"Task class '{taskClassName}' not found or does not inherit from BaseTask.");

        var instance = ActivatorUtilities.CreateInstance(_serviceProvider, taskType, Json, Instance) as BaseTask
            ?? throw new InvalidOperationException($"Failed to create instance of '{taskClassName}'.");

        return instance;
    }
}