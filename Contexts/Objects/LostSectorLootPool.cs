// ReSharper disable PartialTypeWithSinglePart

namespace API.Contexts.Objects;

public partial class LostSectorLootPool
{
    public int Id { get; set; }

    public List<ulong> ItemIds { get; set; } = new();
}