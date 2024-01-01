using System.Text.Json;
using API.Contexts;
using API.Contexts.Objects;
using API.Services;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Definitions.Vendors;
using DotNetBungieAPI.Service.Abstractions;

namespace API.Util;

public static class VendorTools
{
    public static async Task<bool> SingleVendorUpdate(
        IBungieClient bungieClient,
        DbManager db,
        BungieProfile vendorProfile,
        uint vendorId,
        int requiredResets,
        CancellationToken stoppingToken)
    {
        var success = false;
        var logger = LogService.CreateLogger("SingleVendorUpdate");

        try
        {
            var vendorQuery = await bungieClient.ApiAccess.Destiny2.GetVendor(vendorProfile.DestinyMembershipType,
                vendorProfile.DestinyMembershipId, await vendorProfile.GetLatestCharacter(bungieClient),
                vendorId,
                [
                    DestinyComponentType.VendorSales, DestinyComponentType.ItemReusablePlugs
                ], vendorProfile.GetTokenData(), stoppingToken);

            var queryTime = DateTime.UtcNow;
            var vendorDefQuery = bungieClient.TryGetDefinition<DestinyVendorDefinition>(vendorId, out var vendor);

            if (!vendorDefQuery)
                throw new Exception("Failed to fetch definition.");

            foreach (var saleItemComponent in vendorQuery.Response.Sales.Data)
            {
                if (!saleItemComponent.Value.Item.Select(x => x.ItemType == DestinyItemType.Weapon))
                    continue;

                var itemId = saleItemComponent.Value.Item.Select(x => x.Hash);

                var vendorItem = new WeaponSale
                {
                    IsAvailable = true,
                    ItemPerks = JsonSerializer.Serialize(WeaponTools.PopulatePerks(vendorQuery, saleItemComponent.Key)),
                    ItemId = itemId,
                    QueryTime = queryTime,
                    VendorId = vendorId,
                    RequiredResets = requiredResets
                };

                if (saleItemComponent.Value.FailureIndexes.Count != 0)
                {
                    var failureString = vendor.FailureStrings[saleItemComponent.Value.FailureIndexes.First()];
                    failureString = failureString.Replace("Requires Rank ", "");
                    vendorItem.RequiredRank = Convert.ToInt32(failureString);
                }

                var existingWeapons = db.WeaponSales.Where(w => w.ItemId == vendorItem.Id && w.VendorId == vendorId)
                    .ToList();

                if (existingWeapons.Count == 0)
                {
                    // Case: Weapon isn't in the db, add it
                    db.WeaponSales.Add(vendorItem);
                }
                else
                {
                    var addWeapon = true;

                    foreach (var existingWeapon in existingWeapons)
                        if (existingWeapon.ItemPerks == vendorItem.ItemPerks)
                        {
                            // Case: Weapon is in the db with the same itemPerks, update querytime and isAvailable
                            existingWeapon.QueryTime = queryTime;
                            existingWeapon.IsAvailable = true;

                            db.WeaponSales.Update(existingWeapon);
                            addWeapon = false;
                        }
                        else
                        {
                            // Case: Weapon is in the db with different itemPerks, set isAvailable to false for each entry
                            existingWeapon.IsAvailable = false;
                            db.WeaponSales.Update(existingWeapon);
                        }

                    if (addWeapon)
                        db.WeaponSales.Add(vendorItem);
                }
            }

            await db.SaveChangesAsync(stoppingToken);

            foreach (var sale in db.WeaponSales.Where(x => x.VendorId == vendorId))
                if (sale.QueryTime < queryTime && sale.ItemPerks != "[[0]]")
                    sale.IsAvailable = false;

            await db.SaveChangesAsync(stoppingToken);

            success = true;
        }
        catch (Exception e)
        {
            await DiscordTools.SendMessage(DiscordTools.WebhookChannel.Logs,
                $"**Exception in SingleVendorUpdate({vendorId}).**\n\n>>> **{e.GetType()}**: {e.Message}");
            logger.LogError(e, "Exception in {service}", "SingleVendorUpdate");
        }

        return success;
    }
}
