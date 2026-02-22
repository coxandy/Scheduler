USE Scheduler
GO

CREATE OR ALTER PROCEDURE sp_UpdateTaskStatus
    @TaskId BIGINT,
    @Status INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE ScheduledTask
    SET [Status] = @Status
    WHERE TaskId = @TaskId
END
GO
