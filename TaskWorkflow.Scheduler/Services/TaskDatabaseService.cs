using Microsoft.Data.SqlClient;
using TaskWorkflow.Common.Models;
using TaskWorkflow.Common.Models.BlockDefinition.Enums;
using TaskWorkflow.Scheduler.Interfaces;

namespace TaskWorkflow.Scheduler.Services;

public class TaskDatabaseService : ITaskDatabaseService
{
    private readonly string _connectionString;

    public TaskDatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Scheduler")
            ?? throw new InvalidOperationException("Connection string 'Scheduler' is not configured.");
    }

    public async Task<List<ScheduledTask>> GetScheduledTasksAsync()
    {
        var tasks = new List<ScheduledTask>();

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("sp_GetScheduledTasks", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tasks.Add(new ScheduledTask
            {
                TaskId               = reader.GetInt64(reader.GetOrdinal("TaskId")),
                IsActive             = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CronExpression       = reader.GetString(reader.GetOrdinal("CronExpression")),
                TaskName             = reader.GetString(reader.GetOrdinal("TaskName")),
                Description          = reader.GetString(reader.GetOrdinal("Description")),
                LastRunTime          = reader.GetDateTime(reader.GetOrdinal("LastRunTime")),
                Status               = (eTaskStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                WebService           = reader.GetString(reader.GetOrdinal("WebService")),
                DayOffset            = reader.GetInt32(reader.GetOrdinal("DayOffset")),
                TaskJsonDefinitionId = reader.GetInt64(reader.GetOrdinal("TaskJsonDefinitionId"))
            });
        }

        return tasks;
    }

    public async Task UpdateTaskStatusAsync(ScheduledTask scheduledTask)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("sp_UpdateTaskStatus", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@TaskId", scheduledTask.TaskId);
        command.Parameters.AddWithValue("@Status", (int)scheduledTask.Status); //pass id

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }
}
