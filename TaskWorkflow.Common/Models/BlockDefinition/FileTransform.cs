using TaskWorkflow.Common.Models.BlockDefinition.Enums;

namespace TaskWorkflow.Common.Models.BlockDefinition;

public class FileTransform
{
    public eFileTransformation TransformType { get; set; }
    public List<string> TransformParams { get; set; } = new();
}
