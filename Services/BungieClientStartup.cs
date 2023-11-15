using System.Diagnostics;
using API.Util;
using DotNetBungieAPI.Service.Abstractions;

namespace API.Services;

public class BungieClientStartupService(IBungieClient bungieClient,
        ILogger<BungieClientStartupService> logger)
    : BackgroundService
{
    private bool _isUpdating;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var initStopwatch = Stopwatch.StartNew();
            await bungieClient.DefinitionProvider.Initialize();
            initStopwatch.Stop();

            logger.LogInformation("Finished reading definitions ({Time} ms)",
                initStopwatch.ElapsedMilliseconds);

            var currentManifest = await bungieClient.DefinitionProvider.GetCurrentManifest();
            Variables.ManifestVersion = currentManifest.Version;

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(DateTimeExtensions.GetRoundTimeSpan(5), stoppingToken);
                await UpdateChecker();
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception in BungieClientStartupService");
        }
    }

    private async Task UpdateChecker()
    {
        const string serviceName = "ManifestUpdater";

        if (!await BungieTools.IsApiUp(bungieClient))
        {
            logger.LogWarning("[{service}] check failed: Bungie API is down", serviceName);
            return;
        }

        try
        {
            var hasUpdates = await bungieClient.DefinitionProvider.CheckForUpdates();
            if (hasUpdates && !_isUpdating)
            {
                _isUpdating = true;

                logger.LogInformation("[{service}] updating definitions...", serviceName);
                await DiscordTools.SendMessage(DiscordTools.WebhookChannel.Logs,
                    $"[{serviceName}] updating definitions...");

                var updateStopwatch = Stopwatch.StartNew();

                await bungieClient.DefinitionProvider.Update();
                var manifest = await bungieClient.ApiAccess.Destiny2.GetDestinyManifest();
                await bungieClient.DefinitionProvider.ChangeManifestVersion(manifest.Response.Version);

                updateStopwatch.Stop();

                logger.LogInformation("[{service}] finished updating definitions ({Time} ms)",
                    serviceName, updateStopwatch.ElapsedMilliseconds);

                await DiscordTools.SendMessage(DiscordTools.WebhookChannel.Logs,
                    $"[{serviceName}] finished updating definitions ({updateStopwatch.ElapsedMilliseconds} ms)\n" +
                    $"New version: {manifest.Response.Version}");

                Variables.ManifestVersion = manifest.Response.Version;
                _isUpdating = false;
            }
            else
            {
                logger.LogDebug("[{service}] no updates found", serviceName);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception in {service}", serviceName);
        }
    }
}
