﻿// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

using DotNetBungieAPI.Models;

namespace API.Contexts.Objects;

public partial class BungieProfile
{
    public long UserId { get; set; }

    public long MembershipId { get; set; }

    public string OauthToken { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;

    public DateTime TokenExpires { get; set; }

    public DateTime RefreshExpires { get; set; }

    public bool NeverExpire { get; set; }

    public int Id { get; set; }

    public long DestinyMembershipId { get; set; }

    public BungieMembershipType DestinyMembershipType { get; set; }
}
