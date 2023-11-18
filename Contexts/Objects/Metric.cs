// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace API.Contexts.Objects;

public partial class Metric
{
    public string Slug { get; set; } = null!;

    public long TotalUse { get; set; }

    public DateTime LastUse { get; set; }
}
