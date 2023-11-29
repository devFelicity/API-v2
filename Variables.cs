namespace API;

public abstract class Variables
{
    public const ulong OwnerId = 684854397871849482;
    public const ulong BotId = 0;
    public static Environment Environment { get; set; } = Environment.Development;
    public static string? ManifestVersion { get; set; }
    public static string? SecurityKey { get; set; }
    public static DateTime? StartTime { get; set; }
}

public enum Environment
{
    Development,
    Production
}
