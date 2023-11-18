// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace API.Contexts.Objects;

public partial class LostSectorLootPool
{
    public int Id { get; set; }

    public List<ulong> ItemIds { get; set; } = [];
}
