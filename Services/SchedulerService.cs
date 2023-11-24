// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace API.Services;

public class SchedulerService(
    ILogger<SchedulerService> logger)
    : BackgroundService
{
    private const string ServiceName = "SchedulerService";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            TaskSchedulerService.Tasks.Add(new ScheduledTask
                { Name = "UserRefresh", IsRunning = false, LastRun = null });
            TaskSchedulerService.Tasks.Add(new ScheduledTask
                { Name = "VendorsGunsmith", IsRunning = false, LastRun = null });
            TaskSchedulerService.Tasks.Add(new ScheduledTask
                { Name = "VendorsXur", IsRunning = false, LastRun = null });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception in {service}", ServiceName);
        }

        return Task.CompletedTask;
    }
}

public static class TaskSchedulerService
{
    public static List<ScheduledTask> Tasks { get; set; } = [];
}

public class ScheduledTask
{
    public string Name { get; init; } = "Unknown Task";
    public bool IsRunning { get; set; }
    public DateTime? LastRun { get; set; }
}
