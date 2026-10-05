using Jellyfin.Plugin.Animex.Catalogue;
using Jellyfin.Plugin.Animex.Configuration;
using Jellyfin.Plugin.Animex.Library;
using Jellyfin.Plugin.Animex.Playback;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Animex.Sync;

/// <summary>
/// Ablauf eines Synchronisationslaufs: Katalog lesen, Metadaten anreichern, Folgen auflösen,
/// Bibliotheksdateien schreiben, Bestand pflegen.
/// </summary>
public class CatalogueSync
{
    private readonly AnimexCatalogueClient _catalogue;
    private readonly AniListClient _aniList;
    private readonly IStreamResolver _resolver;

    public CatalogueSync(AnimexCatalogueClient catalogue, AniListClient aniList, IStreamResolver resolver)
    {
        _catalogue = catalogue;
        _aniList = aniList;
        _resolver = resolver;
    }

    public async Task<SyncReport> RunAsync(
        PluginConfiguration config,
        string serverBaseUrl,
        ILogger logger,
        Action<double, string>? progress,
        CancellationToken cancellationToken)
    {
        var report = new SyncReport { DryRun = config.DryRun };
        var libraryPath = config.LibraryPath;

        if (string.IsNullOrWhiteSpace(libraryPath))
        {
            report.AbortReason = "Kein Bibliothekspfad konfiguriert.";
            report.AddError(report.AbortReason);
            report.FinishedAt = DateTimeOffset.Now;
            return report;
        }

        Directory.CreateDirectory(libraryPath);

        var ledgerPath = Path.Combine(libraryPath, config.LedgerFileName);
        var ledger = LibraryLedger.Load(ledgerPath);
        var writer = new LibraryWriter(config, logger);

        if (config.ProtectExistingLibraries)
        {
            var known = ledger.Titles.Select(t => t.FolderName).ToList();
            if (writer.ContainsForeignLibrary(libraryPath, known))
            {
                report.AbortReason = "Im Bibliothekspfad liegen bereits fremde Medien. Lauf abgebrochen, um sie nicht zu vermischen.";
                report.AddError(report.AbortReason);
                report.FinishedAt = DateTimeOffset.Now;
                return report;
            }
        }

        IReadOnlyList<AnimeEntry> entries;
        try
        {
            entries = await _catalogue.FetchAsync(config, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            report.AbortReason = $"Katalog nicht lesbar: {ex.Message}";
            report.AddError(report.AbortReason);
            report.FinishedAt = DateTimeOffset.Now;
            return report;
        }

        report.CatalogueSize = entries.Count;
        logger.LogInformation("Katalog enthält {Count} Titel", entries.Count);

        var index = 0;
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            index++;
            progress?.Invoke((double)index / Math.Max(1, entries.Count), entry.Title);

            try
            {
                await SyncTitleAsync(config, serverBaseUrl, entry, ledger, writer, report, logger, cancellationToken)
                    .ConfigureAwait(false);
                report.TitlesProcessed++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException)
            {
                report.AddError($"{entry.Title}: {ex.Message}");
                logger.LogWarning(ex, "Titel {Title} konnte nicht verarbeitet werden", entry.Title);
            }
        }

        // Verwaiste Titel: im Katalog nicht mehr vorhanden.
        foreach (var title in ledger.Titles.Where(t => !t.SeenInCatalogue).ToList())
        {
            report.OrphansFound++;
            if (config.DeleteOrphans && !config.DryRun)
            {
                var removed = writer.RemoveTitle(libraryPath, title);
                report.OrphansDeleted += removed;
                ledger.Titles.Remove(title);
                logger.LogInformation("Verwaisten Titel entfernt: {Title} ({Count} Folgen)", title.Title, removed);
            }
            else
            {
                report.AddNote($"Verwaist: {title.Title} [{title.AnimexId}]");
            }
        }

        ledger.LastRun = DateTimeOffset.Now;
        if (!config.DryRun)
        {
            ledger.Save();
        }

        report.FinishedAt = DateTimeOffset.Now;
        return report;
    }

