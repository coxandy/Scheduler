using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.TaskFactory.Tasks;
using TaskWorkflow.Common.Models;
using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

namespace TaskWorkflow.UnitTests.DefinitionBlockTests;

public class VariableDefinitionTests
{

    [Fact]
    public void VariableDefinition_DeserializesIntoDictionary()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": {
                        "<@@Test1@@>": 13,
                        "<@@Test2@@>": 15,
                        "<@@Test3@@>": "andy",
                        "<@@Test4@@>": "58"
                    },
                    "IsActive": true
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var variable = result[0] as VariableDefinition;

        Assert.NotNull(variable);
        Assert.Equal(4, variable.Variables.Count);
        Assert.Equal("<@@Test1@@>", variable.Variables.Keys.ElementAt(0));
        Assert.Equal("<@@Test2@@>", variable.Variables.Keys.ElementAt(1));
        Assert.Equal("<@@Test3@@>", variable.Variables.Keys.ElementAt(2));
        Assert.Equal("<@@Test4@@>", variable.Variables.Keys.ElementAt(3));

        Assert.Equal("13", variable.Variables.Values.ElementAt(0).ToString());
        Assert.Equal("15", variable.Variables.Values.ElementAt(1).ToString());
        Assert.Equal("andy", variable.Variables.Values.ElementAt(2).ToString());
        Assert.Equal("58", variable.Variables.Values.ElementAt(3).ToString());

        Assert.True(variable.IsActive);
    }

    [Fact]
    public void VariableDefinition_MustBeFirstBlock()
    {
        var json = $$"""
            {
                "ClassDefinition": {
                    "classname": "MyClass",
                    "methodname": "Run",
                    "parameters": []
                },
                "VariableDefinition": {
                    "Variables": { "<@@V1@@>": "val" },
                    "isActive": true
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json, instance.EffectiveDate, instance.EnvironmentName);
        var ex = Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
        Assert.Contains("VariableDefinition should always be the first definition block", ex.Message);
    }

    [Fact]
    public void VariableDefinition_MustNotHaveNumericSuffix()
    {
        var json = $$"""
            {
                "VariableDefinition1": {
                    "Variables": { "<@@V1@@>": "val" },
                    "isActive": true
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json, instance.EffectiveDate, instance.EnvironmentName);
        var ex = Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
        Assert.Contains("must not have a numeric suffix", ex.Message);
    }

    [Fact]
    public void VariableDefinition_IsOptional()
    {
        var json = $$"""
            {
                "ClassDefinition": {
                    "classname": "MyClass",
                    "methodname": "Run",
                    "parameters": []
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.Equal(2, result.Count);
        Assert.IsType<ClassDefinition>(result[0]);
        Assert.IsType<ExitDefinition>(result[1]);
    }

    [Fact]
    public void VariableDefinition_ValidVariableNames_ParsesSuccessfully()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": {
                        "<@@ProductId@@>": 42,
                        "<@@UserName@@>": "admin",
                        "<@@A@@>": "short"
                    },
                    "IsActive": true
                },
                "ClassDefinition": {
                    "classname": "MyClass",
                    "methodname": "Run",
                    "parameters": ["<@@ProductId@@>"]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);
        var variable = result[0] as VariableDefinition;

        Assert.NotNull(variable);
        Assert.Equal(3, variable.Variables.Count);
    }

    [Fact]
    public void VariableDefinition_InvalidVariableName_ThrowsFormatException()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": {
                        "<@@ValidVar@@>": "ok",
                        "InvalidVar": "bad"
                    },
                    "IsActive": true
                },
                "ClassDefinition": {
                    "classname": "MyClass",
                    "methodname": "Run",
                    "parameters": []
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json, instance.EffectiveDate, instance.EnvironmentName);
        VariableDefinition variableBlock = JsonParser.VerifyJson();
        Assert.NotNull(variableBlock);

        var ex = Assert.Throws<FormatException>(() => JsonParser.ApplyVariableReplacementsToJson(json, variableBlock));
        Assert.Contains("<@@", ex.Message);
    }

    [Fact]
    public void VariableDefinition_ReplacesTokensInOtherBlocks()
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

        var result = ParseAndDeserialize(json);

        var classDef = result[1] as ClassDefinition;
        Assert.NotNull(classDef);
        Assert.Equal("Web3.Api.GetBalances", classDef.ClassName);
        Assert.Equal("GetBalances", classDef.MethodName);
        Assert.Single(classDef.Parameters);
        Assert.Equal("epoch42", classDef.Parameters[0]);
    }

    [Fact]
    public void VariableDefinition_IsAlwaysFirstInDeserializedList()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "Variables": { "<@@V1@@>": "val" },
                    "isActive": true
                },
                "ClassDefinition": {
                    "classname": "MyClass",
                    "methodname": "Run",
                    "parameters": []
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.IsType<VariableDefinition>(result[0]);
    }
}
