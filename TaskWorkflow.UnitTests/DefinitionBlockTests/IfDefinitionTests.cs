using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

namespace TaskWorkflow.UnitTests.DefinitionBlockTests;

public class IfDefinitionTests
{
    // CSV representing a daily report status file loaded by the DatasourceDefinition block
    private static readonly string _csvContent = """
        ReportName,Status,RecordCount
        DailySales,COMPLETE,1250
        WeeklyInventory,COMPLETE,847
        MonthlyForecast,PENDING,0
        """;

    /// <summary>
    /// Builds a complete task JSON with:
    ///   - VariableDefinition declaring ExpectedStatus and PassMark
    ///   - DatasourceDefinition loading a CSV into the ReportStatus DataTable
    ///   - IfDefinition comparing the two variables
    ///   - ExitDefinition (SendEmail disabled to prevent SMTP calls during tests)
    /// </summary>
    private static string BuildTaskJson(string tempDir, string expectedStatus, string passMark)
    {
        var jsonSafePath = tempDir.Replace("\\", "\\\\");
        return $$"""
            {
                "VariableDefinition": {
                    "IsActive": true,
                    "Variables": {
                        "<@@ExpectedStatus@@>": "{{expectedStatus}}",
                        "<@@PassMark@@>": "{{passMark}}"
                    }
                },
                "DatasourceDefinition": {
                    "DataSources": [
                        {
                            "Type": "CsvFile",
                            "DSTableName": "ReportStatus",
                            "CsvFilePath": "{{jsonSafePath}}",
                            "CsvFileName": "reportstatus.csv",
                            "CsvFileHeader": true
                        }
                    ]
                },
                "IfDefinition": {
                    "IsActive": true,
                    "Conditions": [
                        {
                            "ConditionName": "StatusCheck",
                            "LeftOperand": "<@@ExpectedStatus@@>",
                            "RightOperand": "<@@PassMark@@>",
                            "Operator": "==",
                            "OnTrueAction": "Proceed",
                            "OnFalseAction": "AbortTask"
                        }
                    ]
                },
                "ExitDefinition": {
                    "IsActive": true,
                    "Success": { "SendEmail": false, "To": [], "CC": [], "BCC": [], "Subject": "", "Body": "", "Priority": "Normal", "Attachments": [] },
                    "Failure": { "SendEmail": false, "To": [], "CC": [], "BCC": [], "Subject": "", "Body": "", "Priority": "Normal", "Attachments": [] }
                }
            }
            """;
    }

    /// <summary>
    /// ExpectedStatus matches PassMark: condition evaluates true, OnTrueAction = Proceed.
    /// Task runs to completion. DatasourceDefinition CSV is loaded into ReportStatus DataTable
    /// before the IfDefinition evaluates.
    /// </summary>
    [Fact]
    public async Task IfDefinition_ConditionTrue_OnTrueAction_Proceed_TaskCompletes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"iftest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "reportstatus.csv"), _csvContent);

            // "COMPLETE" == "COMPLETE" → true → Proceed
            var json = BuildTaskJson(tempDir, expectedStatus: "COMPLETE", passMark: "COMPLETE");
            var task = CreateTask(json);

            await task.Run();

            // Confirm the CSV was loaded into a DataTable by DatasourceDefinition
            var table = task.GetTaskContext().GetDataTable("ReportStatus");
            Assert.NotNull(table);
            Assert.Equal(3, table.Columns.Count);
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal("DailySales",      table.Rows[0]["ReportName"].ToString());
            Assert.Equal("COMPLETE",        table.Rows[0]["Status"].ToString());
            Assert.Equal("1250",            table.Rows[0]["RecordCount"].ToString());
            Assert.Equal("MonthlyForecast", table.Rows[2]["ReportName"].ToString());
            Assert.Equal("PENDING",         table.Rows[2]["Status"].ToString());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// ExpectedStatus does NOT match PassMark: condition evaluates false, OnFalseAction = AbortTask.
    /// An OperationCanceledException is thrown, halting the task flow.
    /// The DatasourceDefinition CSV was already loaded before the IfDefinition aborted,
    /// so the DataTable is still populated in TaskContext.
    /// </summary>
    [Fact]
    public async Task IfDefinition_ConditionFalse_OnFalseAction_AbortTask_ThrowsAndHaltsFlow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"iftest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "reportstatus.csv"), _csvContent);

            // "FAILED" == "COMPLETE" → false → AbortTask
            var json = BuildTaskJson(tempDir, expectedStatus: "FAILED", passMark: "COMPLETE");
            var task = CreateTask(json);

