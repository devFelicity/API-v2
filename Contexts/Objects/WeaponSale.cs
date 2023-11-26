// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace API.Contexts.Objects;

public partial class WeaponSale
{
    public long VendorId { get; set; }

    public DateTime QueryTime { get; set; }

    public long ItemId { get; set; }

    public string ItemPerks { get; set; } = string.Empty;

    public int RequiredRank { get; set; }

    public int RequiredResets { get; set; }

    public int Id { get; set; }

    public bool IsAvailable { get; set; }
}
