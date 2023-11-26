// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace API.Responses;

public class WeaponSaleResponse
{
    public long VendorId { get; set; }

    public DateTime QueryTime { get; set; }

    public long ItemId { get; set; }

    public List<List<uint>> ItemPerks { get; set; } = [];

    public int RequiredRank { get; set; }

    public int RequiredResets { get; set; }

    public bool IsAvailable { get; set; }
}
