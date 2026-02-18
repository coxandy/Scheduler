using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Interfaces;

namespace TaskWorkflow.TaskFactory.Tasks.Base;

public abstract class BaseTask
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _json;

    //protected properties
    protected TaskInstance Instance;
    protected List<IDefinition> DefinitionBlocks;
    protected TaskContext TaskContext { get; set; } = new();
    protected IServiceProvider ServiceProvider { get; set; }

    public abstract Task Run();

    // Internal accessors for unit testing
    internal List<IDefinition> GetDefinitionBlocks() => DefinitionBlocks;
    internal void SetDefinitionBlocks(List<IDefinition> blocks) => DefinitionBlocks = blocks;
    internal TaskContext GetTaskContext() => TaskContext;
    internal IServiceProvider GetServiceProvider() => ServiceProvider;
    internal TaskInstance GetTaskInstance() => Instance;

    public BaseTask(string json, TaskInstance taskInstance, IServiceProvider serviceProvider)
    {
        _json = json;
        this.Instance = taskInstance;
        this.ServiceProvider = serviceProvider;
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(_json, taskInstance.EffectiveDate, taskInstance.EnvironmentName);
        
        //====================================================================================
        // VariableDefinition is exceptional because it has to be processed prior to the 
        // Task Json being deserialized into definition blocks
        // Verify Json format and return VariableDefintion if it exists
        //====================================================================================
        VariableDefinition VariableDefinitionBlock = JsonParser.VerifyJson();
        if (VariableDefinitionBlock != null)
        {
            // Assign variables to TaskContext
            foreach (var variable in VariableDefinitionBlock.Variables)
            {
                this.TaskContext.SetVariable(variable.Key, variable.Value);
            }

            // Apply variable replacement to Json
            _json = JsonParser.ApplyVariableReplacementsToJson(_json, VariableDefinitionBlock);
        }

        // Get final block definition
        this.DefinitionBlocks = JsonParser.DeserializeDefinitionBlocks(_json);
    }

    // Process any definition block errors
    protected async Task ProcessTaskErrorAsync(Exception ex, IDefinition defBlock)
    {
        Log.Error($"Error occured in '{defBlock.BlockName}': {ex.Message}");
        //email
    }
}