using DotNetBungieAPI.Service.Abstractions;

namespace API.Services;

public class BungieClientStartupService(IBungieClient bungieClient,
        ILogger<BungieClientStartupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await bungieClient.DefinitionProvider.Initialize();
            // await bungieClient.DefinitionProvider.ReadToRepository(bungieClient.Repository);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception in BungieClientStartupService");
        }
    }
}