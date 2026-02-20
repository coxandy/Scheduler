using System.Data;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

namespace TaskWorkflow.UnitTests.DefinitionBlockTests;

public class PivotDefinitionTests
{
    [Fact]
    public void PivotDefinition_DeserializesCorrectly()
    {
        var json = $$"""
            {
                "PivotDefinition": {
                    "PivotSources": [
                        {
                            "DSTableSource": "SalesData",
                            "DSTableTarget": "SalesPivot",
                            "Rows": ["Region"],
                            "Columns": ["Product"],
                            "Data": ["Revenue"],
                            "AggregateFunction": "Sum"
                        }
                    ]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var task = CreateTask(json);
        var definitions = task.GetDefinitionBlocks();

        Assert.Equal(2, definitions.Count);
        var pivot = Assert.IsType<PivotDefinition>(definitions[0]);
        Assert.Single(pivot.PivotSources);
        Assert.Equal("SalesData", pivot.PivotSources[0].DSTableSource);
        Assert.Equal("SalesPivot", pivot.PivotSources[0].DSTableTarget);
        Assert.Equal(new List<string> { "Region" }, pivot.PivotSources[0].Rows);
        Assert.Equal(new List<string> { "Product" }, pivot.PivotSources[0].Columns);
        Assert.Equal(new List<string> { "Revenue" }, pivot.PivotSources[0].Data);
        Assert.Equal("Sum", pivot.PivotSources[0].AggregateFunction);
    }

    [Fact]
    public async Task CsvDatasource_PivotSum_ProducesExpectedData()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"pivottest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var csvContent = """
                Region,Product,Revenue
                North,Widgets,100
                North,Widgets,150
                North,Gadgets,200
                South,Widgets,300
                South,Gadgets,50
                South,Gadgets,75
                """;
            File.WriteAllText(Path.Combine(tempDir, "sales.csv"), csvContent);

            var jsonSafePath = tempDir.Replace("\\", "\\\\");
            var json = $$"""
                {
                    "DatasourceDefinition": {
                        "DataSources": [
                            {
                                "Type": "CsvFile",
                                "DSTableName": "SalesData",
                                "CsvFilePath": "{{jsonSafePath}}",
                                "CsvFileName": "sales.csv",
                                "CsvFileHeader": true
                            }
                        ]
                    },
                    "PivotDefinition": {
                        "PivotSources": [
                            {
                                "DSTableSource": "SalesData",
                                "DSTableTarget": "SalesPivot",
                                "Rows": ["Region"],
                                "Columns": ["Product"],
                                "Data": ["Revenue"],
                                "AggregateFunction": "Sum"
                            }
                        ]
                    },
                    {{GetExitDefinitionJson()}}
                }
                """;

            var task = CreateTask(json);
            await task.Run();

            var taskContext = task.GetTaskContext();
            var pivotTable = taskContext.GetDataTable("SalesPivot");

            Assert.NotNull(pivotTable);
            Assert.Equal(2, pivotTable.Rows.Count);
            Assert.Contains("Region", pivotTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            Assert.Contains("Gadgets_Revenue", pivotTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            Assert.Contains("Widgets_Revenue", pivotTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));

            var north = pivotTable.AsEnumerable().Single(r => r["Region"].ToString() == "North");
            var south = pivotTable.AsEnumerable().Single(r => r["Region"].ToString() == "South");

            Assert.Equal(250.0, Convert.ToDouble(north["Widgets_Revenue"]));   // 100 + 150
            Assert.Equal(200.0, Convert.ToDouble(north["Gadgets_Revenue"]));   // 200
            Assert.Equal(300.0, Convert.ToDouble(south["Widgets_Revenue"]));   // 300
            Assert.Equal(125.0, Convert.ToDouble(south["Gadgets_Revenue"]));   // 50 + 75
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CsvDatasource_PivotCount_ProducesExpectedData()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"pivottest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var csvContent = """
                City,Category,Amount
                London,Food,10
                London,Food,20
                London,Transport,30
                Paris,Food,15
                Paris,Transport,25
                Paris,Transport,35
                Paris,Transport,45
                """;
            File.WriteAllText(Path.Combine(tempDir, "expenses.csv"), csvContent);

            var jsonSafePath = tempDir.Replace("\\", "\\\\");
            var json = $$"""
                {
                    "DatasourceDefinition": {
                        "DataSources": [
                            {
                                "Type": "CsvFile",
                                "DSTableName": "Expenses",
                                "CsvFilePath": "{{jsonSafePath}}",
                                "CsvFileName": "expenses.csv",
                                "CsvFileHeader": true
                            }
                        ]
                    },
                    "PivotDefinition": {
                        "PivotSources": [
                            {
                                "DSTableSource": "Expenses",
                                "DSTableTarget": "ExpensePivot",
                                "Rows": ["City"],
                                "Columns": ["Category"],
                                "Data": ["Amount"],
                                "AggregateFunction": "Count"
                            }
                        ]
                    },
                    {{GetExitDefinitionJson()}}
                }
                """;

            var task = CreateTask(json);
            await task.Run();

            var pivotTable = task.GetTaskContext().GetDataTable("ExpensePivot");

            Assert.NotNull(pivotTable);
            Assert.Equal(2, pivotTable.Rows.Count);

            var london = pivotTable.AsEnumerable().Single(r => r["City"].ToString() == "London");
            var paris = pivotTable.AsEnumerable().Single(r => r["City"].ToString() == "Paris");

            Assert.Equal(2.0, Convert.ToDouble(london["Food_Amount"]));       // 2 rows
            Assert.Equal(1.0, Convert.ToDouble(london["Transport_Amount"]));  // 1 row
            Assert.Equal(1.0, Convert.ToDouble(paris["Food_Amount"]));        // 1 row
            Assert.Equal(3.0, Convert.ToDouble(paris["Transport_Amount"]));   // 3 rows
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task MultiplePivotSources_AllProduceDataTables()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"pivottest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var csvContent = """
                Team,Sprint,Points,Hours
                Alpha,S1,13,40
                Alpha,S1,8,24
                Alpha,S2,21,56
                Beta,S1,5,16
                Beta,S2,13,40
                Beta,S2,8,24
                """;
            File.WriteAllText(Path.Combine(tempDir, "sprints.csv"), csvContent);

            var jsonSafePath = tempDir.Replace("\\", "\\\\");
            var json = $$"""
                {
                    "DatasourceDefinition": {
                        "DataSources": [
                            {
                                "Type": "CsvFile",
                                "DSTableName": "SprintData",
                                "CsvFilePath": "{{jsonSafePath}}",
                                "CsvFileName": "sprints.csv",
                                "CsvFileHeader": true
                            }
                        ]
                    },
                    "PivotDefinition": {
                        "PivotSources": [
                            {
                                "DSTableSource": "SprintData",
                                "DSTableTarget": "PointsPivot",
                                "Rows": ["Team"],
                                "Columns": ["Sprint"],
                                "Data": ["Points"],
                                "AggregateFunction": "Sum"
                            },
                            {
                                "DSTableSource": "SprintData",
                                "DSTableTarget": "HoursPivot",
                                "Rows": ["Team"],
                                "Columns": ["Sprint"],
                                "Data": ["Hours"],
                                "AggregateFunction": "Sum"
                            }
                        ]
                    },
                    {{GetExitDefinitionJson()}}
                }
                """;

            var task = CreateTask(json);
            await task.Run();

            var taskContext = task.GetTaskContext();
            var pointsPivot = taskContext.GetDataTable("PointsPivot");
            var hoursPivot = taskContext.GetDataTable("HoursPivot");

            Assert.NotNull(pointsPivot);
            Assert.NotNull(hoursPivot);

            // Points pivot
            var alphaPoints = pointsPivot.AsEnumerable().Single(r => r["Team"].ToString() == "Alpha");
            var betaPoints = pointsPivot.AsEnumerable().Single(r => r["Team"].ToString() == "Beta");
            Assert.Equal(21.0, Convert.ToDouble(alphaPoints["S1_Points"]));  // 13 + 8
            Assert.Equal(21.0, Convert.ToDouble(alphaPoints["S2_Points"]));  // 21
            Assert.Equal(5.0, Convert.ToDouble(betaPoints["S1_Points"]));    // 5
            Assert.Equal(21.0, Convert.ToDouble(betaPoints["S2_Points"]));   // 13 + 8

            // Hours pivot
            var alphaHours = hoursPivot.AsEnumerable().Single(r => r["Team"].ToString() == "Alpha");
            var betaHours = hoursPivot.AsEnumerable().Single(r => r["Team"].ToString() == "Beta");
            Assert.Equal(64.0, Convert.ToDouble(alphaHours["S1_Hours"]));    // 40 + 24
            Assert.Equal(56.0, Convert.ToDouble(alphaHours["S2_Hours"]));    // 56
            Assert.Equal(16.0, Convert.ToDouble(betaHours["S1_Hours"]));     // 16
            Assert.Equal(64.0, Convert.ToDouble(betaHours["S2_Hours"]));     // 40 + 24
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task PivotWithVariableReplacement_ResolvesTokensInPivotConfig()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"pivottest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var csvContent = """
                Country,Sector,MarketCap
                UK,Tech,500
                UK,Tech,300
                UK,Finance,700
                US,Tech,1000
                US,Finance,400
                """;
            File.WriteAllText(Path.Combine(tempDir, "markets.csv"), csvContent);

            var jsonSafePath = tempDir.Replace("\\", "\\\\");
            var json = $$"""
                {
                    "VariableDefinition": {
                        "Variables": {
                            "<@@SourceTable@@>": "MarketData",
                            "<@@TargetTable@@>": "MarketPivot",
                            "<@@AggFunc@@>": "Sum"
                        },
                        "IsActive": true
                    },
                    "DatasourceDefinition": {
                        "DataSources": [
                            {
                                "Type": "CsvFile",
                                "DSTableName": "<@@SourceTable@@>",
                                "CsvFilePath": "{{jsonSafePath}}",
                                "CsvFileName": "markets.csv",
                                "CsvFileHeader": true
                            }
                        ]
                    },
                    "PivotDefinition": {
                        "PivotSources": [
                            {
                                "DSTableSource": "<@@SourceTable@@>",
                                "DSTableTarget": "<@@TargetTable@@>",
                                "Rows": ["Country"],
                                "Columns": ["Sector"],
                                "Data": ["MarketCap"],
                                "AggregateFunction": "<@@AggFunc@@>"
                            }
                        ]
                    },
                    {{GetExitDefinitionJson()}}
                }
                """;

            var task = CreateTask(json);
            await task.Run();

            var pivotTable = task.GetTaskContext().GetDataTable("MarketPivot");

            Assert.NotNull(pivotTable);
            Assert.Equal(2, pivotTable.Rows.Count);

            var uk = pivotTable.AsEnumerable().Single(r => r["Country"].ToString() == "UK");
            var us = pivotTable.AsEnumerable().Single(r => r["Country"].ToString() == "US");

            Assert.Equal(700.0, Convert.ToDouble(uk["Finance_MarketCap"]));  // 700
            Assert.Equal(800.0, Convert.ToDouble(uk["Tech_MarketCap"]));     // 500 + 300
            Assert.Equal(400.0, Convert.ToDouble(us["Finance_MarketCap"]));  // 400
            Assert.Equal(1000.0, Convert.ToDouble(us["Tech_MarketCap"]));    // 1000
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task PivotSourceNotFound_ThrowsArgumentException()
    {
        var json = $$"""
            {
                "PivotDefinition": {
                    "PivotSources": [
                        {
                            "DSTableSource": "NonExistentTable",
                            "DSTableTarget": "PivotResult",
                            "Rows": ["Col1"],
                            "Columns": ["Col2"],
                            "Data": ["Col3"],
                            "AggregateFunction": "Sum"
                        }
                    ]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var task = CreateTask(json);
        await Assert.ThrowsAsync<ArgumentException>(() => task.Run());
    }
}
