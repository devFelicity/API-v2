using API.Contexts;
using API.Contexts.Objects;
using API.Services;
using API.Util;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Service.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace API.Tasks;

public class VendorsTrials(
    IServiceProvider services,
    ILogger<VendorsTrials> logger,
    IBungieClient bungieClient)
    : BackgroundService
{
    private const string ServiceName = "VendorsTrials";
    private const uint VendorId = DefinitionHashes.Vendors.Saint14;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // TODO: raise this to 5/10 minutes
        await Task.Delay(DateTimeExtensions.GetRoundTimeSpan(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            while (TaskSchedulerService.Tasks.First(t => t.Name == "UserRefresh").IsRunning)
                await Task.Delay(DateTimeExtensions.GetRoundTimeSpan(1), stoppingToken);

            TaskSchedulerService.Tasks.First(t => t.Name == ServiceName).IsRunning = true;
            TaskSchedulerService.Tasks.First(t => t.Name == ServiceName).StartTime = DateTime.UtcNow;

            try
            {
                using var scope = services.CreateScope();
                var db =
                    scope.ServiceProvider
                        .GetRequiredService<DbManager>();

                var userList = new List<VendorUser?>
                {
                    db.VendorUsers.FirstOrDefault(x => x.VendorId == VendorId && x.Resets == 0 && x.Rank < 10),
                    db.VendorUsers.FirstOrDefault(x => x.VendorId == VendorId && x.Resets == 1 && x.Rank < 10),
                    db.VendorUsers.FirstOrDefault(x => x.VendorId == VendorId && x.Resets == 2 && x.Rank < 10)
                };

                for (var i = 0; i < userList.Count; i++)
                {
                    var targetUser = userList[i];

                    if (targetUser == null)
                        continue;

                    logger.LogDebug("Using {id} for {vendor} at reset count {count}", targetUser.UserId, VendorId, i);

                    var vendorUser = db.Users.Include(u => u.BungieProfiles)
                        .FirstOrDefault(x => x.Id == targetUser.UserId);

                    if (vendorUser == null)
                    {
                        logger.LogError("Vendor user not found.");
                        return;
                    }

                    var vendorProfile = vendorUser.BungieProfiles.First();

                    if (await vendorProfile.NeedsRefresh(bungieClient))
                        await vendorProfile.RefreshToken(bungieClient, DateTime.UtcNow);

                    await db.SaveChangesAsync(stoppingToken);

                    var done = false;
                    while (!done)
                    {
                        done = await VendorTools.SingleVendorUpdate(bungieClient, db, vendorProfile, VendorId, i, stoppingToken);
                        
                        if (!done)
                            await Task.Delay(DateTimeExtensions.GetRoundTimeSpan(10), stoppingToken);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception in {service}", ServiceName);
            }

            TaskSchedulerService.Tasks.First(t => t.Name == ServiceName).IsRunning = false;
            TaskSchedulerService.Tasks.First(t => t.Name == ServiceName).EndTime = DateTime.UtcNow;

            await Task.Delay(DateTimeExtensions.GetRoundTimeSpan(60), stoppingToken);
        }
    }
}
