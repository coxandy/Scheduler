using Moq;
using TaskWorkflow.Common.Models;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Tasks;
using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

namespace TaskWorkflow.UnitTests.JsonParsing;

public class JsonParsingIntegrationTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider = new();

    // TaskInstance EffectiveDate = 2026-10-05 (from TestHelpers)

    /// <summary>
    /// Creates a GenericWorkflowTask which runs the full BaseTask constructor pipeline:
    ///   1. Date token replacement
    ///   2. Function variable resolution and replacement
    ///   3. JSON validation
    ///   4. VariableDefinition extraction and TaskContext population
    ///   5. Variable token replacement in all other blocks
    ///   6. Final deserialization
    /// </summary>
    private GenericWorkflowTask CreateTask(string json)
    {
        var instance = GetTaskInstance();
        return new GenericWorkflowTask(json, instance, _mockServiceProvider.Object);
    }

    [Fact]
    public void HardcodedVariables_AreReplacedInClassDefinition()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": {
                        "<@@ClassName@@>": "Web3.Api.GetBalances",
                        "<@@MethodName@@>": "GetBalances",
                        "<@@Param1@@>": "epoch42"
                    },
                    "IsActive": true
                },
                "ClassDefinition": {
                    "classname": "<@@ClassName@@>",
                    "methodname": "<@@MethodName@@>",
                    "parameters": ["<@@Param1@@>"]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var task = CreateTask(json);
        var definitions = task.GetDefinitionBlocks();

        var classDef = Assert.IsType<ClassDefinition>(definitions[1]);
        Assert.Equal("Web3.Api.GetBalances", classDef.ClassName);
        Assert.Equal("GetBalances", classDef.MethodName);
        Assert.Single(classDef.Parameters);
        Assert.Equal("epoch42", classDef.Parameters[0]);
    }

    [Fact]
    public void DateTokenVariables_AreResolvedBeforeVariableReplacement()
    {
        // Variable values contain date tokens - these are resolved in Phase 1,
        // then variable replacement in Phase 5 applies the resolved values.
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": {
                        "<@@ReportDate@@>": "<yyyy-MM-dd>",
                        "<@@DisplayDate@@>": "<dd MMM yyyy>",
                        "<@@FilePath@@>": "/data/<yyyy-MM-dd>/report.csv"
                    },
                    "IsActive": true
                },
                "ClassDefinition": {
                    "classname": "Reports.DailyReport",
                    "methodname": "Generate",
                    "parameters": ["<@@ReportDate@@>", "<@@DisplayDate@@>", "<@@FilePath@@>"]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var task = CreateTask(json);
        var definitions = task.GetDefinitionBlocks();

        var classDef = Assert.IsType<ClassDefinition>(definitions[1]);
        Assert.Equal("2026-10-05", classDef.Parameters[0]);
        Assert.Equal("05 Oct 2026", classDef.Parameters[1]);
        Assert.Equal("/data/2026-10-05/report.csv", classDef.Parameters[2]);
    }

    [Fact]
    public void FunctionVariables_AreResolvedBeforeVariableReplacement()
    {
        // Create temp files so fn_GetLatestFile and fn_GetOldestFile can resolve
        var tempDir = Path.Combine(Path.GetTempPath(), $"jsonparse_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var oldFile = Path.Combine(tempDir, "report_001.csv");
            var newFile = Path.Combine(tempDir, "report_002.csv");
            File.WriteAllText(oldFile, "old");
            Thread.Sleep(100);
            File.WriteAllText(newFile, "new");

            var wildcardPath = Path.Combine(tempDir, "report_0*.csv");
            var jsonSafePath = wildcardPath.Replace("\\", "\\\\");

            var json = $$"""
                {
                    "VariableDefinition": {
                        "Variables": {
                            "<@@LatestFile@@>": "<fn_GetLatestFile({{jsonSafePath}})>",
                            "<@@OldestFile@@>": "<fn_GetOldestFile({{jsonSafePath}})>"
                        },
                        "IsActive": true
                    },
                    "ClassDefinition": {
                        "classname": "FileProcessor.Archive",
                        "methodname": "Process",
                        "parameters": ["<@@LatestFile@@>", "<@@OldestFile@@>"]
                    },
                    {{GetExitDefinitionJson()}}
                }
                """;

            var task = CreateTask(json);
            var definitions = task.GetDefinitionBlocks();

            var classDef = Assert.IsType<ClassDefinition>(definitions[1]);
            Assert.Equal(newFile, classDef.Parameters[0]);
            Assert.Equal(oldFile, classDef.Parameters[1]);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void AllReplacementTypes_CombinedInSingleJson()
    {
        // Tests all three replacement types together:
        //   - Date tokens in variable values
        //   - Function tokens in variable values
        //   - Hard-coded variable values
        // All are then replaced in other definition blocks.

        var tempDir = Path.Combine(Path.GetTempPath(), $"jsonparse_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var oldFile = Path.Combine(tempDir, "data_001.xlsx");
            var newFile = Path.Combine(tempDir, "data_002.xlsx");
            File.WriteAllText(oldFile, "old");
            Thread.Sleep(100);
            File.WriteAllText(newFile, "new");

            var wildcardPath = Path.Combine(tempDir, "data_0*.xlsx");
            var jsonSafePath = wildcardPath.Replace("\\", "\\\\");

            var json = $$"""
                {
                    "VariableDefinition": {
                        "Variables": {
                            "<@@ClassName@@>": "ETL.Pipeline.RunDaily",
                            "<@@MethodName@@>": "Execute",
                            "<@@ReportDate@@>": "<yyyy-MM-dd>",
                            "<@@DisplayDate@@>": "<dd MMM yyyy>",
                            "<@@MonthDay@@>": "<MMdd>",
                            "<@@SourceFile@@>": "<fn_GetLatestFile({{jsonSafePath}})>",
                            "<@@ArchiveFile@@>": "<fn_GetOldestFile({{jsonSafePath}})>",
                            "<@@OutputDir@@>": "/output/<yyyy-MM-dd>"
                        },
                        "IsActive": true
                    },
                    "ClassDefinition1": {
                        "classname": "<@@ClassName@@>",
                        "methodname": "<@@MethodName@@>",
                        "parameters": ["<@@ReportDate@@>", "<@@SourceFile@@>", "<@@OutputDir@@>"]
                    },
                    "ClassDefinition2": {
                        "classname": "ETL.Pipeline.Archive",
                        "methodname": "MoveToArchive",
                        "parameters": ["<@@ArchiveFile@@>", "<@@DisplayDate@@>", "<@@MonthDay@@>"]
                    },
                    {{GetExitDefinitionJson()}}
                }
                """;

            var task = CreateTask(json);
            var definitions = task.GetDefinitionBlocks();
            var processedJson = task.GetJson();

            // Verify block count and types
            Assert.Equal(4, definitions.Count);
            Assert.IsType<VariableDefinition>(definitions[0]);
            Assert.IsType<ClassDefinition>(definitions[1]);
            Assert.IsType<ClassDefinition>(definitions[2]);
            Assert.IsType<ExitDefinition>(definitions[3]);

            // ClassDefinition1: hard-coded + date token + function token variables
            var class1 = Assert.IsType<ClassDefinition>(definitions[1]);
            Assert.Equal("ETL.Pipeline.RunDaily", class1.ClassName);
            Assert.Equal("Execute", class1.MethodName);
            Assert.Equal(3, class1.Parameters.Count);
            Assert.Equal("2026-10-05", class1.Parameters[0]);        // date token variable
            Assert.Equal(newFile, class1.Parameters[1]);              // fn_GetLatestFile variable
            Assert.Equal("/output/2026-10-05", class1.Parameters[2]); // date token in value

            // ClassDefinition2: function token + date tokens
            var class2 = Assert.IsType<ClassDefinition>(definitions[2]);
            Assert.Equal("ETL.Pipeline.Archive", class2.ClassName);
            Assert.Equal("MoveToArchive", class2.MethodName);
            Assert.Equal(3, class2.Parameters.Count);
            Assert.Equal(oldFile, class2.Parameters[0]);              // fn_GetOldestFile variable
            Assert.Equal("05 Oct 2026", class2.Parameters[1]);       // date token variable
            Assert.Equal("1005", class2.Parameters[2]);               // date token variable

            // Verify no unresolved tokens remain in processed JSON
            Assert.DoesNotContain("<@@", processedJson);
            Assert.DoesNotContain("@@>", processedJson);
            Assert.DoesNotContain("<fn_", processedJson);
            Assert.DoesNotContain("<yyyy", processedJson);
            Assert.DoesNotContain("<dd ", processedJson);
            Assert.DoesNotContain("<MMdd>", processedJson);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void DateTokens_ReplacedDirectlyInNonVariableBlocks()
    {
        // Date tokens used directly in definition block values (not via variables)
        var json = $$"""
            {
                "ClassDefinition": {
                    "classname": "Reports.Daily",
                    "methodname": "Run_<yyyy-MM-dd>",
                    "parameters": ["<dd MMM yyyy>", "<MMdd>"]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var task = CreateTask(json);
        var definitions = task.GetDefinitionBlocks();

        var classDef = Assert.IsType<ClassDefinition>(definitions[0]);
        Assert.Equal("Run_2026-10-05", classDef.MethodName);
        Assert.Equal("05 Oct 2026", classDef.Parameters[0]);
        Assert.Equal("1005", classDef.Parameters[1]);
    }

    [Fact]
    public void MultipleClassDefinitions_AllVariablesResolved()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": {
                        "<@@Server@@>": "prod-db-01",
                        "<@@Database@@>": "FinanceDB",
                        "<@@RunDate@@>": "<yyyy-MM-dd>",
                        "<@@Recipient@@>": "team@company.com"
                    },
                    "IsActive": true
                },
                "ClassDefinition1": {
                    "classname": "Data.Extract",
                    "methodname": "RunQuery",
                    "parameters": ["<@@Server@@>", "<@@Database@@>", "<@@RunDate@@>"]
                },
                "ClassDefinition2": {
                    "classname": "Data.Transform",
                    "methodname": "Process",
                    "parameters": ["<@@RunDate@@>"]
                },
                "ClassDefinition3": {
                    "classname": "Notification.Email",
                    "methodname": "Send",
                    "parameters": ["<@@Recipient@@>", "<@@RunDate@@>"]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var task = CreateTask(json);
        var definitions = task.GetDefinitionBlocks();
        var processedJson = task.GetJson();
        var taskContext = task.GetTaskContext();

        Assert.Equal(5, definitions.Count);

        var extract = Assert.IsType<ClassDefinition>(definitions[1]);
        Assert.Equal("prod-db-01", extract.Parameters[0]);
        Assert.Equal("FinanceDB", extract.Parameters[1]);
        Assert.Equal("2026-10-05", extract.Parameters[2]);

        var transform = Assert.IsType<ClassDefinition>(definitions[2]);
        Assert.Equal("2026-10-05", transform.Parameters[0]);

        var email = Assert.IsType<ClassDefinition>(definitions[3]);
        Assert.Equal("team@company.com", email.Parameters[0]);
        Assert.Equal("2026-10-05", email.Parameters[1]);

        Assert.DoesNotContain("<@@", processedJson);

        // Verify TaskContext variables match VariableDefinition key/values
        var allVars = taskContext.GetAllVariables();
        Assert.Equal(4, allVars.Count);
        Assert.Equal("prod-db-01", allVars["<@@Server@@>"].ToString());
        Assert.Equal("FinanceDB", allVars["<@@Database@@>"].ToString());
        Assert.Equal("2026-10-05", allVars["<@@RunDate@@>"].ToString());
        Assert.Equal("team@company.com", allVars["<@@Recipient@@>"].ToString());
    }

    [Fact]
    public void AllDateTokenFormats_ResolvedCorrectly()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": {
                        "<@@Fmt1@@>": "<yyyy-MM-dd>",
                        "<@@Fmt2@@>": "<yyyy-MMM-dd>",
                        "<@@Fmt3@@>": "<yyyy MM dd>",
                        "<@@Fmt4@@>": "<yyyy MMM dd>",
                        "<@@Fmt5@@>": "<dd MMM yyyy>",
                        "<@@Fmt6@@>": "<MMdd>"
                    },
                    "IsActive": true
                },
                "ClassDefinition": {
                    "classname": "DateTest.Runner",
                    "methodname": "Verify",
                    "parameters": ["<@@Fmt1@@>", "<@@Fmt2@@>", "<@@Fmt3@@>", "<@@Fmt4@@>", "<@@Fmt5@@>", "<@@Fmt6@@>"]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var task = CreateTask(json);
        var definitions = task.GetDefinitionBlocks();

        var classDef = Assert.IsType<ClassDefinition>(definitions[1]);
        Assert.Equal("2026-10-05", classDef.Parameters[0]);
        Assert.Equal("2026-Oct-05", classDef.Parameters[1]);
        Assert.Equal("2026 10 05", classDef.Parameters[2]);
        Assert.Equal("2026 Oct 05", classDef.Parameters[3]);
        Assert.Equal("05 Oct 2026", classDef.Parameters[4]);
        Assert.Equal("1005", classDef.Parameters[5]);
    }

    [Fact]
    public void NoVariableDefinition_DirectTokensStillResolved()
    {
        // JSON without a VariableDefinition block - date tokens in values still get replaced
        var json = $$"""
            {
                "ClassDefinition": {
                    "classname": "Reports.Generator",
                    "methodname": "Run",
                    "parameters": ["<yyyy-MM-dd>"]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var task = CreateTask(json);
        var definitions = task.GetDefinitionBlocks();

        Assert.Equal(2, definitions.Count);
        var classDef = Assert.IsType<ClassDefinition>(definitions[0]);
        Assert.Equal("2026-10-05", classDef.Parameters[0]);
    }

    [Fact]
    public void VariableUsedMultipleTimes_AllOccurrencesReplaced()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": {
                        "<@@FilePath@@>": "/data/<yyyy-MM-dd>/input.csv"
                    },
                    "IsActive": true
                },
                "ClassDefinition1": {
                    "classname": "IO.Reader",
                    "methodname": "Read",
                    "parameters": ["<@@FilePath@@>"]
                },
                "ClassDefinition2": {
                    "classname": "IO.Validator",
                    "methodname": "Validate",
                    "parameters": ["<@@FilePath@@>"]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var task = CreateTask(json);
        var definitions = task.GetDefinitionBlocks();
        var processedJson = task.GetJson();

        var reader = Assert.IsType<ClassDefinition>(definitions[1]);
        var validator = Assert.IsType<ClassDefinition>(definitions[2]);

        Assert.Equal("/data/2026-10-05/input.csv", reader.Parameters[0]);
        Assert.Equal("/data/2026-10-05/input.csv", validator.Parameters[0]);
        Assert.DoesNotContain("<@@FilePath@@>", processedJson);
    }
}
