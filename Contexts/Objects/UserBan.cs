// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace API.Contexts.Objects;

public partial class UserBan
{
    public ulong UserId { get; set; }

    public DateTime BanTime { get; set; }

    public string BanReason { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
