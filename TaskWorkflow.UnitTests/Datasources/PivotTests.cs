using System.Data;
using TaskWorkflow.Common.Helpers;
using Xunit;

namespace TaskWorkflow.UnitTests.Datasources;

public class PivotTests
{
    [Fact]
    public void PivotDataTable_WeightedAverage_CalculatesCorrectly()
    {
        // ARRANGE
        DataTable sourceTable = new DataTable();
        sourceTable.Columns.Add("Year", typeof(int));
        sourceTable.Columns.Add("Product", typeof(string));
        sourceTable.Columns.Add("Price", typeof(double));
        sourceTable.Columns.Add("Quantity", typeof(double));

        // 2023 Laptop Data: ((500 * 1) + (800 * 4)) / 5 = 740
        sourceTable.Rows.Add(2023, "Laptop", 500.0, 1.0);
        sourceTable.Rows.Add(2023, "Laptop", 800.0, 4.0);
        sourceTable.Rows.Add(2024, "Laptop", 600.0, 1.0);

        var rows = new List<string> { "Year" };
        var cols = new List<string> { "Product" };
        var data = new List<string> { "Price" };

        // ACT
        DataTable result = CommonPivotHelper.PivotDataTable(
                rows, 
                cols, 
                data, 
                "PivotResult", 
                sourceTable, 
                "WeightedAverage", 
                "Quantity"
        );

        // ASSERT
        Assert.NotNull(result);
        Assert.Equal(2, result.Rows.Count);

        // Verify 2023 Laptop_Price calculation
        DataRow row2023 = result.AsEnumerable().Single(r => (int)r["Year"] == 2023);
        double actualValue = Convert.ToDouble(row2023["Laptop_Price"]);
        double expectedValue = 740.0;

        Assert.Equal(expectedValue, actualValue, precision: 3);
    }

    [Fact]
    public void PivotDataTable_Median_HandlesOutliersCorrectly()
    {
        // ARRANGE
        DataTable sourceTable = new DataTable();
        sourceTable.Columns.Add("Store", typeof(string));
        sourceTable.Columns.Add("Dept", typeof(string));
        sourceTable.Columns.Add("Sales", typeof(double));

        // Median of 10, 20, 1000 should be 20
        sourceTable.Rows.Add("Store1", "Electronics", 10.0);
        sourceTable.Rows.Add("Store1", "Electronics", 20.0);
        sourceTable.Rows.Add("Store1", "Electronics", 1000.0);

        // ACT
        DataTable result = CommonPivotHelper.PivotDataTable(
                new List<string> { "Store" },
                new List<string> { "Dept" },
                new List<string> { "Sales" },
                "MedianTest",
                sourceTable,
                "Median"
        );

        // ASSERT
        DataRow resultRow = result.Rows[0];
        double medianValue = Convert.ToDouble(resultRow["Electronics_Sales"]);
        Assert.Equal(20.0, medianValue);
    }

    [Fact]
    public void PivotDataTable_NullHandling_ReturnsNullForMissingCombos()
    {
        // ARRANGE
        DataTable sourceTable = new DataTable();
        sourceTable.Columns.Add("User", typeof(string));
        sourceTable.Columns.Add("App", typeof(string));
        sourceTable.Columns.Add("Score", typeof(double));

        sourceTable.Rows.Add("Alice", "AppA", 10.0);
        sourceTable.Rows.Add("Alice", "AppA", 35.0);
        sourceTable.Rows.Add("Alice", "AppB", 20.0);
        sourceTable.Rows.Add("Alice", "AppC", 30.0);

        sourceTable.Rows.Add("Brian", "AppA", 10.0);
        sourceTable.Rows.Add("Brian", "AppB", 55.0);
        sourceTable.Rows.Add("Brian", "AppC", 10.0);
        sourceTable.Rows.Add("Brian", "AppC", 10.0);
        
        sourceTable.Rows.Add("David", "AppA", 14.0);
        sourceTable.Rows.Add("David", "AppB", 12.0);
        sourceTable.Rows.Add("David", "AppB", 27.0);
        sourceTable.Rows.Add("David", "AppC", 40.0);
        
        sourceTable.Rows.Add("Andrew", "AppA", 18.0);
        
        // ACT
        DataTable result = CommonPivotHelper.PivotDataTable(
                new List<string> { "User" },
                new List<string> { "App" },
                new List<string> { "Score" },
                "NullTest",
                sourceTable,
                "Sum"
        );

        // ASSERT
        // AppB_Score column should exist but be DBNull for Alice
        Assert.True(result.Columns.Count == 4);
        Assert.True(result.Columns.Contains("User"));
        Assert.True(result.Columns.Contains("AppA_Score"));
        Assert.True(result.Columns.Contains("AppB_Score"));
        Assert.True(result.Columns.Contains("AppC_Score"));
        Assert.Equal(DBNull.Value, result.Rows[3]["AppB_Score"]);
    }
}
