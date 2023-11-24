// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace API.Contexts.Objects;

public partial class WeaponSale
{
    public long VendorId { get; set; }

    public DateTime QueryTime { get; set; }

    public long ItemId { get; set; }

    public List<long> ItemPerks { get; set; } = [];

    public int RequiredRank { get; set; }

    public int RequiredResets { get; set; }

    public int Id { get; set; }
}
