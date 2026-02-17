using TaskWorkflow.Common.Models.BlockDefinition.Enums;

namespace TaskWorkflow.Common.Models.BlockDefinition;

public class Spreadsheet
{
    public string Filename { get; set; }
    public eSpreadsheetOperation Operation { get; set; }
    public List<Worksheet> Worksheets { get; set; }
}