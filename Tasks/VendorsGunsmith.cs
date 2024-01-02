using API.Contexts;
using API.Services;
using API.Util;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Service.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace API.Tasks;

public class VendorsGunsmith(
    IServiceProvider services,
    ILogger<VendorsGunsmith> logger,
    IBungieClient bungieClient)
    : BackgroundService
{
    private const string ServiceName = "VendorsGunsmith";
    private const uint VendorId = DefinitionHashes.Vendors.Banshee44_672118013;

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

                var vendorUser = db.Users.Include(u => u.BungieProfiles)
                    .FirstOrDefault(x => x.Id == UserExtensions.SignId(Variables.OwnerId));

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
                while(!done)
                {
                    done = await VendorTools.SingleVendorUpdate(bungieClient, db, vendorProfile, VendorId, 0, stoppingToken);

                    if(!done)
                        await Task.Delay(DateTimeExtensions.GetRoundTimeSpan(10), stoppingToken);
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
