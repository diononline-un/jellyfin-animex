using System.Net;
using System.Text;
using Jellyfin.Plugin.Animex.Catalogue;
using Jellyfin.Plugin.Animex.Configuration;
using Jellyfin.Plugin.Animex.Playback;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Animex.Api;

/// <summary>
/// Wiedergabe-Endpunkt des Plugins. In der Verweisdatei steht diese Adresse, nicht die
/// Adresse des Anbieters. Damit bleibt der Bibliothekseintrag gültig, auch wenn die
/// ermittelte Wiedergabe-Adresse nur zeitlich begrenzt gültig ist: beim Abspielen wird
/// jedes Mal neu aufgelöst.
///
/// Zusätzlich dient der Endpunkt als Weiterleitung für Playlist- und Segmentadressen,
/// damit der Client keine fremden Herkunftsbeschränkungen überwinden muss.
/// </summary>
[ApiController]
[Route("Animex")]
public class PlaybackController : ControllerBase
{
    private readonly AnimexCatalogueClient _catalogue;
    private readonly AniListClient _aniList;
    private readonly IStreamResolver _resolver;
    private readonly ILogger<PlaybackController> _logger;

    public PlaybackController(
        AnimexCatalogueClient catalogue,
        AniListClient aniList,
        IStreamResolver resolver,
        ILogger<PlaybackController> logger)
    {
        _catalogue = catalogue;
        _aniList = aniList;
        _resolver = resolver;
        _logger = logger;
    }

    /// <summary>Liefert die Wiedergabe-Playlist einer Folge.</summary>
    [HttpGet("Playback/{animexId}/{episode:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Playback(
        string animexId,
        int episode,
        [FromQuery] string? language,
        [FromQuery] string? title,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        if (string.IsNullOrWhiteSpace(config.LibraryPath))
        {
            return StatusCode(503, "Plugin ist nicht konfiguriert.");
        }

        var entry = await FindEntryAsync(config, animexId, cancellationToken).ConfigureAwait(false);
        if (entry is null)
        {
            return NotFound($"Titel {animexId} nicht im Katalog gefunden.");
        }

        var request = new ResolveRequest
        {
            AnimexId = animexId,
            AniListId = entry.AniListId,
            Title = entry.Title,
            EpisodeNumber = episode,
            Language = string.IsNullOrWhiteSpace(language) ? "sub" : language!,
            ProviderId = config.ProviderId,
            PlayerBaseUrl = config.PlayerBaseUrl
        };

        var resolved = await _resolver.ResolveAsync(request, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsPlayable)
        {
            _logger.LogWarning("Folge nicht auflösbar: {Title} {Episode} ({Note})", entry.Title, episode, resolved.Note);
            return StatusCode(503, resolved.Note ?? "Quelle derzeit nicht verfügbar.");
        }

        // Bei einer Einzeldatei direkt weiterleiten.
        if (resolved.Kind == StreamKind.File)
        {
            return Redirect(resolved.Url);
        }

        // Bei einer Playlist den Inhalt holen und die Verweise auf den eigenen Endpunkt umschreiben.
        var playlist = await FetchTextAsync(resolved.Url, cancellationToken).ConfigureAwait(false);
        if (playlist is null)
        {
            return Redirect(resolved.Url);
        }

        var baseUri = new Uri(resolved.Url);
        var rewritten = RewritePlaylist(playlist, baseUri);
        return Content(rewritten, "application/vnd.apple.mpegurl", Encoding.UTF8);
    }

    /// <summary>Leitet Playlist- und Segmentadressen weiter, auf die erlaubten Wirte begrenzt.</summary>
    [HttpGet("Proxy")]
    [HttpGet("Proxy/playlist.m3u8")]
    [HttpGet("Proxy/segment.ts")]
    [AllowAnonymous]
    public async Task<IActionResult> Proxy([FromQuery] string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var target))
        {
            return BadRequest("Ungültige Adresse.");
        }

        if (!IsAllowedHost(target))
        {
            _logger.LogWarning("Weiterleitung verweigert für {Host}", target.Host);
            return StatusCode(403, "Adresse ist nicht freigegeben.");
        }

        var client = HttpClients.Shared;
        using var request = new HttpRequestMessage(HttpMethod.Get, target);
        request.Headers.Referrer = new Uri("https://plyr.animex.one/");
        request.Headers.TryAddWithoutValidation("Origin", "https://plyr.animex.one");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (Request.Path.Value?.EndsWith("/segment.ts", StringComparison.OrdinalIgnoreCase) == true)
        {
            contentType = "video/mp2t";
        }

