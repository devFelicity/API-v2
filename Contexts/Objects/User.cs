// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace API.Contexts.Objects;

public partial class User
{
    public long Id { get; set; }

    public DateTime? LastCommand { get; set; }

    public bool RegisteredFelicity { get; set; }

    public bool RegisteredLostsector { get; set; }

    public virtual UserBan? UserBan { get; set; }
}