    private async Task SyncTitleAsync(
        PluginConfiguration config,
        string serverBaseUrl,
        AnimeEntry entry,
        LibraryLedger ledger,
        LibraryWriter writer,
        SyncReport report,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var folderName = LibraryWriter.FolderNameFor(entry);
        var ledgerTitle = ledger.GetOrCreate(entry, folderName);
        var languages = LanguagesFor(config, entry);
        var aniListMetadata = await _aniList.FetchAsync(config, entry.AniListId, cancellationToken).ConfigureAwait(false);

        if (config.DryRun)
        {
            var episodes = EpisodesFor(config, entry, languages.Count, out _);
            report.EpisodesSkipped += episodes;
            report.AddNote($"Trockenlauf: {entry.Title} [{entry.AnimexId}] mit {episodes} Folgen, Fassungen: {string.Join("/", languages)}");
            return;
        }

        var folder = writer.EnsureSeriesFolder(config.LibraryPath, entry);
        writer.WriteSeriesMetadata(folder, entry, aniListMetadata);

        var episodeCount = EpisodesFor(config, entry, languages.Count, out var availableLanguages);
        var languageList = availableLanguages.Count > 0 ? availableLanguages : languages;

        foreach (var language in languageList)
        {
            for (var episode = 1; episode <= episodeCount; episode++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var request = new ResolveRequest
                {
                    AnimexId = entry.AnimexId,
                    AniListId = entry.AniListId,
                    Title = entry.Title,
                    EpisodeNumber = episode,
                    Language = language,
                    ProviderId = config.ProviderId,
                    PlayerBaseUrl = config.PlayerBaseUrl
                };

                var existing = ledgerTitle.Episodes.FirstOrDefault(e => e.Number == episode && e.Language == language);
                if (existing is not null && !string.IsNullOrWhiteSpace(existing.StreamUrl))
                {
                    report.EpisodesSkipped++;
                    continue;
                }

                var resolved = await _resolver.ResolveAsync(request, cancellationToken).ConfigureAwait(false);

                // In der Verweisdatei steht die feste Plugin-Adresse. Sie bleibt gültig, auch wenn
                // die ermittelte Wiedergabe-Adresse ein zeitlich begrenztes Merkmal trägt.
                var reference = LibraryWriter.StreamReference(serverBaseUrl, entry.AnimexId, episode, language);
                var fileName = writer.WriteEpisode(
                    folder,
                    entry,
                    episode,
                    language,
                    languageList.Count,
                    reference,
                    resolved.Note);

                if (!resolved.IsPlayable)
                {
                    report.ResolveFailures++;
                    report.AddNote($"{entry.Title} Folge {episode} ({language}): {resolved.Note}");
                }

                var ledgerEpisode = existing ?? new LedgerEpisode { Number = episode, Language = language };
                ledgerEpisode.FileName = fileName;
                ledgerEpisode.StreamUrl = resolved.IsPlayable ? resolved.Url : string.Empty;
                ledgerEpisode.ResolvedAt = resolved.IsPlayable ? DateTimeOffset.Now : null;
                ledgerEpisode.Note = resolved.Note;
                if (existing is null)
                {
                    ledgerTitle.Episodes.Add(ledgerEpisode);
                }

                report.EpisodesWritten++;

                // Bestand regelmäßig sichern, damit ein Abbruch nicht alles verliert.
                if (report.EpisodesWritten % 25 == 0)
                {
                    ledger.Save();
                }
            }
        }

        logger.LogInformation("Titel fertig: {Title} ({Count} Folgen)", entry.Title, report.EpisodesWritten);
    }

    /// <summary>Welche Fassungen geschrieben werden.</summary>
    public static List<string> LanguagesFor(PluginConfiguration config, AnimeEntry entry)
    {
        var mode = config.AudioMode.ToLowerInvariant();
        var hasSub = entry.SubCount > 0 || entry.EpisodeCount > 0;
        var hasDub = entry.DubCount > 0;

        return mode switch
        {
            "sub" => new List<string> { "sub" },
            "dub" => new List<string> { "dub" },
            _ => BuildBoth(hasSub, hasDub)
        };

        static List<string> BuildBoth(bool hasSub, bool hasDub)
        {
            if (hasSub && hasDub)
            {
                return new List<string> { "sub", "dub" };
            }

            return hasDub && !hasSub ? new List<string> { "dub" } : new List<string> { "sub" };
        }
    }

    /// <summary>Anzahl der Folgen und die tatsächlich vorhandenen Fassungen.</summary>
    public static int EpisodesFor(PluginConfiguration config, AnimeEntry entry, int languageCount, out List<string> availableLanguages)
    {
        availableLanguages = new List<string>();
        var mode = config.AudioMode.ToLowerInvariant();

        var count = entry.EpisodeCount > 0
            ? entry.EpisodeCount
            : Math.Max(Math.Max(entry.SubCount, entry.DubCount), 0);

        if (count <= 0)
        {
            return 0;
        }

        var sub = entry.SubCount > 0 ? Math.Min(entry.SubCount, count) : count;
        var dub = entry.DubCount;

        switch (mode)
        {
            case "sub":
                availableLanguages.Add("sub");
                return sub;
            case "dub":
                if (dub > 0)
                {
                    availableLanguages.Add("dub");
                    return Math.Min(dub, count);
                }

                availableLanguages.Add("sub");
                return sub;
            default:
                availableLanguages.Add("sub");
                if (dub > 0)
                {
                    availableLanguages.Add("dub");
                }

                return count;
        }
    }
}