        // Manche Playlists heißen .txt und werden als text/plain ausgeliefert. Deshalb am Inhalt
        // erkennen, nicht nur am Dateinamen oder Inhaltstyp.
        var isPlaylist = bytes.Length >= 7
            && string.Equals(Encoding.ASCII.GetString(bytes, 0, 7), "#EXTM3U", StringComparison.Ordinal);

        if (isPlaylist)
        {
            _logger.LogInformation("Proxy liefert Playlist für {Url}", target);
            var text = Encoding.UTF8.GetString(bytes);
            return Content(RewritePlaylist(text, target), "application/vnd.apple.mpegurl", Encoding.UTF8);
        }

        _logger.LogDebug("Proxy liefert Binärdaten für {Url}", target);
        return File(bytes, contentType);
    }

    /// <summary>Schreibt relative Verweise einer Playlist auf den eigenen Endpunkt um.</summary>
    private string RewritePlaylist(string playlist, Uri baseUri)
    {
        var builder = new StringBuilder(playlist.Length + 256);
        foreach (var rawLine in playlist.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith('#'))
            {
                // Attribute wie URI="..." innerhalb einer Zeile werden ebenfalls umgeschrieben.
                builder.AppendLine(RewriteAttributes(line, baseUri));
                continue;
            }

            builder.AppendLine(BuildProxyUrl(baseUri, line));
        }

        return builder.ToString();
    }

    private string RewriteAttributes(string line, Uri baseUri)
    {
        const string marker = "URI=\"";
        var index = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return line;
        }

        var start = index + marker.Length;
        var end = line.IndexOf('"', start);
        if (end < 0)
        {
            return line;
        }

        var inner = line[start..end];
        var replaced = BuildProxyUrl(baseUri, inner);
        return string.Concat(line.AsSpan(0, start), replaced, line.AsSpan(end));
    }

    private string BuildProxyUrl(Uri baseUri, string reference)
    {
        // Manche Playlists des Anbieters bezeichnen Segmente als file:///i/... . Das ist kein
        // lokaler Pfad, sondern ein absoluter Pfad auf demselben Wirt wie die Playlist.
        if (reference.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
        {
            reference = reference["file://".Length..];
        }

        var absolute = Uri.TryCreate(reference, UriKind.Absolute, out var parsed)
            && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps)
                ? parsed
                : new Uri(baseUri, reference);
        var route = reference.Contains(".m3u8", StringComparison.OrdinalIgnoreCase)
            || reference.Contains(".txt", StringComparison.OrdinalIgnoreCase)
                ? "Proxy/playlist.m3u8"
                : "Proxy/segment.ts";
        var extension = route.EndsWith("m3u8", StringComparison.Ordinal) ? ".m3u8" : ".ts";
        return $"/Animex/{route}?url={Uri.EscapeDataString(absolute.ToString())}&ext={extension}";
    }

    private static bool IsAllowedHost(Uri target)
    {
        // Nur die Wirte, die Animex selbst für Wiedergabe und Untertitel benutzt.
        string[] allowedSuffixes =
        {
            ".bcdn2.se", ".anicdn.se", ".acscdn.com", ".vidcloud.se", ".animex.one",
            ".megaplay.buzz", ".serveproxy.com", ".akamaihd.net", ".aniwatchtv.site", ".animeapps.top",
            ".nexabloom.top", ".dramahot.top", ".24stream.xyz"
        };

        if (target.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var host = target.Host;
        return allowedSuffixes.Any(suffix => host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<AnimeEntry?> FindEntryAsync(PluginConfiguration config, string animexId, CancellationToken cancellationToken)
    {
        // Einzelabfrage über den Kennungsfilter. Vorher lief hier eine Seitenschleife über den
        // gesamten Katalog, die bei jedem Abspielen hunderte Anfragen auslöste.
        try
        {
            return await _catalogue.FetchByIdAsync(animexId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Katalog für die Wiedergabe nicht lesbar");
            return null;
        }
    }

    private async Task<string?> FetchTextAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await HttpClients.Shared.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Playlist nicht abrufbar: {Url}", url);
            return null;
        }
    }
}

/// <summary>Gemeinsame HTTP-Bausteine des Plugins.</summary>
public static class HttpClients
{
    private static readonly Lazy<HttpClient> LazyClient = new(() =>
    {
        var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All, AllowAutoRedirect = true };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(45) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
        return client;
    });

    /// <summary>Geteilter Client für alle Anfragen an die Gegenstellen.</summary>
    public static HttpClient Shared => LazyClient.Value;
}
