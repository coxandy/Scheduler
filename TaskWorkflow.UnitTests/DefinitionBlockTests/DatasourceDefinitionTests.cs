using System.Data;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

namespace TaskWorkflow.UnitTests.DefinitionBlockTests;

public class DatasourceDefinitionTests
{
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
    public async Task DatasourceDefinition_LimitColumns_ReturnsOnlySpecifiedColumns()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"dstest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var csvContent = """
                Name,Age,City,Salary,Department
                Alice,30,London,50000,Engineering
                Bob,25,Paris,45000,Marketing
                Charlie,35,Berlin,60000,Engineering
                """;
            File.WriteAllText(Path.Combine(tempDir, "employees.csv"), csvContent);

            var jsonSafePath = tempDir.Replace("\\", "\\\\");
            var json = $$"""
                {
                    "DatasourceDefinition": {
                        "DataSources": [
                            {
                                "Type": "CsvFile",
                                "DSTableName": "Employees",
                                "CsvFilePath": "{{jsonSafePath}}",
                                "CsvFileName": "employees.csv",
                                "CsvFileHeader": true,
                                "LimitColumns": ["Name", "Department", "Salary"]
                            }
                        ]
                    },
                    {{GetExitDefinitionJson()}}
                }
                """;

            var task = CreateTask(json);
            await task.Run();

            var table = task.GetTaskContext().GetDataTable("Employees");

            Assert.NotNull(table);
            Assert.Equal(3, table.Columns.Count);
            Assert.Equal("Name", table.Columns[0].ColumnName);
            Assert.Equal("Department", table.Columns[1].ColumnName);
            Assert.Equal("Salary", table.Columns[2].ColumnName);
            Assert.Equal(3, table.Rows.Count);

            Assert.Equal("Alice", table.Rows[0]["Name"].ToString());
            Assert.Equal("Engineering", table.Rows[0]["Department"].ToString());
            Assert.Equal("50000", table.Rows[0]["Salary"].ToString());

            Assert.Equal("Bob", table.Rows[1]["Name"].ToString());
            Assert.Equal("Marketing", table.Rows[1]["Department"].ToString());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DatasourceDefinition_WhereFilter_ReturnsOnlyMatchingRows()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"dstest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var csvContent = """
                Product,Category,Price,InStock
                Widget A,Electronics,29.99,True
                Widget B,Electronics,49.99,False
                Gadget C,Home,15.00,True
                Gadget D,Home,22.50,True
                Gadget E,Electronics,99.99,True
                """;
            File.WriteAllText(Path.Combine(tempDir, "products.csv"), csvContent);

            var jsonSafePath = tempDir.Replace("\\", "\\\\");
            var json = $$"""
                {
                    "DatasourceDefinition": {
                        "DataSources": [
                            {
                                "Type": "CsvFile",
                                "DSTableName": "Products",
                                "CsvFilePath": "{{jsonSafePath}}",
                                "CsvFileName": "products.csv",
                                "CsvFileHeader": true,
                                "WhereFilter": "Category = 'Electronics' AND InStock = 'True'"
                            }
                        ]
                    },
                    {{GetExitDefinitionJson()}}
                }
                """;

            var task = CreateTask(json);
            await task.Run();

            var table = task.GetTaskContext().GetDataTable("Products");

            Assert.NotNull(table);
            Assert.Equal(4, table.Columns.Count);
            Assert.Equal(2, table.Rows.Count);

            Assert.Equal("Widget A", table.Rows[0]["Product"].ToString());
            Assert.Equal("29.99", table.Rows[0]["Price"].ToString());

            Assert.Equal("Gadget E", table.Rows[1]["Product"].ToString());
            Assert.Equal("99.99", table.Rows[1]["Price"].ToString());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
