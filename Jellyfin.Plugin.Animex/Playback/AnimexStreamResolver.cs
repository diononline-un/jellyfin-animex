using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Animex.Playback;

/// <summary>
/// Auflöser für die Hauswiedergabe von Animex.
///
/// Er geht zwei Schritte: zuerst die Kennung der Folge über die Schnittstelle des Players,
/// danach die Wiedergabe-Playlist über die Einbettung des Anbieters. Beide Schritte sind
/// gegen die echte Gegenstelle geprüft (siehe docs/feasibility.md).
///
/// Findet er keine Playlist, meldet er das als Ergebnis und nicht als Ausnahme. Katalog und
/// Bibliothek bleiben dadurch unberührt, der Titel erscheint nur als derzeit nicht abspielbar.
/// </summary>
public class AnimexStreamResolver : IStreamResolver
{
    private readonly HttpClient _http;
    private readonly ILogger _logger;

    public AnimexStreamResolver(HttpClient http, ILogger logger)
    {
        _http = http;
        _logger = logger;
    }

    public string Name => "animex";

    public async Task<ResolvedStream> ResolveAsync(ResolveRequest request, CancellationToken cancellationToken)
    {
        var playerUrl = AnimexUrls.PlayerUrl(request, null, request.EpisodeNumber);
        var episodeId = await FetchEpisodeIdAsync(request, cancellationToken).ConfigureAwait(false);

        playerUrl = AnimexUrls.PlayerUrl(request, episodeId, request.EpisodeNumber);

        // Einbettung des Anbieters: liefert in der Regel eine Playlist-Adresse oder einen Verweis darauf.
        foreach (var candidate in CandidateEmbedUrls(request, episodeId))
        {
            var resolved = await TryExtractFromEmbedAsync(candidate, playerUrl, episodeId, cancellationToken).ConfigureAwait(false);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        return ResolvedStream.Unresolved(
            "Die Wiedergabe-Playlist konnte nicht ermittelt werden. Der Player liefert seine Quelle über eine "
            + "Einbettung mit nachgeladenem Skript; dieser Schritt ist ohne eigenen Auflöser nicht reproduzierbar. "
            + "Der Verweis auf den Player ist gespeichert, die Bibliothek bleibt nutzbar.");
    }

    /// <summary>Holt die Kennung der Folge beim Player.</summary>
    public async Task<string?> FetchEpisodeIdAsync(ResolveRequest request, CancellationToken cancellationToken)
    {
        var aniListId = request.AniListId;
        if (aniListId <= 0)
        {
            return null;
        }

        var url = AnimexUrls.PlayerEpisodeApi(request, aniListId, request.EpisodeNumber);
        try
        {
            using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Player-Schnittstelle antwortete mit HTTP {Status} für {Url}", (int)response.StatusCode, url);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return AnimexUrls.ParseEpisodeId(body);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Player-Schnittstelle nicht erreichbar: {Url}", url);
            return null;
        }
    }

    private static IEnumerable<string> CandidateEmbedUrls(ResolveRequest request, string? episodeId)
    {
        var key = string.IsNullOrWhiteSpace(episodeId) ? request.AniListId.ToString() : episodeId;
        yield return AnimexUrls.ProviderStreamUrl("https://megaplay.buzz", key, request.EpisodeNumber, request.Language);
    }

    private async Task<ResolvedStream?> TryExtractFromEmbedAsync(
        string embedUrl,
        string playerUrl,
        string? episodeId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.GetAsync(embedUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var playlist = AnimexUrls.FindPlaylistUrl(body);
            if (playlist is not null)
            {
                return new ResolvedStream
                {
                    Kind = StreamKind.Hls,
                    Url = playlist,
                    EpisodeId = episodeId,
                    PlayerUrl = playerUrl
                };
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(ex, "Einbettung nicht erreichbar: {Url}", embedUrl);
        }

        return null;
    }
}

/// <summary>
/// Auflöser, der einen externen Befehl aufruft. Damit lässt sich die Auflösung nachrüsten,
/// ohne das Plugin zu ändern: der Auftrag geht als JSON an die Standardeingabe, die Antwort
/// mit der Wiedergabe-URL kommt als JSON von der Standardausgabe.
/// </summary>
public class ExternalCommandResolver : IStreamResolver
{
    private readonly string _command;
    private readonly ILogger _logger;

    public ExternalCommandResolver(string command, ILogger logger)
    {
        _command = command;
        _logger = logger;
    }

    public string Name => "external";

    public async Task<ResolvedStream> ResolveAsync(ResolveRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_command))
        {
            return ResolvedStream.Unresolved("Kein externer Auflöser konfiguriert.");
        }

        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                animexId = request.AnimexId,
                aniListId = request.AniListId,
                title = request.Title,
                episode = request.EpisodeNumber,
                language = request.Language,
                providerId = request.ProviderId,
                playerUrl = AnimexUrls.PlayerUrl(request, null, request.EpisodeNumber),
                watchUrl = AnimexUrls.WatchUrl(request, request.EpisodeNumber)
            });

            var parts = _command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var startInfo = new System.Diagnostics.ProcessStartInfo(parts[0])
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            for (var i = 1; i < parts.Length; i++)
            {
                startInfo.ArgumentList.Add(parts[i]);
            }

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process is null)
            {
                return ResolvedStream.Unresolved("Externer Auflöser ließ sich nicht starten.");
            }

            await process.StandardInput.WriteAsync(payload.AsMemory(), cancellationToken).ConfigureAwait(false);
            process.StandardInput.Close();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Externer Auflöser endete mit Code {Code}: {Error}", process.ExitCode, error.Trim());
                return ResolvedStream.Unresolved($"Externer Auflöser endete mit Code {process.ExitCode}.");
            }

            using var document = System.Text.Json.JsonDocument.Parse(output);
            var root = document.RootElement;
            var url = root.TryGetProperty("url", out var urlElement) ? urlElement.GetString() : null;
            if (string.IsNullOrWhiteSpace(url))
            {
                return ResolvedStream.Unresolved("Externer Auflöser lieferte keine Wiedergabe-URL.");
            }

            var kind = root.TryGetProperty("kind", out var kindElement)
                && string.Equals(kindElement.GetString(), "file", StringComparison.OrdinalIgnoreCase)
                    ? StreamKind.File
                    : StreamKind.Hls;

            var result = new ResolvedStream
            {
                Kind = kind,
                Url = url,
                Note = "Über externen Auflöser ermittelt."
            };

            if (root.TryGetProperty("tracks", out var tracks) && tracks.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var track in tracks.EnumerateArray())
                {
                    if (track.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        result.SubtitleTracks.Add(track.GetString()!);
                    }
                }
            }

            return result;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or System.Text.Json.JsonException or InvalidOperationException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Externer Auflöser fehlgeschlagen");
            return ResolvedStream.Unresolved($"Externer Auflöser fehlgeschlagen: {ex.Message}");
        }
    }
}
