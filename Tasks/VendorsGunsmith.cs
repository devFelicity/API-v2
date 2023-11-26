using API.Contexts;
using API.Services;
using API.Util;
using DotNetBungieAPI.Service.Abstractions;

namespace API.Tasks;

public class VendorsGunsmith(
    IServiceProvider services,
    ILogger<VendorsGunsmith> logger,
    IBungieClient bungieClient)
    : BackgroundService
{
    private const string ServiceName = "VendorsGunsmith";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = services.CreateScope();
        var db =
            scope.ServiceProvider
                .GetRequiredService<DbManager>();

        while (!stoppingToken.IsCancellationRequested)
        {
            while (TaskSchedulerService.Tasks.First(t => t.Name == "UserRefresh").IsRunning)
                await Task.Delay(DateTimeExtensions.GetRoundTimeSpan(1), stoppingToken);

            TaskSchedulerService.Tasks.First(t => t.Name == ServiceName).IsRunning = true;

            try
            {
                // do stuff
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception in {service}", ServiceName);
            }

            TaskSchedulerService.Tasks.First(t => t.Name == ServiceName).IsRunning = false;
            TaskSchedulerService.Tasks.First(t => t.Name == ServiceName).LastRun = DateTime.UtcNow;

            await Task.Delay(DateTimeExtensions.GetRoundTimeSpan(60), stoppingToken);
        }
    }
}
