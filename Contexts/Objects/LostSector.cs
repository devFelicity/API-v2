// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace API.Contexts.Objects;

public partial class LostSector
{
    public long Id { get; set; }

    public int BarrierCount { get; set; }

    public int OverloadCount { get; set; }

    public int UnstopCount { get; set; }

    public int ArcCount { get; set; }

    public int SolarCount { get; set; }

    public int VoidCount { get; set; }
}
