// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace API.Contexts.Objects;

public partial class ArmorSale
{
    public long VendorId { get; set; }

    public long ItemId { get; set; }

    public string? SetName { get; set; }

    public int Class { get; set; }

    public string Stats { get; set; } = null!;

    public int Id { get; set; }
}
