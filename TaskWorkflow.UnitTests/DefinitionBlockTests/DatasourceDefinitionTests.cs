using System.Data;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Tasks;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.Enums;
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
                    "DataSources": [
                        {
                            "Type": "StoredProc",
                            "Name": "sp_GetUsers",
                            "Database": "Suppliers",
                            "DSTableName": "users",
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
                        },
                        {
                            "Type": "StoredProc",
                            "Name": "sp_GetUserSuppliers",
                            "Database": "Suppliers",
                            "DSTableName": "user_suppliers",
                            "Params": [
                                {
                                    "ParameterName": "@SupplierID",
                                    "SqlDbType": "Int",
                                    "Value": 27
                                },
                                {
                                    "ParameterName": "@Note",
                                    "SqlDbType": "NVarChar",
                                    "Size": 30,
                                    "Value": "Annual bonus applied."
                                }
                            ]
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
        Assert.NotNull(ds.DataSources);
        Assert.Equal(2, ds.DataSources.Count);
        Assert.Equal(eDatasourceTypeType.StoredProc, ds.DataSources[0].Type);
        Assert.Equal("sp_GetUsers", ds.DataSources[0].Name);
        Assert.Equal("Suppliers", ds.DataSources[0].Database);
        Assert.Equal("users", ds.DataSources[0].DSTableName);
        Assert.Equal(eDatasourceTypeType.StoredProc, ds.DataSources[1].Type);
        Assert.Equal("sp_GetUserSuppliers", ds.DataSources[1].Name);
        Assert.Equal("Suppliers", ds.DataSources[1].Database);
        Assert.Equal("user_suppliers", ds.DataSources[1].DSTableName);
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

        Assert.NotNull(ds.DataSources[0].Params);
        Assert.Equal(5, ds.DataSources[0].Params.Count);
        Assert.NotNull(ds.DataSources[1].Params);
        Assert.Equal(2, ds.DataSources[1].Params.Count);
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

        Assert.Equal("@EmployeeID", ds.DataSources[0].Params[0].ParameterName);
        Assert.Equal("@Note", ds.DataSources[0].Params[1].ParameterName);
        Assert.Equal("@BonusAmount", ds.DataSources[0].Params[2].ParameterName);
        Assert.Equal("@EffectiveDate", ds.DataSources[0].Params[3].ParameterName);
        Assert.Equal("@ManagerID", ds.DataSources[0].Params[4].ParameterName);

        Assert.Equal("@SupplierID", ds.DataSources[1].Params[0].ParameterName);
        Assert.Equal("@Note", ds.DataSources[1].Params[1].ParameterName);
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

        Assert.Equal(SqlDbType.Int, ds.DataSources[0].Params[0].SqlDbType);
        Assert.Equal(SqlDbType.NVarChar, ds.DataSources[0].Params[1].SqlDbType);
        Assert.Equal(SqlDbType.Decimal, ds.DataSources[0].Params[2].SqlDbType);
        Assert.Equal(SqlDbType.DateTimeOffset, ds.DataSources[0].Params[3].SqlDbType);
        Assert.Equal(SqlDbType.Int, ds.DataSources[0].Params[4].SqlDbType);

        Assert.Equal(SqlDbType.Int, ds.DataSources[1].Params[0].SqlDbType);
        Assert.Equal(SqlDbType.NVarChar, ds.DataSources[1].Params[1].SqlDbType);
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

        Assert.Equal(101L, ds.DataSources[0].Params[0].Value);
        Assert.Equal("Annual bonus applied.", ds.DataSources[0].Params[1].Value);
        Assert.Equal(1500.5, ds.DataSources[0].Params[2].Value);
        Assert.Equal("2023-10-27T10:30:00", ds.DataSources[0].Params[3].Value);
        Assert.Equal(DBNull.Value, ds.DataSources[0].Params[4].Value);

        Assert.Equal(27L, ds.DataSources[1].Params[0].Value);
        Assert.Equal("Annual bonus applied.", ds.DataSources[1].Params[1].Value);
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

        Assert.Equal(0, ds.DataSources[0].Params[0].Size);
        Assert.Equal(500, ds.DataSources[0].Params[1].Size);
        Assert.Equal(0, ds.DataSources[0].Params[2].Size);

        Assert.Equal(0, ds.DataSources[1].Params[0].Size);
        Assert.Equal(30, ds.DataSources[1].Params[1].Size);
    }

    [Fact]
    public void DatasourceDefinition_WithNumericSuffix_SupportsMultiple()
    {
        var json = $$"""
            {
                "DatasourceDefinition1": {
                    "DataSources": [
                        {
                            "Type": "StoredProc",
                            "Name": "sp_GetUsers",
                            "Database": "Suppliers",
                            "DSTableName": "users",
                            "Params": []
                        }
                    ]
                },
                "DatasourceDefinition2": {
                    "DataSources": [
                        {
                            "Type": "StoredProc",
                            "Name": "sp_GetOrders",
                            "Database": "Sales",
                            "DSTableName": "orders",
                            "Params": []
                        }
                    ]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.Equal(3, result.Count);
        var ds1 = Assert.IsType<DatasourceDefinition>(result[0]);
        var ds2 = Assert.IsType<DatasourceDefinition>(result[1]);
        Assert.Equal("sp_GetUsers", ds1.DataSources[0].Name);
        Assert.Equal("sp_GetOrders", ds2.DataSources[0].Name);
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
                    "DataSources": [
                        {
                            "Type": "StoredProc",
                            "Name": "<@@ProcName@@>",
                            "Database": "<@@DbName@@>",
                            "DSTableName": "inventory",
                            "Params": []
                        }
                    ]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var ds = result[1] as DatasourceDefinition;

        Assert.NotNull(ds);
        Assert.Equal("sp_UpdateInventory", ds.DataSources[0].Name);
        Assert.Equal("Warehouse", ds.DataSources[0].Database);
    }

    [Fact]
    public void DatasourceDefinition_IsActiveDefaultsToTrue()
    {
        var ds = new DatasourceDefinition();
        Assert.True(ds.IsActive);
    }
}
