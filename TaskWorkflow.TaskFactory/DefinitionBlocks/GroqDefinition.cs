using Microsoft.Extensions.Configuration;
using Serilog;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.BlockDefinition;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using TaskWorkflow.Common.Helpers;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class GroqDefinition : IDefinition
{
    public bool IsActive { get; set; } = true;
    public string BlockName { get; set; } = string.Empty;
    public eOnError OnError { get; set; } = eOnError.AbortTaskAndReportError;
    
    

    public List<GroqQuestion> Questions { get; set; } = new();

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance, IServiceProvider serviceProvider, TaskContext taskContext)
    {
        Log.Debug($"RunDefinitionBlockAsync() - RunId: {taskInstance.RunId}  Running {GetType().Name}..");

        var config = serviceProvider?.GetService(typeof(IConfiguration)) as IConfiguration;
        var configApiKey = config?["Groq:ApiKey"];

        foreach (var question in Questions)
        {
            var dataTable = await CommonGroqHelper.SendGroqRequestAsync(question, configApiKey);
            taskContext.AddDataTable(dataTable);
        }
    }
}
