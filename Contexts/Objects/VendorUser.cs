// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace API.Contexts.Objects;

public partial class VendorUser
{
    public long UserId { get; set; }

    public long VendorId { get; set; }

    public int Rank { get; set; }

    public int Resets { get; set; }

    public int Id { get; set; }

    public virtual User User { get; set; } = null!;
}
