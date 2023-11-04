using API.Contexts;
using API.Services;
using DotNetBungieAPI;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Definitions.InventoryItems;
using DotNetBungieAPI.Service.Abstractions;
using Marvin.DefinitionProvider.Postgresql;
using Serilog;
using Serilog.Events;

namespace API;

public abstract class Program
{
    public static void Main()
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
#endif
            .WriteTo.Console()
            .WriteTo.File("Logs/latest-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14,
                restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        try
        {
            EnsureDirectoryExists("Logs");
            EnsureDirectoryExists("Data");

            var builder = WebApplication.CreateBuilder();
            builder.Host.UseSerilog();
            builder.Services.AddDbContext<DbManager>();
            builder.Services
                .UseBungieApiClient(bungieClientBuilder =>
                {
                    bungieClientBuilder.ClientConfiguration.ApiKey = builder.Configuration["Bungie:ApiKey"] ??
                                                                     throw new Exception("API key not configured.");

                    bungieClientBuilder.ClientConfiguration.UsedLocales.Add(BungieLocales.EN);
                    bungieClientBuilder.ClientConfiguration.TryFetchDefinitionsFromProvider = true;

                    bungieClientBuilder.DefinitionProvider.UsePostgresqlDefinitionProvider(provider =>
                    {
                        provider.ConnectionString = builder.Configuration.GetConnectionString("PostgreSQL") ??
                                                    throw new Exception("Connection string not configured.");

                        provider.DefinitionsToLoad.AddRange(new[]
                        {
                            DefinitionsEnum.DestinyActivityDefinition,
                            DefinitionsEnum.DestinyActivityModeDefinition,
                            DefinitionsEnum.DestinyActivityTypeDefinition,
                            DefinitionsEnum.DestinyCollectibleDefinition,
                            DefinitionsEnum.DestinyInventoryItemDefinition,
                            DefinitionsEnum.DestinyMetricDefinition,
                            DefinitionsEnum.DestinyObjectiveDefinition,
                            DefinitionsEnum.DestinyRecordDefinition,
                            DefinitionsEnum.DestinyVendorDefinition
                        });
                        provider.AutoUpdateOnStartup = false;
                        provider.CleanUpOldManifestsAfterUpdate = false;
                    });
                })
                .AddHostedService<BungieClientStartupService>();

            var app = builder.Build();

            app.UseSerilogRequestLogging();

            app.MapGet("/", () => "Hello World!");

            app.MapGroup("/users")
                .MapGet("/getUser", (DbManager db) => Task.FromResult(TypedResults.Ok(db.Users.First())));

            app.MapGet("/invItem", (IBungieClient bungieClient) =>
            {
                if (bungieClient.Repository.TryGetDestinyDefinition<DestinyInventoryItemDefinition>(343863063,
                        out var def))
                {
                    return Task.FromResult(TypedResults.Json(def.ToString()));
                }
                
                return Task.FromResult(TypedResults.Json("manifest query failed"));
            });

            app.MapGet("/health", () => Results.Ok());

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void EnsureDirectoryExists(string dirName)
    {
        if (!Directory.Exists(dirName))
            Directory.CreateDirectory(dirName);
    }
}