using TaskWorkflow.Common.Helpers;
using TaskWorkflow.Common.Models;

namespace TaskWorkflow.Scheduler.TestRunData;

public static class TestDataHelper
{
    public static async Task<List<ScheduledTask>> GetTestTasks()
    {
        return await ReadTaskWorkflowScheduleAsync();    
    }

    public static async Task<string> GetTestTaskWorkflowJson(long taskJsonDefinitionId)
    {
        string json = """
                {
                    "VariableDefinition": {
                        "id": 101,
                        "role": "Administrator",
                        "permissions": ["read", "write", "delete"],
                        "isActive": true
                    },
                    "ClassDefinition": {
                        "classname": "Web3.Api.GetBalancesByEpoch",
                        "methodname": "GetBalancesByEpoch",
                        "parameters": ["arrayval1", "arrayval2" ]
                    },
                    "SchemaDefinition": {
                        "version": "v2.1",
                        "lastUpdated": "2024-05-20T14:30:00Z",
                        "isDeprecated": false,
                        "author": "DevOps Team"
                    }
                }
                """;
        return json;
    }

    private static async Task<List<ScheduledTask>> ReadTaskWorkflowScheduleAsync()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "TestRunData", "CronSchedule.csv");
        var rows = CommonFileHelper.ReadDelimitedFile(filePath, ',');
        var scheduledTasks = new List<ScheduledTask>(rows.Count);

        foreach (var fields in rows)
        {
            if (fields.Length >= 5)
            {
                scheduledTasks.Add(new ScheduledTask
                {
                    TaskId = Convert.ToInt64(fields[0]),
                    CronExpression = fields[1],
                    TaskName = fields[2],
                    Description = fields[3],
                    LastRunTime = Convert.ToDateTime(fields[4]),
                    Status = fields[5],
                    WebService = fields[6],
                    DayOffset= Convert.ToInt32(fields[7])
                });
            }
        }
        return scheduledTasks;
    }

    public static async Task WriteTaskWorkflowScheduleAsync(ScheduledTask updatedTask)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "TestRunData", "CronSchedule.csv");
        var allTasks = await ReadTaskWorkflowScheduleAsync();

        var target = allTasks.FirstOrDefault(t => t.TaskName == updatedTask.TaskName);
        if (target != null)
        {
            target.LastRunTime = updatedTask.LastRunTime;
        }

        var lines = new List<string>(allTasks.Count);
        foreach (var task in allTasks)
        {
            var line = $"\"{task.TaskId}\", \"{task.CronExpression}\", \"{task.TaskName}\", \"{task.Description}\", \"{task.LastRunTime:dd MMM yyyy HH:mm:ss}\", \"{task.Status}\", \"{task.WebService}\", \"{task.DayOffset}\", \"{task.TaskJsonDefinitionId}\"";
            lines.Add(line);
        }

        await File.WriteAllLinesAsync(filePath, lines);
    }

}

