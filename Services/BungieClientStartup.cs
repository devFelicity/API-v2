using DotNetBungieAPI.Service.Abstractions;
using System.Diagnostics;

namespace API.Services;

public class BungieClientStartupService(IBungieClient bungieClient,
        ILogger<BungieClientStartupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var initStopwatch = Stopwatch.StartNew();

            await bungieClient.DefinitionProvider.Initialize();
            // await bungieClient.DefinitionProvider.ReadToRepository(bungieClient.Repository);

            initStopwatch.Stop();
            logger.LogInformation("Finished reading definitions ({Time} ms)",
                initStopwatch.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception in BungieClientStartupService");
        }
    }
}