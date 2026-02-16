using System.Data;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Tasks;
using TaskWorkflow.Common.Models;
using Xunit;

namespace TaskWorkflow.UnitTests.DefinitionBlockTests;

public class DatasourceDefinitionTests
{
    private static TaskInstance GetTaskInstance() => new TaskInstance
    {
        EffectiveDate = new DateTime(2026, 10, 5),
        RunId = Guid.CreateVersion7().ToString(),
        IsManual = false,
        EnvironmentName = "Development"
    };

    private static string GetExitDefinitionJson() => """
                "ExitDefinition": {
                    "isActive": true,
                    "success": { "email": true, "to": ["admin@test.com"], "subject": "Task Succeeded", "body": "Completed", "priority": "Normal", "attachments": [] },
                    "failure": { "email": true, "to": ["admin@test.com"], "subject": "Task Failed", "body": "Error", "priority": "High", "attachments": [] }
                }
        """;

    private static string GetDatasourceJson() => """
                "DatasourceDefinition": {
                    "Type": "StoredProc",
                    "Name": "sp_GetUsers",
                    "Database": "Suppliers",
                    "DSTable": "users",
                    "Params": [
                        {
                            "ParameterName": "@EmployeeID",
                            "SqlDbType": "Int",
                            "Value": 101
                        },
                        {
                            "ParameterName": "@Note",
                            "SqlDbType": "NVarChar",
                            "Size": 500,
                            "Value": "Annual bonus applied."
                        },
                        {
                            "ParameterName": "@BonusAmount",
                            "SqlDbType": "Decimal",
                            "Value": 1500.5
                        },
                        {
                            "ParameterName": "@EffectiveDate",
                            "SqlDbType": "DateTimeOffset",
                            "Value": "2023-10-27T10:30:00"
                        },
                        {
                            "ParameterName": "@ManagerID",
                            "SqlDbType": "Int",
                            "Value": null
                        }
                    ]
                }
        """;

    private static List<IDefinition> ParseAndDeserialize(string json)
    {
        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json, instance.EffectiveDate, instance.EnvironmentName);
        VariableDefinition VariableDefinitionBlock = JsonParser.VerifyJson();
        if (VariableDefinitionBlock != null)
        {
            var variables = VariableDefinitionBlock.Variables;
            json = JsonParser.ApplyVariableReplacementsToJson(json, VariableDefinitionBlock);
        }
        return JsonParser.DeserializeDefinitionBlocks(json);
    }

    [Fact]
    public void DatasourceDefinition_DeserializesCorrectly()
    {
        var json = $$"""
            {
                {{GetDatasourceJson()}},
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.Equal(2, result.Count);
        var ds = Assert.IsType<DatasourceDefinition>(result[0]);
        Assert.Equal("StoredProc", ds.Type);
        Assert.Equal("sp_GetUsers", ds.Name);
        Assert.Equal("Suppliers", ds.Database);
        Assert.Equal("users", ds.DSTable);
    }

    [Fact]
    public void DatasourceDefinition_DeserializesAllParameters()
    {
        var json = $$"""
            {
                {{GetDatasourceJson()}},
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var ds = Assert.IsType<DatasourceDefinition>(result[0]);

        Assert.NotNull(ds.Params);
        Assert.Equal(5, ds.Params.Count);
    }

    [Fact]
    public void DatasourceDefinition_ParameterNames_AreCorrect()
    {
        var json = $$"""
            {
                {{GetDatasourceJson()}},
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var ds = Assert.IsType<DatasourceDefinition>(result[0]);

        Assert.Equal("@EmployeeID", ds.Params[0].ParameterName);
        Assert.Equal("@Note", ds.Params[1].ParameterName);
        Assert.Equal("@BonusAmount", ds.Params[2].ParameterName);
        Assert.Equal("@EffectiveDate", ds.Params[3].ParameterName);
        Assert.Equal("@ManagerID", ds.Params[4].ParameterName);
    }

    [Fact]
    public void DatasourceDefinition_ParameterSqlDbTypes_AreCorrect()
    {
        var json = $$"""
            {
                {{GetDatasourceJson()}},
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var ds = Assert.IsType<DatasourceDefinition>(result[0]);

        Assert.Equal(SqlDbType.Int, ds.Params[0].SqlDbType);
        Assert.Equal(SqlDbType.NVarChar, ds.Params[1].SqlDbType);
        Assert.Equal(SqlDbType.Decimal, ds.Params[2].SqlDbType);
        Assert.Equal(SqlDbType.DateTimeOffset, ds.Params[3].SqlDbType);
        Assert.Equal(SqlDbType.Int, ds.Params[4].SqlDbType);
    }

    [Fact]
    public void DatasourceDefinition_ParameterValues_AreCorrect()
    {
        var json = $$"""
            {
                {{GetDatasourceJson()}},
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var ds = Assert.IsType<DatasourceDefinition>(result[0]);

        Assert.Equal(101L, ds.Params[0].Value);
        Assert.Equal("Annual bonus applied.", ds.Params[1].Value);
        Assert.Equal(1500.5, ds.Params[2].Value);
        Assert.Equal("2023-10-27T10:30:00", ds.Params[3].Value);
        Assert.Equal(DBNull.Value, ds.Params[4].Value);
    }

    [Fact]
    public void DatasourceDefinition_ParameterSize_IsOptional()
    {
        var json = $$"""
            {
                {{GetDatasourceJson()}},
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var ds = Assert.IsType<DatasourceDefinition>(result[0]);

        Assert.Equal(0, ds.Params[0].Size);
        Assert.Equal(500, ds.Params[1].Size);
        Assert.Equal(0, ds.Params[2].Size);
    }

    [Fact]
    public void DatasourceDefinition_WithNumericSuffix_SupportsMultiple()
    {
        var json = $$"""
            {
                "DatasourceDefinition1": {
                    "Type": "StoredProc",
                    "Name": "sp_GetUsers",
                    "Database": "Suppliers",
                    "DSTable": "users",
                    "Params": []
                },
                "DatasourceDefinition2": {
                    "Type": "StoredProc",
                    "Name": "sp_GetOrders",
                    "Database": "Sales",
                    "DSTable": "orders",
                    "Params": []
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.Equal(3, result.Count);
        var ds1 = Assert.IsType<DatasourceDefinition>(result[0]);
        var ds2 = Assert.IsType<DatasourceDefinition>(result[1]);
        Assert.Equal("sp_GetUsers", ds1.Name);
        Assert.Equal("sp_GetOrders", ds2.Name);
        Assert.Equal("DatasourceDefinition1", ds1.BlockName);
        Assert.Equal("DatasourceDefinition2", ds2.BlockName);
    }

    [Fact]
    public void DatasourceDefinition_WithVariableReplacement_ReplacesTokens()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": {
                        "<@@ProcName@@>": "sp_UpdateInventory",
                        "<@@DbName@@>": "Warehouse"
                    },
                    "IsActive": true
                },
                "DatasourceDefinition": {
                    "Type": "StoredProc",
                    "Name": "<@@ProcName@@>",
                    "Database": "<@@DbName@@>",
                    "DSTable": "inventory",
                    "Params": []
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var ds = result[1] as DatasourceDefinition;

        Assert.NotNull(ds);
        Assert.Equal("sp_UpdateInventory", ds.Name);
        Assert.Equal("Warehouse", ds.Database);
    }

    [Fact]
    public void DatasourceDefinition_IsActiveDefaultsToTrue()
    {
        var ds = new DatasourceDefinition();
        Assert.True(ds.IsActive);
    }
}
