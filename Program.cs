using API.Contexts;
using API.Services;
using DotNetBungieAPI;
using DotNetBungieAPI.DefinitionProvider.Sqlite;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Definitions.InventoryItems;
using DotNetBungieAPI.Service.Abstractions;
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
            EnsureDirectoryExists("Data/Manifest");

            var builder = WebApplication.CreateBuilder();
            builder.Host.UseSerilog();
            builder.Services.AddDbContext<DbManager>();
            builder.Services
                .UseBungieApiClient(bungieClientBuilder =>
                {
                    bungieClientBuilder.ClientConfiguration.ApiKey = builder.Configuration["Bungie:ApiKey"] ??
                                                                     throw new Exception(
                                                                         "Bungie API Key not configured.");
                    bungieClientBuilder.ClientConfiguration.ClientId =
                        Convert.ToInt32(builder.Configuration["Bungie:ClientId"]);

                    bungieClientBuilder.ClientConfiguration.ClientSecret =
                        builder.Configuration["Bungie:ClientSecret"] ??
                        throw new Exception("Bungie Client Secret not configured.");

                    bungieClientBuilder.ClientConfiguration.CacheDefinitions = false;

                    bungieClientBuilder.ClientConfiguration.UsedLocales.AddRange(Enum.GetValues<BungieLocales>());

                    bungieClientBuilder.ClientConfiguration.TryFetchDefinitionsFromProvider = true;

                    bungieClientBuilder
                        .DefinitionProvider.UseSqliteDefinitionProvider(definitionProvider =>
                        {
                            definitionProvider.AutoUpdateManifestOnStartup = true;
                            definitionProvider.DeleteOldManifestDataAfterUpdates = false;
                            definitionProvider.FetchLatestManifestOnInitialize = true;
                            definitionProvider.ManifestFolderPath = "Data/Manifest";
                        });

                    bungieClientBuilder.DotNetBungieApiHttpClient.ConfigureDefaultHttpClient(options =>
                        options.SetRateLimitSettings(190, TimeSpan.FromSeconds(10)));

                    bungieClientBuilder
                        .DefinitionRepository.ConfigureDefaultRepository(x =>
                        {
                            var includeTypes = new[]
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
                            };

                            foreach (var defType in Enum.GetValues<DefinitionsEnum>())
                                if (!includeTypes.Contains(defType))
                                    x.IgnoreDefinitionType(defType);
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
                if (bungieClient.TryGetDefinition<DestinyInventoryItemDefinition>(343863063,
                        out var def, BungieLocales.FR))
                    return Task.FromResult(TypedResults.Json(def.ToString()));

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