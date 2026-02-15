using System.Text.Json;
using System.Text.Json.Serialization;
using TaskWorkflow.Common.Models;
using TaskWorkflow.TaskFactory.Tasks;
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
        { "SchemaDefinition", typeof(SchemaDefinition) }
    };

    private readonly string _json;

    //protected properties
    protected TaskInstance Instance;
    protected List<IDefinition> DefinitionBlocks;
    protected Dictionary<string, object> Variables { get; set; } = new();

    public abstract Task Run();

    public BaseTask(string json, TaskInstance taskInstance)
    {
        _json = json;
        this.Instance = taskInstance;
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(_json);
        
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
}