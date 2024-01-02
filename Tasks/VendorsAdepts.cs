using System.Diagnostics;
using API.Contexts;
using API.Contexts.Objects;
using API.Services;
using API.Util;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Definitions.InventoryItems;
using DotNetBungieAPI.Service.Abstractions;
using Microsoft.EntityFrameworkCore;
using static DotNetBungieAPI.HashReferences.DefinitionHashes;
using UserExtensions = API.Util.UserExtensions;

namespace API.Tasks;

public class VendorsAdepts(
    IServiceProvider services,
    ILogger<VendorsAdepts> logger,
    IBungieClient bungieClient)
    : BackgroundService
{
    private const string ServiceName = "VendorsAdepts";

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

            var nfAdept = "None";
            var trialsAdept = "None";

            var currentTime = DateTime.UtcNow;

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

                var charactersQuery = await bungieClient.ApiAccess.Destiny2.GetProfile(
                    vendorProfile.DestinyMembershipType,
                    vendorProfile.DestinyMembershipId, [DestinyComponentType.Characters],
                    cancellationToken: stoppingToken);

                if (charactersQuery.IsSuccessfulResponseCode)
                {
                    var vendorList = new Dictionary<uint, uint>
                    {
                        {
                            Vendors.FocusedDecoding_502095006,
                            Vendors.Saint14
                        },
                        {
                            Vendors.FocusedDecoding_2232145065,
                            Vendors.CommanderZavala_69482069
                        }
                    };

                    foreach (var vendor in vendorList)
                    {
                        var vendorQuery = await bungieClient.ApiAccess.Destiny2.GetVendor(
                            vendorProfile.DestinyMembershipType, vendorProfile.DestinyMembershipId,
                            charactersQuery.Response.Characters.Data.First().Key, vendor.Key,
                            [
                                DestinyComponentType.VendorSales
                            ], vendorProfile.GetTokenData(), stoppingToken);

                        if (!vendorQuery.IsSuccessfulResponseCode)
                            continue;

                        var adeptWeapon = vendorQuery.Response.Sales.Data
                            .Where(destinyVendorSaleItemComponent =>
                                destinyVendorSaleItemComponent.Value.Item.Select(x =>
                                    x.DisplayProperties.Name.EndsWith("(Adept)")))
                            .Select(destinyVendorSaleItemComponent => destinyVendorSaleItemComponent.Value.Item)
                            .FirstOrDefault();

                        if (!adeptWeapon.HasValidHash)
                            continue;

                        var adeptWeaponId = adeptWeapon.Select(x => x.Hash);

                        if (adeptWeaponId != 0)
                        {
                            var vendorWeapon = WeaponTools.GetWeaponFromDummy(adeptWeapon.Hash ?? 0);

                            logger.LogDebug("Converting {dummyId} to {vendorId}", adeptWeaponId, vendorWeapon);

                            var existingItem = db.WeaponSales.FirstOrDefault(x =>
                                x.VendorId == vendor.Value && x.ItemId == vendorWeapon);
                            if (existingItem != null)
                                db.WeaponSales.Remove(existingItem);

                            await db.SaveChangesAsync(stoppingToken);

                            var vendorItem = new WeaponSale
                            {
                                IsAvailable = true,
                                ItemId = vendorWeapon,
                                ItemPerks = "[[0]]",
                                QueryTime = currentTime,
                                VendorId = vendor.Value
                            };

                            logger.LogInformation("[{service}]: Found adept weapon ID: {weaponId}",
                                "FetchWeaponVendors", vendorWeapon);

                            db.WeaponSales.Add(vendorItem);

                            var validDef = bungieClient.TryGetDefinition<DestinyInventoryItemDefinition>(
                                vendorWeapon, out var adeptWeaponDef);

                            switch (vendor.Key)
                            {
                                case Vendors.FocusedDecoding_502095006:
                                    trialsAdept = validDef
                                        ? $"[{adeptWeaponDef.DisplayProperties.Name}](https://d2foundry.gg/w/{adeptWeaponDef.Hash})"
                                        : "Unknown";
                                    break;
                                case Vendors.FocusedDecoding_2232145065:
                                    nfAdept = validDef
                                        ? $"[{adeptWeaponDef.DisplayProperties.Name}](https://d2foundry.gg/w/{adeptWeaponDef.Hash})"
                                        : "Unknown";
                                    break;
                            }
                        }
                        else
                        {
                            var existingItem =
                                db.WeaponSales.FirstOrDefault(x => x.VendorId == vendor.Value && x.ItemId == 0);
                            if (existingItem != null)
                                db.WeaponSales.Remove(existingItem);

                            await db.SaveChangesAsync(stoppingToken);

                            var vendorItem = new WeaponSale
                            {
                                IsAvailable = true,
                                ItemId = 0,
                                ItemPerks = "[[0]]",
                                QueryTime = currentTime,
                                VendorId = vendor.Value
                            };

                            logger.LogInformation("[{service}]: No adept weapon found.", "FetchWeaponVendors");

                            db.WeaponSales.Add(vendorItem);

                            switch (vendor.Value)
                            {
                                case Vendors.FocusedDecoding_502095006:
                                    trialsAdept = "None";
                                    break;
                                case Vendors.FocusedDecoding_2232145065:
                                    nfAdept = "None";
                                    break;
                            }
                        }
                    }

                    var oldWeapons = db.WeaponSales.Where(x => x.QueryTime < currentTime && x.ItemPerks == "[[0]]")
                        .ToList();

                    foreach (var weaponSale in oldWeapons)
                    {
                        if (weaponSale.IsAvailable)
                            weaponSale.IsAvailable = false;

                        db.WeaponSales.Update(weaponSale);
                    }

                    await db.SaveChangesAsync(stoppingToken);

                    if (!Debugger.IsAttached)
                        await DiscordTools.SendMessage(DiscordTools.WebhookChannel.Vendors,
                            $"**Trials Adept:** {trialsAdept}\n**Nightfall Adept:** {nfAdept}");
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception in {service}", ServiceName);
            }

            TaskSchedulerService.Tasks.First(t => t.Name == ServiceName).IsRunning = false;
            TaskSchedulerService.Tasks.First(t => t.Name == ServiceName).EndTime = DateTime.UtcNow;

            await bungieClient.ResetService.WaitForNextDailyReset(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
