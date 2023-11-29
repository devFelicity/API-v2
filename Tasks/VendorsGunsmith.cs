using System.Text.Json;
using API.Contexts;
using API.Contexts.Objects;
using API.Services;
using API.Util;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Responses;
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
        using var scope = services.CreateScope();
        var db =
            scope.ServiceProvider
                .GetRequiredService<DbManager>();

        // TODO: raise this to 5/10 minutes
        await Task.Delay(DateTimeExtensions.GetRoundTimeSpan(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            while (TaskSchedulerService.Tasks.First(t => t.Name == "UserRefresh").IsRunning)
                await Task.Delay(DateTimeExtensions.GetRoundTimeSpan(1), stoppingToken);

            TaskSchedulerService.Tasks.First(t => t.Name == ServiceName).IsRunning = true;

            try
            {
                var vendorUser = db.Users.Include(u => u.BungieProfiles)
                    .FirstOrDefault(x => x.Id == UserExtensions.SignId(Variables.OwnerId));

                if (vendorUser == null)
                {
                    logger.LogError("Vendor user not found.");
                    return;
                }

                var vendorProfile = vendorUser.BungieProfiles.First();

                if (vendorProfile.NeedsRefresh())
                    await vendorProfile.RefreshToken(bungieClient, DateTime.UtcNow);

                await db.SaveChangesAsync(stoppingToken);

                var vendorQuery = await bungieClient.ApiAccess.Destiny2.GetVendor(vendorProfile.DestinyMembershipType,
                    vendorProfile.DestinyMembershipId, await vendorProfile.GetLatestCharacter(bungieClient),
                    VendorId,
                    [
                        DestinyComponentType.VendorSales, DestinyComponentType.ItemReusablePlugs
                    ], vendorProfile.GetTokenData(), stoppingToken);

                var queryTime = DateTime.UtcNow;

                foreach (var saleItemComponent in vendorQuery.Response.Sales.Data)
                {
                    if (!saleItemComponent.Value.Item.Select(x => x.ItemType == DestinyItemType.Weapon))
                        continue;

                    var itemId = saleItemComponent.Value.Item.Select(x => x.Hash);

                    var vendorItem = new WeaponSale
                    {
                        IsAvailable = true,
                        ItemPerks = JsonSerializer.Serialize(PopulatePerks(vendorQuery, saleItemComponent.Key)),
                        ItemId = itemId,
                        QueryTime = queryTime,
                        VendorId = VendorId
                    };

                    if (db.WeaponSales.Any(x => x.ItemId == vendorItem.ItemId))
                        db.WeaponSales.Update(vendorItem);
                    else
                        db.WeaponSales.Add(vendorItem);
                }

                foreach (var sale in db.WeaponSales.Where(x => x.VendorId == VendorId && x.QueryTime < queryTime))
                    sale.IsAvailable = false;

                await db.SaveChangesAsync(stoppingToken);
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

    private static List<List<uint>> PopulatePerks(BungieResponse<DestinyVendorResponse> vendorQuery, int key)
    {
        var disallowList = new List<string>
        {
            "Intrinsic",
            "Restore Defaults",
            "Weapon Mod"
        };

        var list = new List<List<uint>>();

        if (!vendorQuery.Response.ItemComponents.ReusablePlugs.Data.TryGetValue(key, out var plugComponent))
            return list;

        foreach (var plugSet in plugComponent.Plugs)
        {
            var plugList = new List<uint>();

            plugList.AddRange(from plug in plugSet.Value
                where !disallowList.Contains(plug.PlugItem.Select(x => x.ItemTypeDisplayName))
                where !plug.PlugItem.Select(x => x.DisplayProperties.Name).Contains(" Frame")
                where !plug.PlugItem.Select(x => x.DisplayProperties.Name).Contains(" Tracker")
                select plug.PlugItem.Select(x => x.Hash));

            if (plugList.Count != 0)
                list.Add(plugList);
        }

        return list;
    }
}
