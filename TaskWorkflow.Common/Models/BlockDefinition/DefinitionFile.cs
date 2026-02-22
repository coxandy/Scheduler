using TaskWorkflow.Common.Models.BlockDefinition.Enums;

namespace TaskWorkflow.Common.Models.BlockDefinition;

public class DefinitionFile
{
    public bool HasFileHeader { get; set; }
    public string SourceFilePath { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string TargetFilePath { get; set; } = string.Empty;
    public string TargetFileName { get; set; } = string.Empty;
    public eFileAction Action { get; set; }
    public List<FileTransform> Transforms { get; set; } = [];
}
