USE Scheduler
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ScheduledTask')
BEGIN
    DROP TABLE ScheduledTask;
END
GO


-- 1. Create the table structure aligned with C# ScheduledTask class
CREATE TABLE ScheduledTask (
    TaskId BIGINT, -- long
    IsActive BIT NOT NULL, -- bool
    CronExpression NVARCHAR(100) NOT NULL,
    TaskName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NOT NULL,
    ClassName NVARCHAR(255) NOT NULL,
    LastRunTime DATETIME2 NOT NULL, -- DateTime
    Status NVARCHAR(50) NOT NULL,
    WebService NVARCHAR(100) NOT NULL,
    DayOffset INT NOT NULL, -- int
    TaskJsonDefinitionId BIGINT NOT NULL -- long
);
GO

CREATE CLUSTERED INDEX CIDX_ScheduledTask_IsActive ON ScheduledTask (TaskId, IsActive);
GO

-- 2. Insert the data
INSERT INTO ScheduledTask 
(
    TaskId, 
    IsActive, 
    CronExpression, 
    TaskName, 
    Description, 
    ClassName,
    LastRunTime, 
    Status, 
    WebService, 
    DayOffset, 
    TaskJsonDefinitionId
)
VALUES 
(1, 0, '*/5 * * * *', 'Task1', 'Task1 every 5 mins', 'GenericWorkflowTask', '2026-02-13 14:23:34', 'Completed', 'WebService1', -1, 1),
(2, 0, '*/10 * * * *', 'Task2', 'Task2 every 10 mins', 'GenericWorkflowTask', '2026-02-13 09:23:08', 'Completed', 'WebService2', -1, 1),
(3, 0, '*/20 * * * *', 'Task3', 'Task3 every 20 mins', 'GenericWorkflowTask', '2026-02-13 09:47:17', 'Completed', 'WebService1', -1, 1),
(4, 0, '*/15 * * * *', 'Task4', 'Task4 every 15 mins', 'GenericWorkflowTask', '2026-02-13 10:28:51', 'Completed', 'WebService3', -1, 1),
(5, 0, '15,30,45 12,13 * * *', 'Task5', 'Task5 blah blah', 'GenericWorkflowTask', '2026-02-13 10:28:51', 'Completed', 'WebService1', -1, 1),
(6, 0, '15,45 12 * * *', 'Task6', 'Task6 at 12:15 and 12:45', 'GenericWorkflowTask', '2026-02-13 10:28:51', 'Completed', 'WebService2', -1, 1),
(7, 0, '10,30,37 12,15,16 * * *', 'Task7', 'Task7 blah blah', 'GenericWorkflowTask', '2026-02-13 10:28:51', 'Completed', 'WebService1', -1, 1),
(8, 0, '5,12,37 12,13,14,15,16 * * *', 'Task8', 'Task8 blah blah', 'GenericWorkflowTask', '2026-02-13 10:28:51', 'Completed', 'WebService2', -1, 1),
(9, 1, '5,12,37 11,14 * * *', 'Task9', 'Task9 blah blah', 'GenericWorkflowTask', '2026-02-13 08:28:51', 'Completed', 'WebService3', -1, 1);
GO
