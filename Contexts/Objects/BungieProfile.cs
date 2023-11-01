// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace API.Contexts.Objects;

public partial class BungieProfile
{
    public long UserId { get; set; }

    public long MembershipId { get; set; }

    public int MembershipType { get; set; }

    public string OauthToken { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;

    public DateTime TokenExpires { get; set; }

    public DateTime RefreshExpires { get; set; }

    public bool NeverExpire { get; set; }

    public virtual User User { get; set; } = null!;
}
