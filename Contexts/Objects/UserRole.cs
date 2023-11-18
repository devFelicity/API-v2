// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace API.Contexts.Objects;

public partial class UserRole
{
    public ulong UserId { get; set; }

    public string RoleSlug { get; set; } = null!;

    public virtual Role RoleSlugNavigation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
