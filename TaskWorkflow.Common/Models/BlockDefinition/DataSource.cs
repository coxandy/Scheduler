using System.Data;
using Microsoft.Data.SqlClient;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;

namespace TaskWorkflow.Common.Models.BlockDefinition;

public class DataSource
{
    public eDatasourceTypeType Type { get; set; }
    public string? Name { get; set; }
    public string? Database { get; set; }
    public string? DSTableName { get; set; }
    public List<SqlParameter>? Params { get; set; }

    public bool HasFileHeader { get; set; }
    public string? CsvFilePath { get; set; }
    public string? CsvFileName { get; set; }
    public char CsvFileDelimiter { get; set; }

    // Limit/Re-order columns in Datatable to 'LimitColumns' List -- exclude those columns not included
    public List<string?> LimitColumns { get; set; }

    // Like a Sql 'Where' filter - to filter DataTable rows
    public string? WhereFilter { get; set; }

    // New columns to be added to the Datatable
    public List<DataColumn>? AdditionalColumns { get; set; }
}