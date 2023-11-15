using System.Diagnostics;
using API.Contexts;
using API.Responses;
using API.Routes;
using API.Services;
using API.Util;
using DotNetBungieAPI;
using DotNetBungieAPI.AspNet.Security.OAuth.Providers;
using DotNetBungieAPI.DefinitionProvider.Sqlite;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Applications;
using DotNetBungieAPI.Models.Destiny;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
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
#endif
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
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

            DiscordTools.Initialize(builder.Configuration);

            var dbDataSource = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("PostgreSQL"))
                .Build();
            builder.Services.AddDbContext<DbManager>(options => options.UseNpgsql(dbDataSource));

            builder.Services
                .UseBungieApiClient(bungieBuilder =>
                {
                    bungieBuilder.ClientConfiguration.ApplicationScopes = ApplicationScopes.ReadUserData |
                                                                          ApplicationScopes.ReadBasicUserProfile |
                                                                          ApplicationScopes.ReadDestinyInventoryAndVault |
                                                                          ApplicationScopes.MoveEquipDestinyItems;

                    bungieBuilder.ClientConfiguration
                            .ApiKey = builder.Configuration["Bungie:ApiKey"] ??
                                      throw new Exception("Bungie API Key not configured.");

                    bungieBuilder.ClientConfiguration
                        .ClientId = Convert.ToInt32(builder.Configuration["Bungie:ClientId"]);

                    bungieBuilder.ClientConfiguration
                            .ClientSecret = builder.Configuration["Bungie:ClientSecret"] ??
                                            throw new Exception("Bungie Client Secret not configured.");

                    bungieBuilder.ClientConfiguration.CacheDefinitions = false;

                    bungieBuilder.ClientConfiguration.UsedLocales.AddRange(Enum.GetValues<BungieLocales>());

                    bungieBuilder.ClientConfiguration.TryFetchDefinitionsFromProvider = true;

                    bungieBuilder
                        .DefinitionProvider.UseSqliteDefinitionProvider(definitionProvider =>
                        {
                            definitionProvider.AutoUpdateManifestOnStartup = true;
                            definitionProvider.DeleteOldManifestDataAfterUpdates = false;
                            definitionProvider.FetchLatestManifestOnInitialize = true;
                            definitionProvider.ManifestFolderPath = "Data/Manifest";
                        });

                    bungieBuilder.DotNetBungieApiHttpClient.ConfigureDefaultHttpClient(options =>
                        options.SetRateLimitSettings(190, TimeSpan.FromSeconds(10)));

                    bungieBuilder
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

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = BungieNetAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = BungieNetAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddBungieNet(options =>
                {
                    options.ClientId = builder.Configuration["Bungie:ClientId"]!;
                    options.ApiKey = builder.Configuration["Bungie:ApiKey"]!;
                    options.ClientSecret = builder.Configuration["Bungie:ClientSecret"]!;
                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = oAuthCreatingTicketContext =>
                        {
                            BungieAuthCacheService.TryAddContext(oAuthCreatingTicketContext);
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services
                .AddControllers(options => { options.EnableEndpointRouting = false; })
                .AddJsonOptions(x => { BungieAuthCacheService.Initialize(x.JsonSerializerOptions); });

            builder.Services.AddCors(c =>
            {
                c.AddPolicy("AllowOrigin",
                    options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });

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

            app.MapGroup("/auth").MapAuth();
            app.MapGroup("/manifest").MapManifest();
            app.MapGroup("/user").MapUsers();
            app.MapGroup("/status").MapStatus();

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
