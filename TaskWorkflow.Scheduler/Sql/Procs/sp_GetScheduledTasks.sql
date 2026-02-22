USE Scheduler
GO

CREATE OR ALTER PROCEDURE sp_GetScheduledTasks
    @TaskId BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        TaskId,
        IsActive,
        CronExpression,
        TaskName,
        Description,
        LastRunTime,
        Status,
        WebService,
        DayOffset,
        TaskJsonDefinitionId
    FROM ScheduledTask
    WHERE (@TaskId IS NULL OR TaskId = @TaskId);
END
GO