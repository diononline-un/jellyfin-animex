using Jellyfin.Plugin.Animex.Api;
using Jellyfin.Plugin.Animex.Catalogue;
using Jellyfin.Plugin.Animex.Configuration;
using Jellyfin.Plugin.Animex.Library;
using Jellyfin.Plugin.Animex.Playback;
using Jellyfin.Plugin.Animex.Sync;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Animex;

/// <summary>Registriert die Dienste des Plugins in der Jellyfin-Instanz.</summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Der geteilte Client mit Browser-Kennung, weil die Gegenstellen einfache Anfragen abweisen.
        serviceCollection.AddSingleton(_ => HttpClients.Shared);

        serviceCollection.AddSingleton(provider => Plugin.Instance?.Configuration ?? new PluginConfiguration());

        serviceCollection.AddSingleton(provider => new AnimexCatalogueClient(
            provider.GetRequiredService<HttpClient>(),
            provider.GetRequiredService<ILogger<AnimexCatalogueClient>>()));

        serviceCollection.AddSingleton(provider => new AniListClient(
            provider.GetRequiredService<HttpClient>(),
            provider.GetRequiredService<ILogger<AniListClient>>()));

        serviceCollection.AddSingleton<IStreamResolver>(provider =>
        {
            var config = provider.GetRequiredService<PluginConfiguration>();
            var logger = provider.GetRequiredService<ILogger<AnimexStreamResolver>>();
            return new AnimexStreamResolver(provider.GetRequiredService<HttpClient>(), logger);
        });

        serviceCollection.AddSingleton(provider => new CatalogueSync(
            provider.GetRequiredService<AnimexCatalogueClient>(),
            provider.GetRequiredService<AniListClient>(),
            provider.GetRequiredService<IStreamResolver>()));

        serviceCollection.AddSingleton(provider => new LibraryWriter(
            provider.GetRequiredService<PluginConfiguration>(),
            provider.GetRequiredService<ILogger<LibraryWriter>>()));

        serviceCollection.AddSingleton(provider => new AnimexSyncTask(
            provider.GetRequiredService<AnimexCatalogueClient>(),
            provider.GetRequiredService<AniListClient>(),
            provider.GetRequiredService<IStreamResolver>(),
            provider.GetRequiredService<ILogger<AnimexSyncTask>>()));
    }
}
