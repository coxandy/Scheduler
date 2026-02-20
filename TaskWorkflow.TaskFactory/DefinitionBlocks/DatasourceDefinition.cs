using Serilog;
using System.Data;
using Microsoft.Data.SqlClient;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Helpers;
using TaskWorkflow.Common.Models.BlockDefinition;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class DatasourceDefinition: IDefinition
{
    public bool IsActive { get; set; } = true;
    public string BlockName{ get; set; } = String.Empty;
    public eOnError OnError { get; set; } = eOnError.AbortTaskAndReportError;
    public eTaskStatus Status { get; set; }

    public List<DataSource> DataSources { get; set; }

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance, IServiceProvider serviceProvider, TaskContext taskContext)
    {
        Log.Debug($"RunDefinitionBlockAsync() - RunId: {taskInstance.RunId}  Running {GetType().Name}..");
        foreach (var ds in DataSources)
        {
            List<DataTable> tables = new List<DataTable>();
            await Task.Run(async () =>
            {
                switch (ds.Type)
                {
                    case eDatasourceTypeType.StoredProc:
                        {
                            var connString = ConnectionStringHelper.GetConnectionString(ds.Database);
                            tables = await ProcessStoredProcAsync(ds, connString); // can handle multiple recordsets
                            break;
                        }
                    case eDatasourceTypeType.CsvFile:
                        {
                            tables.Add(await ProcessCsvFileAsync(ds));
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(ds.Type), $"Unknown datasource type: {ds.Type}");
                }

                // Perform any row filtering
                if ((tables.Count == 1) && (!String.IsNullOrEmpty(ds.WhereFilter)))
                {
                    tables[0] = await CommonDataTableHelper.WhereFilter(tables[0], ds.WhereFilter);
                }

                // Perform any column filtering/re-ordering
                if ((tables.Count == 1) && (ds.LimitColumns?.Any() == true))
                {
                    tables[0] = await CommonDataTableHelper.LimitColumns(tables[0], ds.LimitColumns);
                }

                taskContext.AddDataTables(tables);
            });
        }
    }


    private async Task<DataTable> ProcessCsvFileAsync(DataSource dataSource)
    {
        var filePath = Path.Combine(dataSource.CsvFilePath, dataSource.CsvFileName);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"CSV file not found: '{filePath}'");

        var delimiter = dataSource.CsvFileDelimiter == default ? ',' : dataSource.CsvFileDelimiter;
        var rows = CommonFileHelper.ReadDelimitedFile(filePath, delimiter);

        if (rows.Count == 0)
            throw new FormatException($"CSV file is empty: '{filePath}'");

        var dt = new DataTable(dataSource.DSTableName);
        int startIndex = 0;

        if (dataSource.CsvFileHeader)
        {
            foreach (var header in rows[0])
                dt.Columns.Add(header);
            startIndex = 1;
        }
        else
        {
            for (int i = 0; i < rows[0].Length; i++)
                dt.Columns.Add($"Column{i + 1}");
        }

        for (int r = startIndex; r < rows.Count; r++)
        {
            var dataRow = dt.NewRow();
            for (int i = 0; i < Math.Min(rows[r].Length, dt.Columns.Count); i++)
                dataRow[i] = rows[r][i];
            dt.Rows.Add(dataRow);
        }

        return dt;
    }

    private async Task<List<DataTable>> ProcessStoredProcAsync(DataSource dataSource, string connectionString)
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
            //Add numeric suffix for multiple datatables
            var tableName = resultSetIndex == 0
                ? dataSource.DSTableName
                : $"{dataSource.DSTableName}{resultSetIndex + 1}";

            var dataTable = new DataTable(tableName);
            dataTable.Load(reader);
            dataTables.Add(dataTable);
            resultSetIndex++;
        } while (await reader.NextResultAsync());

        // If only one datatable then no need for numeric suffix
        if (dataTables.Count == 1) dataTables[0].TableName = dataSource.DSTableName;
        
        return dataTables;
    }
}
