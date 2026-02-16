using Microsoft.Data.SqlClient;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Models.Enums;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Helpers;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class DatasourceDefinition: IDefinition
{
    public bool IsActive { get; set; } = true;
    public string BlockName{ get; set; } = String.Empty;
    public eOnError OnError { get; set; } = eOnError.AbortTask;
    public eTaskStatus Status { get; set; }

    public string Type { get; set; }
    public string Name { get; set; }
    public string Database { get; set; }
    public string DSTable { get; set; }
    public List<SqlParameter> Params { get; set; }

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance, IServiceProvider serviceProvider)
    {
        //var connString = ConnectionStringHelper.GetConnectionString(Database);
        Console.Write($"RunId: {taskInstance.RunId}  Running {GetType().Name}..");
    }
}
