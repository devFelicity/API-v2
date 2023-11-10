namespace API;

public abstract class Variables
{
    public static Environment Environment { get; set; } = Environment.Development;
}

public enum Environment
{
    Development,
    Production
}
