using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Tasks;
using Xunit;
using System.Reflection;

namespace TaskWorkflow.UnitTests.TaskFactory;

public class DeserializeDefinitionBlocksTests
{
    private static string GetValidJson() => """
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
                "SchemaDefinition": {
                    "version": "v2.1",
                    "lastUpdated": "2024-05-20T14:30:00Z",
                    "isDeprecated": false,
                    "author": "DevOps Team"
                }
                }
        """;

    // Helper: ParseJson + DeserializeDefinitionBlocks
    private static List<IDefinition> ParseAndDeserialize(string json)
    {
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json);
        VariableDefinition VariableDefinitionBlock = JsonParser.VerifyJson();
        if (VariableDefinitionBlock != null)
        {
            // Assign variables to class
            var variables = VariableDefinitionBlock.Variables;

            // Apply variable replacement to Json
            json = JsonParser.ApplyVariableReplacementsToJson(json, VariableDefinitionBlock);
        }

        // Get final block definition
        return JsonParser.DeserializeDefinitionBlocks(json);
    }

    // DeserializeDefinitionBlocks tests

    [Fact]
    public void ValidJson_ReturnsThreeDefinitions()
    {
        var result = ParseAndDeserialize(GetValidJson());
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ValidJson_ReturnsCorrectTypes()
    {
        var result = ParseAndDeserialize(GetValidJson());
        Assert.IsType<VariableDefinition>(result[0]);
        Assert.IsType<ClassDefinition>(result[1]);
        Assert.IsType<SchemaDefinition>(result[2]);
    }

    [Fact]
    public void ValidJson_VariableDefinition_DeserializesCorrectly_into_Dictionary()
    {
        var result = ParseAndDeserialize(GetValidJson());
        var variable = result[0] as VariableDefinition;

        Assert.NotNull(variable);
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
    public void ValidJson_SchemaDefinition_DeserializesCorrectly()
    {
        var result = ParseAndDeserialize(GetValidJson());
        var schema = result[2] as SchemaDefinition;

        Assert.NotNull(schema);
        Assert.Equal("v2.1", schema.Version);
        Assert.Equal("DevOps Team", schema.Author);
        Assert.False(schema.IsDeprecated);
    }

    [Fact]
    public void SingleDefinitionBlock_ReturnsSingleItem()
    {
        var json = """
            {
                "VariableDefinition": {
                    "id": 1,
                    "role": "Admin",
                    "permissions": [],
                    "isActive": true
                }
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.Single(result);
        Assert.IsType<VariableDefinition>(result[0]);
    }

    [Fact]
    public void MultipleBlocksWithNumericSuffix_ReturnsCorrectCount()
    {
        var json = """
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
                }
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.Equal(4, result.Count);
        Assert.IsType<VariableDefinition>(result[0]);
        Assert.Equal(3, result.Skip(1).Count(d => d is ClassDefinition));
    }

    [Fact]
    public void MultipleBlocksWithNumericSuffix_DeserializesCorrectValues()
    {
        var json = """
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
                }
            }
            """;

        var result = ParseAndDeserialize(json);

        Assert.Equal(3, result.Count);
        Assert.IsType<VariableDefinition>(result[0]);
        var class1 = Assert.IsType<ClassDefinition>(result[1]);
        var class2 = Assert.IsType<ClassDefinition>(result[2]);
        Assert.Equal("MyClass.First", class1.ClassName);
        Assert.Equal("MyClass.Second", class2.ClassName);
    }

    // ParseJson validation tests

    [Fact]
    public void ParseJson_NullJson_ThrowsArgumentException()
    {
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(null!);
        Assert.Throws<ArgumentException>(() => JsonParser.VerifyJson());
    }

    [Fact]
    public void ParseJson_EmptyJson_ThrowsArgumentException()
    {
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser("");
        Assert.Throws<ArgumentException>(() => JsonParser.VerifyJson());
    }

    [Fact]
    public void ParseJson_InvalidJson_ThrowsFormatException()
    {
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser("not valid json");
        Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
    }

    [Fact]
    public void ParseJson_JsonArray_ThrowsFormatException()
    {
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser("[]");
        Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
    }

    [Fact]
    public void ParseJson_EmptyObject_ThrowsFormatException()
    {
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser("{}");
        Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
    }

    [Fact]
    public void ParseJson_UnknownBlock_ThrowsKeyNotFoundException()
    {
        var json = """
            {
                "VariableDefinition": {
                    "id": 1,
                    "role": "Admin",
                    "permissions": [],
                    "isActive": true
                },
                "UnknownDefinition": { "foo": "bar" }
            }
            """;
        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json);
        var ex = Assert.Throws<KeyNotFoundException>(() => JsonParser.VerifyJson());
        Assert.Contains("UnknownDefinition", ex.Message);
    }

    [Fact]
    public void ParseJson_DuplicateKeys_ThrowsFormatException()
    {
        var json = """
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
                }
            }
            """;

        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json);
        var ex = Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
        Assert.Contains("Duplicate", ex.Message);
    }

    [Fact]
    public void ParseJson_VariableDefinitionNotFirst_ThrowsFormatException()
    {
        var json = """
            {
                "ClassDefinition": {
                    "classname": "MyClass",
                    "methodname": "Run",
                    "parameters": []
                },
                "VariableDefinition": {
                    "id": 1,
                    "role": "Admin",
                    "permissions": [],
                    "isActive": true
                }
            }
            """;

        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json);
        var ex = Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
        Assert.Contains("VariableDefinition should always be the first definition block", ex.Message);
    }

    [Fact]
    public void ParseJson_VariableDefinitionWithSuffix_ThrowsFormatException()
    {
        var json = """
            {
                "VariableDefinition1": {
                    "id": 1,
                    "role": "Admin",
                    "permissions": [],
                    "isActive": true
                }
            }
            """;

        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json);
        var ex = Assert.Throws<FormatException>(() => JsonParser.VerifyJson());
        Assert.Contains("must not have a numeric suffix", ex.Message);
    }

    [Fact]
    public void ParseJson_ValidJson_NoException()
    {
        var json = """
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
                }
            }
            """;

        WorkflowTaskJsonParser JsonParser = new WorkflowTaskJsonParser(json);
        JsonParser.VerifyJson(json);
        Assert.True(true);
    }
}
