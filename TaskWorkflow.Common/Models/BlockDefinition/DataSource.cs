using Microsoft.Data.SqlClient;
using TaskWorkflow.Common.Models.Enums;

namespace TaskWorkflow.Common.Models.BlockDefinition;

public class DataSource
{
    public eDatasourceTypeType Type { get; set; }
    public string Name { get; set; }
    public string Database { get; set; }
    public string DSTableName { get; set; }
    public List<SqlParameter> Params { get; set; }
}