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
                { Name = "UserRefresh", IsRunning = false });
            TaskSchedulerService.Tasks.Add(new ScheduledTask
                { Name = "VendorsAdepts", IsRunning = false });
            TaskSchedulerService.Tasks.Add(new ScheduledTask
                { Name = "VendorsGunsmith", IsRunning = false });
            TaskSchedulerService.Tasks.Add(new ScheduledTask
                { Name = "VendorsIronBanner", IsRunning = false });
            TaskSchedulerService.Tasks.Add(new ScheduledTask
                { Name = "VendorsTrials", IsRunning = false });
            TaskSchedulerService.Tasks.Add(new ScheduledTask
                { Name = "VendorsWarTable", IsRunning = false });
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
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
