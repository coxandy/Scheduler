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

    public bool CsvFileHeader { get; set; }
    public string? CsvFilePath { get; set; }
    public string? CsvFileName { get; set; }
    public char CsvFileDelimiter { get; set; }
}