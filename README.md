# TaskWorkflow Scheduler

A .NET 10 background service that executes scheduled tasks based on cron expressions, using the [Cronos](https://github.com/HangfireIO/Cronos) library.

## Projects

- **TaskWorkflow.Scheduler** - Worker service that monitors and triggers scheduled tasks. Runs as a Windows service or console application.
- **TaskWorkflow.Common** - Shared library containing models, cron evaluation helpers, and CSV file I/O.

## Configuration

Task schedules are defined in `TaskWorkflow.Scheduler/TestRunData/CronSchedule.csv` using the format:

```
"<cron>", "<TaskName>", "<Description>", "<LastRunTime>", "<Status>", "<WebService>"
```

Application settings are in `appsettings.json` (with per-environment overrides for Development, Staging, and Production):

| Setting | Description | Default |
|---|---|---|
| `Scheduler:MaxConcurrentTasks` | Maximum number of tasks that can run concurrently | 3 |

## Running

```bash
dotnet run --project TaskWorkflow.Scheduler
```

To specify an environment:

```bash
dotnet run --project TaskWorkflow.Scheduler --environment Development
```

## Logging

Uses Serilog with console and rolling file sinks. Log files are written to the `logs/` directory relative to the application base path.
