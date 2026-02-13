using TaskWorkflow.Common.Models;

namespace TaskWorkflow.Common.Helpers;

public static class CommonFileHelper
{
    
    public static async Task<List<ScheduledTask>> ReadTaskWorkflowScheduleAsync()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "RunData", "CronSchedule.csv");
        var rows = ReadDelimitedFile(filePath, ',');
        var scheduledTasks = new List<ScheduledTask>(rows.Count);

        foreach (var fields in rows)
        {
            if (fields.Length >= 5)
            {
                scheduledTasks.Add(new ScheduledTask
                {
                    CronExpression = fields[0],
                    TaskName = fields[1],
                    Description = fields[2],
                    LastRunTime = Convert.ToDateTime(fields[3]),
                    Status = fields[4],
                    WebService = fields[5],
                    DayOffset= Convert.ToInt32(fields[6])
                });
            }
        }
        return scheduledTasks;
    }

    public static async Task WriteTaskWorkflowScheduleAsync(ScheduledTask updatedTask)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "RunData", "CronSchedule.csv");
        var allTasks = await ReadTaskWorkflowScheduleAsync();

        var target = allTasks.FirstOrDefault(t => t.TaskName == updatedTask.TaskName);
        if (target != null)
        {
            target.LastRunTime = updatedTask.LastRunTime;
        }

        var lines = new List<string>(allTasks.Count);
        foreach (var task in allTasks)
        {
            var line = $"\"{task.CronExpression}\", \"{task.TaskName}\", \"{task.Description}\", \"{task.LastRunTime:dd MMM yyyy HH:mm:ss}\", \"{task.Status}\", \"{task.WebService}\", \"{task.DayOffset}\"";
            lines.Add(line);
        }

        await File.WriteAllLinesAsync(filePath, lines);
    }

    public static List<string[]> ReadDelimitedFile(string filePath, char delimiter)
    {
        var content = File.ReadAllText(filePath);
        var span = content.AsSpan();
        var rows = new List<string[]>();
        var lineStart = 0;

        while (lineStart < span.Length)
        {
            // Find end of line
            var lineEnd = span[lineStart..].IndexOfAny('\r', '\n');
            ReadOnlySpan<char> line;
            int nextLineStart;

            if (lineEnd < 0)
            {
                line = span[lineStart..];
                nextLineStart = span.Length;
            }
            else
            {
                line = span[lineStart..(lineStart + lineEnd)];
                nextLineStart = lineStart + lineEnd + 1;
                // Handle \r\n
                if (nextLineStart < span.Length && span[nextLineStart] == '\n')
                    nextLineStart++;
            }

            lineStart = nextLineStart;

            if (line.IsEmpty)
                continue;

            rows.Add(ParseDelimitedLine(line, delimiter));
        }

        return rows;
    }

    public static string[] ParseDelimitedLine(ReadOnlySpan<char> line, char delimiter)
    {
        // Count fields for pre-allocation (count delimiters outside quotes)
        var fieldCount = 1;
        var inQuotes = false;
        for (var c = 0; c < line.Length; c++)
        {
            if (line[c] == '"') inQuotes = !inQuotes;
            else if (line[c] == delimiter && !inQuotes) fieldCount++;
        }

        var fields = new string[fieldCount];
        var fieldIndex = 0;
        var i = 0;

        while (i <= line.Length && fieldIndex < fieldCount)
        {
            // Skip whitespace before field
            while (i < line.Length && line[i] == ' ')
                i++;

            if (i >= line.Length)
            {
                fields[fieldIndex++] = string.Empty;
                break;
            }

            if (line[i] == '"')
            {
                // Quoted field
                i++; // skip opening quote
                var start = i;
                while (i < line.Length)
                {
                    if (line[i] == '"')
                    {
                        // Escaped quote ("") or closing quote
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            i += 2; // skip escaped quote
                            continue;
                        }
                        break; // closing quote
                    }
                    i++;
                }

                // Check if field contained escaped quotes
                var fieldSpan = line[start..i];
                fields[fieldIndex++] = fieldSpan.Contains('"')
                    ? fieldSpan.ToString().Replace("\"\"", "\"")
                    : fieldSpan.ToString();

                if (i < line.Length) i++; // skip closing quote

                // Advance past delimiter
                while (i < line.Length && line[i] != delimiter)
                    i++;
                if (i < line.Length) i++; // skip delimiter
            }
            else
            {
                // Unquoted field
                var start = i;
                while (i < line.Length && line[i] != delimiter)
                    i++;
                fields[fieldIndex++] = line[start..i].Trim().ToString();
                if (i < line.Length) i++; // skip delimiter
            }
        }

        return fields;
    }

}