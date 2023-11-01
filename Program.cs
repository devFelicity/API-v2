namespace API;

public abstract class Program
{
    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");
        app.MapGet("/health", () => Results.Ok());

        app.Run();
    }
}