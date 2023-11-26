namespace API;

public abstract class Variables
{
    public static Environment Environment { get; set; } = Environment.Development;
    public static string? ManifestVersion { get; set; }
    public static string? SecurityKey { get; set; }
    public const ulong OwnerId = 684854397871849482;
}

public enum Environment
{
    Development,
    Production
}
