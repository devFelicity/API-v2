using DotNetBungieAPI.Models.Destiny;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Marvin.DefinitionProvider.Postgresql;

public class PostgresqlDefinitionProviderConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool CleanUpOldManifestsAfterUpdate { get; set; } = true;
    public int MaxAmountOfLeftoverManifests { get; set; } = 1;
    public bool AutoUpdateOnStartup { get; set; }

    public List<DefinitionsEnum> DefinitionsToLoad { get; set; } = new();
}