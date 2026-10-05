using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.Animex.Playback;

/// <summary>Bildet die Adressen rund um eine Folge ab.</summary>
public static partial class AnimexUrls
{
    /// <summary>Serienseite, aus der die Verweisadresse gelesen wird.</summary>
    public static string SeriesPage(ResolveRequest request)
        => $"{Trim(request.SiteBaseUrl)}/anime/{Uri.EscapeDataString(request.AnimexId)}";

    /// <summary>Fällt die Kennung der Serie aus. Animex führt in der Adresse "{titel}-{id}".</summary>
    public static string SeriesKey(string animexId, string title)
    {
        var slug = Slugify(title);
        return string.IsNullOrEmpty(slug) ? animexId : $"{slug}-{animexId}";
    }

    /// <summary>Verweis auf eine Folge in der Weboberfläche.</summary>
    public static string WatchUrl(ResolveRequest request, int episodeNumber)
        => $"{Trim(request.SiteBaseUrl)}/watch/{SeriesKey(request.AnimexId, request.Title)}-episode-{episodeNumber}";

    /// <summary>Player-Adresse eines Anbieters für die Hauswiedergabe.</summary>
    public static string PlayerUrl(ResolveRequest request, string? episodeId, int episodeNumber)
        => $"{Trim(request.PlayerBaseUrl)}/e/{Uri.EscapeDataString(episodeId ?? request.AniListId.ToString())}/{episodeNumber}"
           + $"?lang={Uri.EscapeDataString(request.Language)}&s={Uri.EscapeDataString(request.ProviderId)}";

    /// <summary>Schnittstelle des Players, die die Kennung der Folge liefert.</summary>
    public static string PlayerEpisodeApi(ResolveRequest request, int aniListId, int episodeNumber)
        => $"{Trim(request.PlayerBaseUrl)}/api/anmx/{aniListId}/{episodeNumber}";

    /// <summary>Adresse der Einbettung eines Fremdanbieters.</summary>
    public static string ProviderStreamUrl(string providerBaseUrl, string episodeId, int episodeNumber, string language)
        => $"{Trim(providerBaseUrl)}/stream/ani/{Uri.EscapeDataString(episodeId)}/{episodeNumber}/{Uri.EscapeDataString(language)}";

    /// <summary>Liest die Kennung der Folge aus der Antwort der Player-Schnittstelle.</summary>
    public static string? ParseEpisodeId(string json)
    {
        var match = EpisodeIdRegex().Match(json);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>Liest die Bewertung der Tonspuren (sub, dub, both) aus der Antwort der Player-Schnittstelle.</summary>
    public static string? ParseAudioLayout(string json)
    {
        var match = AudioLayoutRegex().Match(json);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>Prüft, ob die Antwort eine Playlist beschreibt.</summary>
    public static bool LooksLikeHls(string body)
        => body.Contains("#EXTM3U", StringComparison.OrdinalIgnoreCase);

    /// <summary>Liest die erste Playlist-Adresse aus einer Wiedergabe-Playlist oder aus Text.</summary>
    public static string? FindPlaylistUrl(string text)
    {
        // Antworten der Gegenstelle maskieren Schrägstriche teils als \/ .
        var normalized = text.Replace("\\/", "/", StringComparison.Ordinal);
        var match = PlaylistRegex().Match(normalized);
        return match.Success ? match.Value : null;
    }

    private static string Trim(string value) => value.TrimEnd('/');

    private static string Slugify(string value)
    {
        var lowered = value.ToLowerInvariant();
        var builder = new System.Text.StringBuilder(lowered.Length);
        var lastWasDash = false;
        foreach (var character in lowered)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                lastWasDash = false;
            }
            else if (!lastWasDash && builder.Length > 0)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    [GeneratedRegex("\"episodeId\"\\s*:\\s*\"?(?<id>[0-9]+)\"?", RegexOptions.IgnoreCase)]
    private static partial Regex EpisodeIdRegex();

    [GeneratedRegex("\"audioLayout\"\\s*:\\s*\"(?<layout>[a-zA-Z]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex AudioLayoutRegex();

    [GeneratedRegex("https?://[^\"'\\s<>]*?(?:master\\.(?:txt|m3u8)|index\\.m3u8|\\.m3u8)[^\"'\\s<>]*", RegexOptions.IgnoreCase)]
    private static partial Regex PlaylistRegex();
}
