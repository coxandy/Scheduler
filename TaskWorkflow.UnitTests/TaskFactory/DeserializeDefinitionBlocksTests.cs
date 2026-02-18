using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.TaskFactory.Tasks;
using TaskWorkflow.Common.Models;
using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

namespace TaskWorkflow.UnitTests.TaskFactory;

public class DeserializeDefinitionBlocksTests
{
    private static string GetValidJson() => $$"""
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
                "ClassDefinition": {
                    "status": "Failed",
                    "classname": "Web3.Api.GetBalancesByEpoch",
                    "methodname": "GetBalancesByEpoch",
                    "parameters": [
                    "arrayval1",
                    "arrayval2"
                    ]
                },
                {{GetExitDefinitionJson()}}
                }
        """;

    // DeserializeDefinitionBlocks tests

    [Fact]
    public void ValidJson_ReturnsCorrectTypes()
    {
        var result = ParseAndDeserialize(GetValidJson());
        Assert.IsType<VariableDefinition>(result[0]);
        Assert.IsType<ClassDefinition>(result[1]);
        Assert.IsType<ExitDefinition>(result[2]);
    }

    [Fact]
    public void ValidJson_ClassDefinition_DeserializesCorrectly()
    {
        var result = ParseAndDeserialize(GetValidJson());
        var classDef = result[1] as ClassDefinition;

        Assert.NotNull(classDef);
        Assert.Equal("Web3.Api.GetBalancesByEpoch", classDef.ClassName);
        Assert.Equal("GetBalancesByEpoch", classDef.MethodName);
        Assert.Equal(2, classDef.Parameters.Count);
    }

    [Fact]
    public void SingleDefinitionBlock_ReturnsSingleItem()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "id": 1,
                    "role": "Admin",
                    "permissions": [],
                    "isActive": true
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.Equal(2, result.Count);
        Assert.IsType<VariableDefinition>(result[0]);
        Assert.IsType<ExitDefinition>(result[1]);
    }

    [Fact]
    public void MultipleBlocksWithNumericSuffix_ReturnsCorrectCount()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "id": 1,
                    "role": "Admin",
                    "permissions": ["read"],
                    "isActive": true
                },
                "ClassDefinition1": {
                    "classname": "Class.First",
                    "methodname": "Run",
                    "parameters": []
                },
                "ClassDefinition2": {
                    "classname": "Class.Second",
                    "methodname": "Execute",
                    "parameters": []
                },
                "ClassDefinition3": {
                    "classname": "Class.Third",
                    "methodname": "Process",
                    "parameters": []
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.Equal(5, result.Count);
        Assert.IsType<VariableDefinition>(result[0]);
        Assert.Equal(3, result.Count(d => d is ClassDefinition));
        Assert.IsType<ExitDefinition>(result[4]);
    }

    [Fact]
    public void MultipleBlocksWithNumericSuffix_DeserializesCorrectValues()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "<@@Test1@@>": 13,
                    "<@@Test2@@>": 15,
                    "<@@Test3@@>": "andy",
                    "<@@Test4@@>": "58"
                },
                "ClassDefinition1": {
                    "classname": "MyClass.First",
                    "methodname": "Run",
                    "parameters": ["p1"]
                },
                "ClassDefinition2": {
                    "classname": "MyClass.Second",
                    "methodname": "Execute",
                    "parameters": ["p1", "p2"]
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.Equal(4, result.Count);
        Assert.IsType<VariableDefinition>(result[0]);
        var class1 = Assert.IsType<ClassDefinition>(result[1]);
        var class2 = Assert.IsType<ClassDefinition>(result[2]);
        Assert.Equal("MyClass.First", class1.ClassName);
        Assert.Equal("MyClass.Second", class2.ClassName);
        Assert.IsType<ExitDefinition>(result[3]);
    }

    // ParseJson validation tests

    [Fact]
    public void ParseJson_NullJson_ThrowsArgumentException()
    {
        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(null!, instance.EffectiveDate, instance.EnvironmentName);
        Assert.Throws<ArgumentException>(() => JsonParser.VerifyJson());
    }

    [Fact]
    public void ParseJson_EmptyJson_ThrowsArgumentException()
    {
        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser("", instance.EffectiveDate, instance.EnvironmentName);
        Assert.Throws<ArgumentException>(() => JsonParser.VerifyJson());
    }

    [Fact]
    public void ParseJson_InvalidJson_ThrowsFormatException()
    {
        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser("not valid json", instance.EffectiveDate, instance.EnvironmentName);
        Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
    }

    [Fact]
    public void ParseJson_JsonArray_ThrowsFormatException()
    {
        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser("[]", instance.EffectiveDate, instance.EnvironmentName);
        Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
    }

    [Fact]
    public void ParseJson_EmptyObject_ThrowsFormatException()
    {
        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser("{}", instance.EffectiveDate, instance.EnvironmentName);
        Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
    }

    [Fact]
    public void ParseJson_UnknownBlock_ThrowsKeyNotFoundException()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "id": 1,
                    "role": "Admin",
                    "permissions": [],
                    "isActive": true
                },
                "UnknownDefinition": { "foo": "bar" },
                {{GetExitDefinitionJson()}}
            }
            """;
        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json, instance.EffectiveDate, instance.EnvironmentName);
        var ex = Assert.Throws<KeyNotFoundException>(() => JsonParser.VerifyJson());
        Assert.Contains("UnknownDefinition", ex.Message);
    }

    [Fact]
    public void ParseJson_DuplicateKeys_ThrowsFormatException()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "id": 1,
                    "role": "Admin",
                    "permissions": [],
                    "isActive": true
                },
                "ClassDefinition1": {
                    "classname": "MyClass",
                    "methodname": "Run",
                    "parameters": []
                },
                "ClassDefinition1": {
                    "classname": "MyClass2",
                    "methodname": "Execute",
                    "parameters": []
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json, instance.EffectiveDate, instance.EnvironmentName);
        var ex = Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
        Assert.Contains("Duplicate", ex.Message);
    }

    [Fact]
    public void ParseJson_ValidJson_NoException()
    {
        var json = $$"""
            {
                "VariableDefinition": {
                    "id": 1,
                    "role": "Admin",
                    "permissions": [],
                    "isActive": true
                },
                "ClassDefinition1": {
                    "classname": "MyClass",
                    "methodname": "Run",
                    "parameters": []
                },
                {{GetExitDefinitionJson()}}
            }
            """;

        TaskInstance instance = GetTaskInstance();
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json, instance.EffectiveDate, instance.EnvironmentName);
        JsonParser.VerifyJson(json);
        Assert.True(true);
    }
}
