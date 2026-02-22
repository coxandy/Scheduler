using Serilog;
using TaskWorkflow.TaskFactory.Interfaces;
using TaskWorkflow.Common.Tasks;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.BlockDefinition;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;

namespace TaskWorkflow.TaskFactory.DefinitionBlocks;

public class FileDefinition : IDefinition
{
    public bool IsActive { get; set; } = true;
    public string BlockName { get; set; } = string.Empty;
    public eOnError OnError { get; set; } = eOnError.AbortTaskAndReportError;

    public List<DefinitionFile> Files { get; set; } = [];

    public async Task RunDefinitionBlockAsync(TaskInstance taskInstance, IServiceProvider serviceProvider, TaskContext taskContext)
    {
        Log.Debug($"RunDefinitionBlockAsync() - RunId: {taskInstance.RunId}  Running {GetType().Name}..");
        foreach (var file in Files)
        {
            await ProcessFileAsync(file);
        }
    }

    private static async Task ProcessFileAsync(DefinitionFile file)
    {
        await Task.Run(() =>
        {
            var sourcePath = Path.Combine(file.SourceFilePath, file.SourceFileName);
            var targetPath = Path.Combine(file.TargetFilePath, file.TargetFileName);

            switch (file.Action)
            {
                case eFileAction.Copy:
                    Log.Debug($"Copying '{sourcePath}' to '{targetPath}'");
                    EnsureTargetDirectory(file.TargetFilePath);
                    System.IO.File.Copy(sourcePath, targetPath, overwrite: true);
                    break;

                case eFileAction.Move:
                    Log.Debug($"Moving '{sourcePath}' to '{targetPath}'");
                    EnsureTargetDirectory(file.TargetFilePath);
                    System.IO.File.Move(sourcePath, targetPath, overwrite: true);
                    break;

                case eFileAction.Delete:
                    Log.Debug($"Deleting '{sourcePath}'");
                    if (!System.IO.File.Exists(sourcePath))
                        throw new FileNotFoundException($"File not found for deletion: '{sourcePath}'");
                    System.IO.File.Delete(sourcePath);
                    break;

                case eFileAction.Transform:
                    Log.Debug($"Transforming '{sourcePath}' to '{targetPath}' with {file.Transforms.Count} transform(s)");
                    EnsureTargetDirectory(file.TargetFilePath);
                    ApplyTransforms(file, sourcePath, targetPath);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(file.Action), $"Unknown file action: {file.Action}");
            }
        });
    }

    private static void ApplyTransforms(DefinitionFile file, string sourcePath, string targetPath)
    {
        if (!System.IO.File.Exists(sourcePath))
            throw new FileNotFoundException($"Source file not found: '{sourcePath}'");

        var lines = System.IO.File.ReadAllLines(sourcePath).ToList();

        foreach (var transform in file.Transforms)
        {
            Log.Debug($"Applying transform '{transform.TransformType}' with param '{transform.TransformParam}'");
            lines = ApplyTransform(lines, transform);
        }

        System.IO.File.WriteAllLines(targetPath, lines);
    }

    private static List<string> ApplyTransform(List<string> lines, FileTransform transform)
    {
        // Transform dispatch — extend TransformType cases as needed
        return transform.TransformType switch
        {
            _ => throw new NotSupportedException($"Transform type '{transform.TransformType}' is not supported.")
        };
    }

    private static void EnsureTargetDirectory(string targetFilePath)
    {
        if (!Directory.Exists(targetFilePath))
            Directory.CreateDirectory(targetFilePath);
    }
}
