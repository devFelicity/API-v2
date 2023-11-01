using DotNetBungieAPI.Service.Abstractions;
// ReSharper disable ClassNeverInstantiated.Global

namespace Marvin.DefinitionProvider.Postgresql.Tests.Fixtures;

public class DefinitionProviderFixture : IAsyncLifetime
{
    public DefinitionProviderFixture(IDefinitionProvider definitionProvider)
    {
        DefinitionProvider = definitionProvider;
    }

    public IDefinitionProvider DefinitionProvider { get; }

    public async Task InitializeAsync()
    {
        await DefinitionProvider.Initialize();
    }

    public async Task DisposeAsync()
    {
        await DefinitionProvider.DisposeAsync();
    }
}