using System.Text.Json;
using API.Contexts;
using API.Contexts.Objects;
using API.Responses;

namespace API.Routes;

public static class VendorRoute
{
    public static void MapVendors(this RouteGroupBuilder group)
    {
        group.MapGet("/getWeapons", (DbManager db) =>
        {
            var response = new ListResponse
            {
                ErrorStatus = "Success",
                ErrorCode = ErrorCode.Success,
                Message = "Felicity.Api.Vendor",
                Response = GetWeapons(db.WeaponSales.ToList())
            };

            return TypedResults.Json(response, Common.JsonSerializerOptions);
        });
    }

    private static List<object> GetWeapons(IEnumerable<WeaponSale> dbWeaponSales)
    {
        var weaponSales = new List<WeaponSaleResponse>();

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var weaponSale in dbWeaponSales.OrderByDescending(x => x.QueryTime))
        {
            weaponSales.Add(new WeaponSaleResponse
            {
                IsAvailable = weaponSale.IsAvailable,
                ItemId = weaponSale.ItemId,
                ItemPerks = JsonSerializer.Deserialize<List<List<uint>>>(weaponSale.ItemPerks) ?? [],
                QueryTime = weaponSale.QueryTime,
                RequiredRank = weaponSale.RequiredRank,
                RequiredResets = weaponSale.RequiredResets,
                VendorId = weaponSale.VendorId
            });
        }

        return [weaponSales];
    }
}

