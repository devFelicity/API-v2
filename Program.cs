using System.Diagnostics;
using API.Contexts;
using API.Responses;
using API.Routes;
using API.Services;
using DotNetBungieAPI;
using DotNetBungieAPI.DefinitionProvider.Sqlite;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.Destiny.Definitions.InventoryItems;
using DotNetBungieAPI.Service.Abstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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

        Variables.Environment = Debugger.IsAttached ? Environment.Development : Environment.Production;

        try
        {
            EnsureDirectoryExists("Logs");
            EnsureDirectoryExists("Data");
            EnsureDirectoryExists("Data/Manifest");

            var builder = WebApplication.CreateBuilder();
            builder.Host.UseSerilog();

            var dbDataSource = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("PostgreSQL"))
                .Build();
            builder.Services.AddDbContext<DbManager>(options => options.UseNpgsql(dbDataSource));

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

                    bungieClientBuilder.ClientConfiguration.UsedLocales.Add(BungieLocales.EN);

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
                                // DefinitionsEnum.DestinyActivityModeDefinition,
                                // DefinitionsEnum.DestinyActivityTypeDefinition,
                                DefinitionsEnum.DestinyCollectibleDefinition,
                                DefinitionsEnum.DestinyInventoryItemDefinition,
                                // DefinitionsEnum.DestinyMetricDefinition,
                                // DefinitionsEnum.DestinyObjectiveDefinition,
                                // DefinitionsEnum.DestinyRecordDefinition,
                                DefinitionsEnum.DestinyVendorDefinition
                            };

                            foreach (var defType in Enum.GetValues<DefinitionsEnum>())
                                if (!includeTypes.Contains(defType))
                                    x.IgnoreDefinitionType(defType);
                        });
                })
                .AddHostedService<BungieClientStartupService>();

            var app = builder.Build();

            Logging.LoggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

            app.UseSerilogRequestLogging(x =>
            {
                x.MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.00} ms";
            });

            app.MapGet("/", () =>
            {
                return Variables.Environment switch
                {
                    Environment.Development => Results.Ok("Hello World!"),
                    _ => Results.Redirect("https://tryfelicity.one", true)
                };
            });

            app.MapGet("/health", () => Results.Ok());

            app.MapGroup("/users").MapUsers();
            app.MapGroup("/status").MapStatus();

            app.MapGet("/invItem",
                (IBungieClient bungieClient) => Task.FromResult(
                    bungieClient.TryGetDefinition<DestinyInventoryItemDefinition>(
                        DefinitionHashes.InventoryItems.ExoticEngram_343863063, out var def)
                        ? TypedResults.Json(def.ToString())
                        : TypedResults.Json("manifest query failed")));

            app.MapGet("/pp", async () => TypedResults.Json(await Voluspa.ParallelQuery.GetResponse()));

            app.MapGet("/bestApp", async () =>
            {
                var programs = await Voluspa.ParallelQuery.GetResponse();
                return TypedResults.Json(programs.Response?.ElementAt(1));
            });

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
