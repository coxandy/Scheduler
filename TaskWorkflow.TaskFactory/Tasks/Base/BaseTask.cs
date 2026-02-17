using System.Text.Json;
using System.Text.Json.Serialization;
using System.Data;
using Serilog;
using TaskWorkflow.Common.Models;
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

    private static readonly Dictionary<string, Type> _definitionBlockTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "VariableDefinition", typeof(VariableDefinition) },
        { "ClassDefinition", typeof(ClassDefinition) },
        { "SchemaDefinition", typeof(SchemaDefinition) },
        { "DatasourceDefinition", typeof(DatasourceDefinition) },
        { "ExitDefinition", typeof(ExitDefinition) }
    };

    private readonly string _json;

    //protected properties
    protected TaskInstance Instance;
    protected List<IDefinition> DefinitionBlocks;
    protected Dictionary<string, object> Variables { get; set; } = new();
    protected List<DataTable> TaskDataTables { get; set; } = new();
    protected IServiceProvider ServiceProvider { get; set; }

    public abstract Task Run();

    // Internal accessors for unit testing
    internal List<IDefinition> GetDefinitionBlocks() => DefinitionBlocks;
    internal void SetDefinitionBlocks(List<IDefinition> blocks) => DefinitionBlocks = blocks;
    internal Dictionary<string, object> GetVariables() => Variables;
    internal List<DataTable> GetTaskDataTables() => TaskDataTables;
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
            // Assign variables to class
            this.Variables = VariableDefinitionBlock.Variables;

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