            var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => task.Run());
            Assert.Contains("StatusCheck", ex.Message);
            Assert.Contains("aborted", ex.Message);

            // DatasourceDefinition ran before IfDefinition aborted, so DataTable is still populated
            var table = task.GetTaskContext().GetDataTable("ReportStatus");
            Assert.NotNull(table);
            Assert.Equal(3, table.Rows.Count);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // -------------------------------------------------------------------------
    // Runtime function tests: <fn_runtime_functionname("param")>
    // -------------------------------------------------------------------------

    private static string NoEmailExitDefinition => """
        "ExitDefinition": {
            "IsActive": true,
            "Success": { "SendEmail": false, "To": [], "CC": [], "BCC": [], "Subject": "", "Body": "", "Priority": "Normal", "Attachments": [] },
            "Failure": { "SendEmail": false, "To": [], "CC": [], "BCC": [], "Subject": "", "Body": "", "Priority": "Normal", "Attachments": [] }
        }
        """;

    private static string BuildCsvDatasourceJson(string jsonSafePath) => $$"""
        "DatasourceDefinition": {
            "DataSources": [
                {
                    "Type": "CsvFile",
                    "DSTableName": "ReportStatus",
                    "CsvFilePath": "{{jsonSafePath}}",
                    "CsvFileName": "reportstatus.csv",
                    "CsvFileHeader": true
                }
            ]
        }
        """;

    /// <summary>
    /// _get_datatable_count returns the row count of a DataTable as a string.
    /// CSV has 3 data rows → count == "3" → condition true → Proceed.
    /// </summary>
    [Fact]
    public async Task RuntimeFunction_GetDataTableCount_MatchesRowCount_Proceeds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"iftest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "reportstatus.csv"), _csvContent);

            var jsonSafePath = tempDir.Replace("\\", "\\\\");
            // \" inside the raw string literal becomes the two chars \ and " which JSON
            // decodes to a literal " — giving the operand: <fn_runtime_get_datatable_count("ReportStatus")>
            var json = $$"""
                {
                    {{BuildCsvDatasourceJson(jsonSafePath)}},
                    "IfDefinition": {
                        "IsActive": true,
                        "Conditions": [
                            {
                                "ConditionName": "RowCountCheck",
                                "LeftOperand":  "<fn_runtime_get_datatable_count(\"ReportStatus\")>",
                                "RightOperand": "3",
                                "Operator": "==",
                                "OnTrueAction":  "Proceed",
                                "OnFalseAction": "AbortTask"
                            }
                        ]
                    },
                    {{NoEmailExitDefinition}}
                }
                """;

            var task = CreateTask(json);
            await task.Run();  // row count 3 == "3" → Proceed → no exception

            var table = task.GetTaskContext().GetDataTable("ReportStatus");
            Assert.NotNull(table);
            Assert.Equal(3, table.Rows.Count);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// _get_datatable_count: CSV has 3 rows but condition expects 0 → false → AbortTask.
    /// </summary>
    [Fact]
    public async Task RuntimeFunction_GetDataTableCount_MismatchedRowCount_Aborts()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"iftest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "reportstatus.csv"), _csvContent);

            var jsonSafePath = tempDir.Replace("\\", "\\\\");
            var json = $$"""
                {
                    {{BuildCsvDatasourceJson(jsonSafePath)}},
                    "IfDefinition": {
                        "IsActive": true,
                        "Conditions": [
                            {
                                "ConditionName": "EmptyTableCheck",
                                "LeftOperand":  "<fn_runtime_get_datatable_count(\"ReportStatus\")>",
                                "RightOperand": "0",
                                "Operator": "==",
                                "OnTrueAction":  "Proceed",
                                "OnFalseAction": "AbortTask"
                            }
                        ]
                    },
                    {{NoEmailExitDefinition}}
                }
                """;

            var task = CreateTask(json);
            var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => task.Run());
            Assert.Contains("EmptyTableCheck", ex.Message);
            Assert.Contains("aborted", ex.Message);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// _get_datatable_hash returns a SHA-256 hex string over all cell values.
    /// A table with data produces a non-empty hash → hash != "" → true → Proceed.
    /// </summary>
    [Fact]
    public async Task RuntimeFunction_GetDataTableHash_NonEmptyHash_Proceeds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"iftest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "reportstatus.csv"), _csvContent);

            var jsonSafePath = tempDir.Replace("\\", "\\\\");
            var json = $$"""
                {
                    {{BuildCsvDatasourceJson(jsonSafePath)}},
                    "IfDefinition": {
                        "IsActive": true,
                        "Conditions": [
                            {
                                "ConditionName": "HashNotEmptyCheck",
                                "LeftOperand":  "<fn_runtime_get_datatable_hash(\"ReportStatus\")>",
                                "RightOperand": "",
                                "Operator": "!=",
                                "OnTrueAction":  "Proceed",
                                "OnFalseAction": "AbortTask"
                            }
                        ]
                    },
                    {{NoEmailExitDefinition}}
                }
                """;

            var task = CreateTask(json);
            await task.Run();  // hash of real data != "" → Proceed → no exception

            var table = task.GetTaskContext().GetDataTable("ReportStatus");
            Assert.NotNull(table);
            Assert.Equal(3, table.Rows.Count);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// _get_datatable_hash: a SHA-256 hash will never equal a trivial placeholder value.
    /// hash == "0000" → false → AbortTask, confirming the hash is being computed and tested.
    /// </summary>
    [Fact]
    public async Task RuntimeFunction_GetDataTableHash_ImpossibleMatch_Aborts()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"iftest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "reportstatus.csv"), _csvContent);

            var jsonSafePath = tempDir.Replace("\\", "\\\\");
            var json = $$"""
                {
                    {{BuildCsvDatasourceJson(jsonSafePath)}},
                    "IfDefinition": {
                        "IsActive": true,
                        "Conditions": [
                            {
                                "ConditionName": "HashMatchCheck",
                                "LeftOperand":  "<fn_runtime_get_datatable_hash(\"ReportStatus\")>",
                                "RightOperand": "0000",
                                "Operator": "==",
                                "OnTrueAction":  "Proceed",
                                "OnFalseAction": "AbortTask"
                            }
                        ]
                    },
                    {{NoEmailExitDefinition}}
                }
                """;

            var task = CreateTask(json);
            var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => task.Run());
            Assert.Contains("HashMatchCheck", ex.Message);
            Assert.Contains("aborted", ex.Message);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// _get_datatable_column_total sums a numeric column and returns it as a string.
    /// RecordCount column: 1250 + 847 + 0 = 2097 → "2097" == "2097" → true → Proceed.
    /// </summary>
    [Fact]
    public async Task RuntimeFunction_GetDataTableColumnTotal_NumericColumn_Proceeds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"iftest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "reportstatus.csv"), _csvContent);

            var jsonSafePath = tempDir.Replace("\\", "\\\\");
            var json = $$"""
                {
                    {{BuildCsvDatasourceJson(jsonSafePath)}},
                    "IfDefinition": {
                        "IsActive": true,
                        "Conditions": [
                            {
                                "ConditionName": "ColumnTotalCheck",
                                "LeftOperand":  "<fn_runtime_get_datatable_column_total(\"ReportStatus\", \"RecordCount\")>",
                                "RightOperand": "2097",
                                "Operator": "==",
                                "OnTrueAction":  "Proceed",
                                "OnFalseAction": "AbortTask"
                            }
                        ]
                    },
                    {{NoEmailExitDefinition}}
                }
                """;

            var task = CreateTask(json);
            await task.Run();  // 1250 + 847 + 0 = 2097 == "2097" → Proceed
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// _get_datatable_column_total throws InvalidOperationException when the column
    /// contains non-numeric values (ReportName contains strings).
    /// </summary>
    [Fact]
    public async Task RuntimeFunction_GetDataTableColumnTotal_NonNumericColumn_Throws()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"iftest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "reportstatus.csv"), _csvContent);

            var jsonSafePath = tempDir.Replace("\\", "\\\\");
            var json = $$"""
                {
                    {{BuildCsvDatasourceJson(jsonSafePath)}},
                    "IfDefinition": {
                        "IsActive": true,
                        "Conditions": [
                            {
                                "ConditionName": "NonNumericColumnCheck",
                                "LeftOperand":  "<fn_runtime_get_datatable_column_total(\"ReportStatus\", \"ReportName\")>",
                                "RightOperand": "0",
                                "Operator": "==",
                                "OnTrueAction":  "Proceed",
                                "OnFalseAction": "AbortTask"
                            }
                        ]
                    },
                    {{NoEmailExitDefinition}}
                }
                """;

            var task = CreateTask(json);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => task.Run());
            Assert.Contains("ReportName", ex.Message);
            Assert.Contains("non-numeric", ex.Message);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
