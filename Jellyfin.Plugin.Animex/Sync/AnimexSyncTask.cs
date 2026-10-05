using Jellyfin.Plugin.Animex.Catalogue;
using Jellyfin.Plugin.Animex.Playback;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Animex.Sync;

/// <summary>Geplanter Task: gleicht die Bibliothek mit dem Animex-Katalog ab.</summary>
public class AnimexSyncTask : IScheduledTask
{
    private readonly AnimexCatalogueClient _catalogue;
    private readonly AniListClient _aniList;
    private readonly IStreamResolver _resolver;
    private readonly ILogger<AnimexSyncTask> _logger;

    public AnimexSyncTask(
        AnimexCatalogueClient catalogue,
        AniListClient aniList,
        IStreamResolver resolver,
        ILogger<AnimexSyncTask> logger)
    {
        _catalogue = catalogue;
        _aniList = aniList;
        _resolver = resolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Animex-Katalog abgleichen";

    /// <inheritdoc />
    public string Key => "AnimexCatalogueSync";

    /// <inheritdoc />
    public string Description =>
        "Liest den Animex-Katalog, reichert Metadaten über AniList an und schreibt je Folge eine Verweisdatei "
        + "in den konfigurierten Bibliotheksordner. Bei aktiviertem Trockenlauf wird nur berichtet.";

    /// <inheritdoc />
    public string Category => "Animex";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.DailyTrigger,
            TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
        };
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var plugin = Plugin.Instance;
        var config = plugin?.Configuration ?? new Configuration.PluginConfiguration();

        var sync = new CatalogueSync(_catalogue, _aniList, _resolver);
        var report = await sync.RunAsync(
            config,
            ServerBaseUrl(config),
            _logger,
            (percent, current) =>
            {
                progress.Report(Math.Clamp(percent * 100, 0, 100));
                _logger.LogDebug("Fortschritt {Percent:F0}% bei {Title}", percent * 100, current);
            },
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("{Summary}", report.Summary());
        foreach (var error in report.Errors.Take(20))
        {
            _logger.LogWarning("Fehler im Lauf: {Error}", error);
        }

        plugin?.SaveReport(report);
        progress.Report(100);
    }

    /// <summary>
    /// Adresse, die in die Verweisdateien geschrieben wird. Standard ist der Platzhalter
    /// <c>{serverAddress}</c>, den Jellyfin beim Lesen durch die tatsächliche Serveradresse
    /// des Clients ersetzt. Damit funktioniert die Bibliothek im Heimnetz und von außen.
    /// </summary>
    private static string ServerBaseUrl(Configuration.PluginConfiguration config)
        => string.IsNullOrWhiteSpace(config.ServerBaseUrl) ? "{serverAddress}" : config.ServerBaseUrl.TrimEnd('/');
}
