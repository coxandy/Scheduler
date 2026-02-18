namespace TaskWorkflow.Common.Models.BlockDefinition.Enums;

public enum eOnError
{
    Skip=1,
    SkipAndReportError=1,
    AbortTask=2,
    AbortTaskAndReportError=3
}