using TaskWorkflow.Common.Helpers;
using TaskWorkflow.Common.Models;

namespace TaskWorkflow.Common.TestRunData;

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
                        "Variables": {
                            "<@@ClassName@@>": "Web3.Api.GetBalancesByEpoch",
                            "<@@MethodName@@>": "GetBalancesByEpoch",
                            "<@@Param1@@>": "arrayval1",
                            "<@@Param2@@>": "arrayval2"
                        },
                        "IsActive": true
                    },
                    "ClassDefinition": {
                        "classname": "<@@ClassName@@>",
                        "methodname": "<@@MethodName@@>",
                        "parameters": ["<@@Param1@@>", "<@@Param2@@>"]
                    },
                    "SchemaDefinition": {
                        "version": "v2.1",
                        "lastUpdated": "2024-05-20T14:30:00Z",
                        "isDeprecated": false,
                        "author": "DevOps Team"
                    },
                    "ExitDefinition": {
                        "isActive": true,
                        "success": { "email": true, "to": ["admin@test.com"], "subject": "Task Succeeded", "body": "Completed", "priority": "Normal", "attachments": [] },
                        "failure": { "email": true, "to": ["admin@test.com"], "subject": "Task Failed", "body": "Error", "priority": "High", "attachments": [] }
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
                    IsActive = (Convert.ToInt16(fields[1]) == 1) ? true : false,
                    CronExpression = fields[2],
                    TaskName = fields[3],
                    Description = fields[4],
                    LastRunTime = Convert.ToDateTime(fields[5]),
                    Status = fields[6],
                    WebService = fields[7],
                    DayOffset= Convert.ToInt32(fields[8])
                });
            }
        }
        return scheduledTasks.Where(x => x.IsActive == true).ToList();
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
            var line = $"\"{task.TaskId}\", \"{Convert.ToInt16(task.IsActive)}\", \"{task.CronExpression}\", \"{task.TaskName}\", \"{task.Description}\", \"{task.LastRunTime:dd MMM yyyy HH:mm:ss}\", \"{task.Status}\", \"{task.WebService}\", \"{task.DayOffset}\", \"{task.TaskJsonDefinitionId}\"";
            lines.Add(line);
        }

        await File.WriteAllLinesAsync(filePath, lines);
    }

}

