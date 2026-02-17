using System.Text.RegularExpressions;

namespace TaskWorkflow.Common.Helpers;

public static class CommonExcelHelper
{

    public static (int Row, int Column) ParseTopLeft(string? topLeft)
    {
        if (string.IsNullOrWhiteSpace(topLeft))
            return (1, 1);

        var match = Regex.Match(topLeft.Trim(), @"^([A-Za-z]+)(\d+)$");
        if (!match.Success)
            return (1, 1);

        var colLetters = match.Groups[1].Value.ToUpper();
        var row = int.Parse(match.Groups[2].Value);
        int col = 0;
        foreach (char c in colLetters)
        {
            col = col * 26 + (c - 'A' + 1);
        }
        return (row, col);
    }
}