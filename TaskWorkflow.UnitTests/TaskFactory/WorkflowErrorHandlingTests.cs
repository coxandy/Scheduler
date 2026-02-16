using Moq;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.Enums;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.TaskFactory.Tasks;
using Xunit;

namespace TaskWorkflow.UnitTests.TaskFactory;

public class WorkflowErrorHandlingTests
{
    private static readonly string _validJson = """
        {
            "VariableDefinition": {
                "Variables": {
                    "<@@Param1@@>": "value1"
                },
                "IsActive": true
            },
            "ClassDefinition": {
                "classname": "Test.Class",
                "methodname": "Run",
                "parameters": ["<@@Param1@@>"]
            },
            "ExitDefinition": {
                "isActive": true,
                "success": { "email": false, "to": [], "subject": "", "body": "", "priority": "Normal", "attachments": [] },
                "failure": { "email": false, "to": [], "subject": "", "body": "", "priority": "Normal", "attachments": [] }
            }
        }
        """;

    private static TaskInstance GetTaskInstance() => new TaskInstance
    {
        EffectiveDate = new DateTime(2026, 10, 5),
        RunId = Guid.CreateVersion7().ToString(),
        IsManual = false,
        EnvironmentName = "Development"
    };

    private static GenericWorkflowTask CreateTaskWithBlocks(List<IDefinition> blocks)
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var task = new GenericWorkflowTask(_validJson, GetTaskInstance(), mockServiceProvider.Object);
        task.SetDefinitionBlocks(blocks);
        return task;
    }

    private static Mock<IDefinition> CreateMockBlock(string blockName, eOnError onError, bool throws = false)
    {
        var mock = new Mock<IDefinition>();
        mock.Setup(d => d.IsActive).Returns(true);
        mock.Setup(d => d.BlockName).Returns(blockName);
        mock.Setup(d => d.OnError).Returns(onError);

        if (throws)
        {
            mock.Setup(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()))
                .ThrowsAsync(new InvalidOperationException($"Error in {blockName}"));
        }
        else
        {
            mock.Setup(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()))
                .Returns(Task.CompletedTask);
        }

        return mock;
    }

    [Fact]
    public async Task Run_AbortTask_StopsExecutionOnError()
    {
        var block1 = CreateMockBlock("ClassDefinition1", eOnError.AbortTask, throws: true);
        var block2 = CreateMockBlock("ClassDefinition2", eOnError.Skip);

        var task = CreateTaskWithBlocks([block1.Object, block2.Object]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => task.Run());

        block1.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Once);
        block2.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Never);
    }

    [Fact]
    public async Task Run_Skip_ContinuesExecutionOnError()
    {
        var block1 = CreateMockBlock("ClassDefinition1", eOnError.Skip, throws: true);
        var block2 = CreateMockBlock("ClassDefinition2", eOnError.Skip);

        var task = CreateTaskWithBlocks([block1.Object, block2.Object]);

        await task.Run();

        block1.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Once);
        block2.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Once);
    }

    [Fact]
    public async Task Run_Skip_ThenAbort_StopsAtAbortBlock()
    {
        var block1 = CreateMockBlock("ClassDefinition1", eOnError.Skip, throws: true);
        var block2 = CreateMockBlock("ClassDefinition2", eOnError.AbortTask, throws: true);
        var block3 = CreateMockBlock("ClassDefinition3", eOnError.Skip);

        var task = CreateTaskWithBlocks([block1.Object, block2.Object, block3.Object]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => task.Run());

        block1.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Once);
        block2.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Once);
        block3.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Never);
    }

    [Fact]
    public async Task Run_NoErrors_AllBlocksExecute()
    {
        var block1 = CreateMockBlock("ClassDefinition1", eOnError.AbortTask);
        var block2 = CreateMockBlock("ClassDefinition2", eOnError.AbortTask);
        var block3 = CreateMockBlock("ClassDefinition3", eOnError.Skip);

        var task = CreateTaskWithBlocks([block1.Object, block2.Object, block3.Object]);

        await task.Run();

        block1.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Once);
        block2.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Once);
        block3.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Once);
    }

    [Fact]
    public async Task Run_InactiveBlock_IsSkipped()
    {
        var block1 = CreateMockBlock("ClassDefinition1", eOnError.AbortTask);
        var block2 = CreateMockBlock("ClassDefinition2", eOnError.AbortTask);
        block2.Setup(d => d.IsActive).Returns(false);
        var block3 = CreateMockBlock("ClassDefinition3", eOnError.AbortTask);

        var task = CreateTaskWithBlocks([block1.Object, block2.Object, block3.Object]);

        await task.Run();

        block1.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Once);
        block2.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Never);
        block3.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Once);
    }

    [Fact]
    public async Task Run_MultipleSkipErrors_AllBlocksStillExecute()
    {
        var block1 = CreateMockBlock("ClassDefinition1", eOnError.Skip, throws: true);
        var block2 = CreateMockBlock("ClassDefinition2", eOnError.Skip, throws: true);
        var block3 = CreateMockBlock("ClassDefinition3", eOnError.Skip);

        var task = CreateTaskWithBlocks([block1.Object, block2.Object, block3.Object]);

        await task.Run();

        block1.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Once);
        block2.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Once);
        block3.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()), Times.Once);
    }

    [Fact]
    public async Task Run_AbortTask_ThrowsOriginalException()
    {
        var block1 = CreateMockBlock("ClassDefinition1", eOnError.AbortTask);
        block1.Setup(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var task = CreateTaskWithBlocks([block1.Object]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => task.Run());
        Assert.Equal("Database connection failed", ex.Message);
    }

    [Fact]
    public async Task Run_DefaultOnError_IsAbortTask()
    {
        var classDef = new ClassDefinition();
        Assert.Equal(eOnError.AbortTask, classDef.OnError);

        var schemaDef = new SchemaDefinition();
        Assert.Equal(eOnError.AbortTask, schemaDef.OnError);

        var exitDef = new ExitDefinition();
        Assert.Equal(eOnError.AbortTask, exitDef.OnError);

        var varDef = new VariableDefinition();
        Assert.Equal(eOnError.AbortTask, varDef.OnError);
    }
}
