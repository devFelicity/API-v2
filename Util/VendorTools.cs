using System.Text.Json;
using API.Contexts;
using API.Contexts.Objects;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Definitions.Vendors;
using DotNetBungieAPI.Service.Abstractions;

namespace API.Util;

public static class VendorTools
{
    public static async Task SingleVendorUpdate(
        IBungieClient bungieClient,
        DbManager db,
        BungieProfile vendorProfile,
        uint vendorId,
        int requiredResets,
        CancellationToken stoppingToken)
    {
        var vendorQuery = await bungieClient.ApiAccess.Destiny2.GetVendor(vendorProfile.DestinyMembershipType,
            vendorProfile.DestinyMembershipId, await vendorProfile.GetLatestCharacter(bungieClient),
            vendorId,
            [
                DestinyComponentType.VendorSales, DestinyComponentType.ItemReusablePlugs
            ], vendorProfile.GetTokenData(), stoppingToken);

        var queryTime = DateTime.UtcNow;
        bungieClient.TryGetDefinition<DestinyVendorDefinition>(vendorId, out var vendor);

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

            var existingWeapon = db.WeaponSales.FirstOrDefault(x => x.ItemId == vendorItem.ItemId);

            if (existingWeapon is { IsAvailable: false })
                continue;

            var addWeapon = true;

            if (existingWeapon != null)
            {
                if (existingWeapon.ItemPerks != vendorItem.ItemPerks)
                {
                    existingWeapon.IsAvailable = false;
                }
                else
                {
                    existingWeapon.QueryTime = queryTime;
                    addWeapon = false;
                }

                db.WeaponSales.Update(existingWeapon);
            }

            if (addWeapon)
                db.WeaponSales.Add(vendorItem);
        }

        await db.SaveChangesAsync(stoppingToken);

        foreach (var sale in db.WeaponSales.Where(x => x.VendorId == vendorId))
            if (sale.QueryTime < queryTime)
                sale.IsAvailable = false;

        await db.SaveChangesAsync(stoppingToken);
    }
}
