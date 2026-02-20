using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.TaskFactory.Tasks;
using TaskWorkflow.Common.Models;
using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

namespace TaskWorkflow.UnitTests.TaskFactory;

public class DeserializeDefinitionBlocksTests
{
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
        Assert.Throws<ArgumentException>(() => new WorkflowTaskJsonParser(null!, instance));
    }

    [Fact]
    public void ParseJson_EmptyJson_ThrowsArgumentException()
    {
        TaskInstance instance = GetTaskInstance();
        Assert.Throws<ArgumentException>(() => new WorkflowTaskJsonParser("", instance));
    }

    [Fact]
    public void ParseJson_InvalidJson_ThrowsFormatException()
    {
        TaskInstance instance = GetTaskInstance();
        Assert.Throws<FormatException>(() => new WorkflowTaskJsonParser("not valid json", instance));
    }

    [Fact]
    public void ParseJson_JsonArray_ThrowsFormatException()
    {
        TaskInstance instance = GetTaskInstance();
        Assert.Throws<FormatException>(() => new WorkflowTaskJsonParser("[]", instance));
    }

    [Fact]
    public void ParseJson_EmptyObject_ThrowsFormatException()
    {
        TaskInstance instance = GetTaskInstance();
        Assert.Throws<FormatException>(() => new WorkflowTaskJsonParser("{}", instance));
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
        var ex = Assert.Throws<KeyNotFoundException>(() => new WorkflowTaskJsonParser(json, instance));
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
        var ex = Assert.Throws<FormatException>(() => new WorkflowTaskJsonParser(json, instance));
        Assert.Contains("Duplicate", ex.Message);
    }
}
