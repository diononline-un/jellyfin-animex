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

        // Die Quellen-Schnittstelle des Players erwartet die Animex-Serienkennung (Slug plus ID),
        // nicht die AniList-Kennung. Sie liefert direkt eine HLS-Adresse; ein Browser ist dafür
        // nicht nötig. Die frühere Einbettung von megaplay.buzz ist nicht mehr erreichbar.
        foreach (var provider in ProviderChain(request.ProviderId))
        {
            var resolved = await TryResolveFromSourcesApiAsync(request, provider, playerUrl, cancellationToken).ConfigureAwait(false);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        return ResolvedStream.Unresolved(
            "Die Wiedergabe-Playlist konnte nicht ermittelt werden. Die Quellen-Schnittstelle des Players "
            + "antwortete für keinen der verfügbaren Anbieter mit einer Playlist. Der Verweis auf den Player "
            + "ist gespeichert, die Bibliothek bleibt nutzbar.");
    }

    private static IEnumerable<string> ProviderChain(string preferred)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(preferred)
            && !string.Equals(preferred, "anmx", StringComparison.OrdinalIgnoreCase)
            && seen.Add(preferred))
        {
            yield return preferred;
        }

        foreach (var provider in new[] { "nero", "yuki", "zuna" })
        {
            if (seen.Add(provider))
            {
                yield return provider;
            }
        }
    }

    private async Task<ResolvedStream?> TryResolveFromSourcesApiAsync(
        ResolveRequest request,
        string provider,
        string playerUrl,
        CancellationToken cancellationToken)
    {
        var seriesKey = string.IsNullOrWhiteSpace(request.AnimexId)
            ? AnimexUrls.SeriesKey(request.AnimexId, request.Title)
            : request.AnimexId;
        var url = AnimexUrls.SourcesApiUrl(seriesKey, request.EpisodeNumber, request.Language, provider);
        try
        {
            using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Quellen-Schnittstelle antwortete mit HTTP {Status} für Anbieter {Provider}", (int)response.StatusCode, provider);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var document = System.Text.Json.JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("sources", out var sources)
                || sources.ValueKind != System.Text.Json.JsonValueKind.Array
                || sources.GetArrayLength() == 0)
            {
                return null;
            }

            var source = sources[0];
            var streamUrl = source.TryGetProperty("url", out var urlElement) ? urlElement.GetString() : null;
            if (string.IsNullOrWhiteSpace(streamUrl))
            {
                return null;
            }

            var mediaType = source.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
            var kind = mediaType is not null && mediaType.Contains("mpegurl", StringComparison.OrdinalIgnoreCase)
                ? StreamKind.Hls
                : StreamKind.File;

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (document.RootElement.TryGetProperty("headers", out var headerElement)
                && headerElement.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var header in headerElement.EnumerateObject())
                {
                    if (header.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        headers[header.Name] = header.Value.GetString() ?? string.Empty;
                    }
                }
            }

            // Quelle vorab prüfen: Ein toter CDN-Link darf nicht als Redirect an Jellyfin gehen,
            // sonst versucht ffmpeg zu transkodieren und scheitert mit einem unklaren Fehler.
            if (kind == StreamKind.Hls)
            {
                using var check = new HttpRequestMessage(HttpMethod.Get, streamUrl);
                foreach (var header in headers)
                {
                    check.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                using var checkResponse = await _http.SendAsync(check, cancellationToken).ConfigureAwait(false);
                if (!checkResponse.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Quelle von {Provider} antwortet mit HTTP {Status}", provider, (int)checkResponse.StatusCode);
                    return null;
                }

                var probe = await checkResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (!probe.TrimStart().StartsWith("#EXTM3U", StringComparison.Ordinal))
                {
                    _logger.LogDebug("Quelle von {Provider} ist keine HLS-Playlist", provider);
                    return null;
                }
            }

            var result = new ResolvedStream
            {
                Kind = kind,
                Url = streamUrl,
                PlayerUrl = playerUrl,
                Note = $"Über Anbieter {provider} ermittelt.",
                Headers = headers
            };

            if (document.RootElement.TryGetProperty("tracks", out var tracks)
                && tracks.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var track in tracks.EnumerateArray())
                {
                    if (track.TryGetProperty("file", out var file) && file.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var value = file.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            result.SubtitleTracks.Add(value);
                        }
                    }
                }
            }

            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogDebug(ex, "Quellen-Schnittstelle nicht erreichbar für Anbieter {Provider}", provider);
            return null;
        }
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
