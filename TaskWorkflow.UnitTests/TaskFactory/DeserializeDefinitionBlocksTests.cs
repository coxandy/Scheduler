using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.TaskFactory.Tasks.Base;
using Xunit;

namespace TaskWorkflow.UnitTests.TaskFactory;

public class DeserializeDefinitionBlocksTests
{
    private static string GetValidJson() => """
        {
            "VariableDefinition": {
                "id": 101,
                "role": "Administrator",
                "permissions": ["read", "write", "delete"],
                "isActive": true
            },
            "ClassDefinition": {
                "classname": "Web3.Api.GetBalancesByEpoch",
                "methodname": "GetBalancesByEpoch",
                "parameters": ["arrayval1", "arrayval2"]
            },
            "SchemaDefinition": {
                "version": "v2.1",
                "lastUpdated": "2024-05-20T14:30:00Z",
                "isDeprecated": false,
                "author": "DevOps Team"
            }
        }
        """;

    [Fact]
    public void ValidJson_ReturnsThreeDefinitions()
    {
        var result = BaseTask.DeserializeDefinitionBlocks(GetValidJson());

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ValidJson_ReturnsCorrectTypes()
    {
        var result = BaseTask.DeserializeDefinitionBlocks(GetValidJson());

        Assert.IsType<VariableDefinition>(result[0]);
        Assert.IsType<ClassDefinition>(result[1]);
        Assert.IsType<SchemaDefinition>(result[2]);
    }

    [Fact]
    public void ValidJson_VariableDefinition_DeserializesCorrectly()
    {
        var result = BaseTask.DeserializeDefinitionBlocks(GetValidJson());
        var variable = result[0] as VariableDefinition;

        Assert.NotNull(variable);
        Assert.Equal(101, variable.Id);
        Assert.Equal("Administrator", variable.Role);
        Assert.Equal(3, variable.Permissions.Count);
        Assert.True(variable.IsActive);
    }

    [Fact]
    public void ValidJson_ClassDefinition_DeserializesCorrectly()
    {
        var result = BaseTask.DeserializeDefinitionBlocks(GetValidJson());
        var classDef = result[1] as ClassDefinition;

        Assert.NotNull(classDef);
        Assert.Equal("Web3.Api.GetBalancesByEpoch", classDef.ClassName);
        Assert.Equal("GetBalancesByEpoch", classDef.MethodName);
        Assert.Equal(2, classDef.Parameters.Count);
    }

    [Fact]
    public void ValidJson_SchemaDefinition_DeserializesCorrectly()
    {
        var result = BaseTask.DeserializeDefinitionBlocks(GetValidJson());
        var schema = result[2] as SchemaDefinition;

        Assert.NotNull(schema);
        Assert.Equal("v2.1", schema.Version);
        Assert.Equal("DevOps Team", schema.Author);
        Assert.False(schema.IsDeprecated);
    }

    [Fact]
    public void NullJson_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => BaseTask.DeserializeDefinitionBlocks(null!));
    }

    [Fact]
    public void EmptyJson_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => BaseTask.DeserializeDefinitionBlocks(""));
    }

    [Fact]
    public void InvalidJson_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => BaseTask.DeserializeDefinitionBlocks("not valid json"));
    }

    [Fact]
    public void JsonArray_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => BaseTask.DeserializeDefinitionBlocks("[]"));
    }

    [Fact]
    public void UnknownDefinitionBlock_ThrowsKeyNotFoundException()
    {
        var json = """
            {
                "UnknownDefinition": { "foo": "bar" }
            }
            """;

        var ex = Assert.Throws<KeyNotFoundException>(() => BaseTask.DeserializeDefinitionBlocks(json));
        Assert.Contains("UnknownDefinition", ex.Message);
    }

    [Fact]
    public void EmptyObject_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => BaseTask.DeserializeDefinitionBlocks("{}"));
    }

    [Fact]
    public void SingleDefinitionBlock_ReturnsSingleItem()
    {
        var json = """
            {
                "ClassDefinition": {
                    "classname": "MyClass",
                    "methodname": "MyMethod",
                    "parameters": []
                }
            }
            """;

        var result = BaseTask.DeserializeDefinitionBlocks(json);

        Assert.Single(result);
        Assert.IsType<ClassDefinition>(result[0]);
    }
}
