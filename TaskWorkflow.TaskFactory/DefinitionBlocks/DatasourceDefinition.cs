using System.Data;
using Microsoft.Data.SqlClient;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Helpers;
using TaskWorkflow.Common.Models.BlockDefinition;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class DatasourceDefinition: IDefinition
{
    public bool IsActive { get; set; } = true;
    public string BlockName{ get; set; } = String.Empty;
    public eOnError OnError { get; set; } = eOnError.AbortTask;
    public eTaskStatus Status { get; set; }

    public List<DataSource> DataSources { get; set; }

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance, IServiceProvider serviceProvider)
    {
        foreach (var ds in DataSources)
        {
            if (ds.Type == eDatasourceTypeType.StoredProc)
            {
                var connString = ConnectionStringHelper.GetConnectionString(ds.Database);
                List<DataTable> tables = await GetDataFromStoredProcAsync(ds, connString);
            }
            Console.Write($"RunId: {taskInstance.RunId}  Running {GetType().Name}..");
        }
    }

    private async Task<List<DataTable>> GetDataFromStoredProcAsync(DataSource dataSource, string connectionString)
    {
        var dataTables = new List<DataTable>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(dataSource.Name, connection);
        command.CommandType = CommandType.StoredProcedure;

        if (dataSource.Params != null)
        {
            foreach (var param in dataSource.Params)
            {
                command.Parameters.Add(param);
            }
        }

        await using var reader = await command.ExecuteReaderAsync();

        int resultSetIndex = 0;
        do
        {
            var tableName = resultSetIndex == 0
                ? dataSource.DSTableName
                : $"{dataSource.DSTableName}{resultSetIndex + 1}";

            var dataTable = new DataTable(tableName);
            dataTable.Load(reader);
            dataTables.Add(dataTable);
            resultSetIndex++;
        } while (await reader.NextResultAsync());

        return dataTables;
    }
}
