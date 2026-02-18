using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.Common.Models.BlockDefinition;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Helpers;

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

    // Process any definition block errors by sending email
    protected async Task ProcessTaskErrorAsync(Exception ex, IDefinition defBlock)
    {
        Log.Error($"Error occured in '{defBlock.BlockName}': {ex.Message}");

        // Report error -- email
        if ((defBlock.OnError == eOnError.AbortTaskAndReportError) || (defBlock.OnError == eOnError.SkipAndReportError)) 
        {
            await SendTaskErrorMessageAsync(ex, defBlock);
        }
    }

    private async Task SendTaskErrorMessageAsync(Exception ex, IDefinition defBlock)
    {
        Message msg = new Message();
        msg.SendEmail = true;
        msg.Priority = "High";
            
        var exitDefinitionBlock = (ExitDefinition)DefinitionBlocks.FirstOrDefault(x => x.BlockName.ToUpper().StartsWith("ExitDefinition".ToUpper()));
        if ((exitDefinitionBlock != null) && (exitDefinitionBlock.Failure != null)) 
        {
            msg = exitDefinitionBlock.Failure;    
        }
        else
        {
            //======================================================================================
            // If ExitDefinition Failure email is not configured - use default exception email
            //======================================================================================
            msg.Subject = $"[{Instance.EnvironmentName}]\nError thrown in Task Name: {Instance.Instance.TaskName}";
            msg.Body = $"\n\nTask Id: {Instance.Instance.TaskId}\nTask Name: {Instance.Instance.TaskName}\nTask Error: {ex.Message}";
            msg.To.Add("coxandy@yahoo.com");
            msg.IncludeBanner = true;
            msg.BannerFilePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Images");
            msg.BannerFileName = "erroremailbanner.png";
            msg.BannerOverlayText = $"Task Error: {Instance.Instance.TaskName}";
        }

        //send email
        if (msg.SendEmail)
        {
            await CommonEmailHelper.SendEmailAsync(msg, TaskContext);
        }
    }
}