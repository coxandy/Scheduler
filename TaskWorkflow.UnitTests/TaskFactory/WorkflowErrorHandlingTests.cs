using Moq;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using TaskWorkflow.TaskFactory.DefinitionBlocks;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.TaskFactory.Tasks;
using Xunit;
using static TaskWorkflow.UnitTests.Helpers.TestHelpers;

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
                "success": { "sendEmail": false, "to": [], "cc": [], "bcc": [], "subject": "", "body": "", "priority": "Normal", "attachments": [] },
                "failure": { "sendEmail": false, "to": [], "cc": [], "bcc": [], "subject": "", "body": "", "priority": "Normal", "attachments": [] }
            }
        }
        """;

    private static GenericWorkflowTask CreateTaskWithBlocks(List<IDefinition> blocks)
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var task = new GenericWorkflowTask(_validJson, GetTaskInstance(), mockServiceProvider.Object);

        // Preserve the parsed ExitDefinition so ProcessTaskErrorAsync can find it
        var exitDef = task.GetDefinitionBlocks().OfType<ExitDefinition>().FirstOrDefault();
        if (exitDef != null) blocks.Add(exitDef);

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
            mock.Setup(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()))
                .ThrowsAsync(new InvalidOperationException($"Error in {blockName}"));
        }
        else
        {
            mock.Setup(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()))
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

        block1.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()), Times.Once);
        block2.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()), Times.Never);
    }

    [Fact]
    public async Task Run_Skip_ContinuesExecutionOnError()
    {
        var block1 = CreateMockBlock("ClassDefinition1", eOnError.Skip, throws: true);
        var block2 = CreateMockBlock("ClassDefinition2", eOnError.Skip);

        var task = CreateTaskWithBlocks([block1.Object, block2.Object]);

        await task.Run();

        block1.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()), Times.Once);
        block2.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()), Times.Once);
    }

    [Fact]
    public async Task Run_Skip_ThenAbort_StopsAtAbortBlock()
    {
        var block1 = CreateMockBlock("ClassDefinition1", eOnError.Skip, throws: true);
        var block2 = CreateMockBlock("ClassDefinition2", eOnError.AbortTask, throws: true);
        var block3 = CreateMockBlock("ClassDefinition3", eOnError.Skip);

        var task = CreateTaskWithBlocks([block1.Object, block2.Object, block3.Object]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => task.Run());

        block1.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()), Times.Once);
        block2.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()), Times.Once);
        block3.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()), Times.Never);
    }

    [Fact]
    public async Task Run_NoErrors_AllBlocksExecute()
    {
        var block1 = CreateMockBlock("ClassDefinition1", eOnError.AbortTask);
        var block2 = CreateMockBlock("ClassDefinition2", eOnError.AbortTask);
        var block3 = CreateMockBlock("ClassDefinition3", eOnError.Skip);

        var task = CreateTaskWithBlocks([block1.Object, block2.Object, block3.Object]);

        await task.Run();

        block1.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()), Times.Once);
        block2.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()), Times.Once);
        block3.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()), Times.Once);
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

        block1.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()), Times.Once);
        block2.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()), Times.Never);
        block3.Verify(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()), Times.Once);
    }

    [Fact]
    public async Task Run_AbortTask_ThrowsOriginalException()
    {
        var block1 = CreateMockBlock("ClassDefinition1", eOnError.AbortTask);
        block1.Setup(d => d.RunDefinitionBlockAsync(It.IsAny<TaskInstance>(), It.IsAny<IServiceProvider>(), It.IsAny<TaskContext>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var task = CreateTaskWithBlocks([block1.Object]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => task.Run());
        Assert.Equal("Database connection failed", ex.Message);
    }

    [Fact]
    public async Task Run_DefaultOnError_IsAbortTask()
    {
        var classDef = new ClassDefinition();
        Assert.Equal(eOnError.AbortTaskAndReportError, classDef.OnError);

        var exitDef = new ExitDefinition();
        Assert.Equal(eOnError.AbortTaskAndReportError, exitDef.OnError);

        var varDef = new VariableDefinition();
        Assert.Equal(eOnError.AbortTaskAndReportError, varDef.OnError);
    }
}
