// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace API.Contexts.Objects;

public partial class User
{
    public ulong Id { get; set; }

    public DateTime? LastCommand { get; set; }

    public bool RegisteredFelicity { get; set; }

    public bool RegisteredLostSector { get; set; }

    public virtual ICollection<BungieProfile> BungieProfiles { get; set; } = new List<BungieProfile>();

    public virtual UserBan? UserBan { get; set; }

    public virtual UserRole? UserRole { get; set; }

    public virtual VendorUser? VendorUser { get; set; }
}
