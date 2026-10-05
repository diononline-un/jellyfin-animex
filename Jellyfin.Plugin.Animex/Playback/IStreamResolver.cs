namespace Jellyfin.Plugin.Animex.Playback;

/// <summary>Art der ermittelten Quelle.</summary>
public enum StreamKind
{
    /// <summary>HLS-Playlist.</summary>
    Hls,

    /// <summary>Einzelne Mediendatei.</summary>
    File,

    /// <summary>Nicht ermittelbar.</summary>
    Unresolved
}

/// <summary>Ergebnis einer Auflösung.</summary>
public class ResolvedStream
{
    public StreamKind Kind { get; set; } = StreamKind.Unresolved;

    /// <summary>Wiedergabe-URL, bei HLS die Playlist.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Kennung der Folge beim Anbieter, soweit ermittelt.</summary>
    public string? EpisodeId { get; set; }

    /// <summary>Verweis auf den Player, der die Folge abspielt. Nützlich für Meldungen und Prüfungen.</summary>
    public string? PlayerUrl { get; set; }

    /// <summary>Untertitelspuren, soweit bekannt.</summary>
    public List<string> SubtitleTracks { get; set; } = new();

    /// <summary>Meldung, falls nur teilweise aufgelöst wurde.</summary>
    public string? Note { get; set; }

    public bool IsPlayable => Kind != StreamKind.Unresolved && !string.IsNullOrWhiteSpace(Url);

    public static ResolvedStream Unresolved(string note) => new() { Kind = StreamKind.Unresolved, Note = note };
}

/// <summary>Auftrag an einen Auflöser.</summary>
public class ResolveRequest
{
    public string AnimexId { get; set; } = string.Empty;

    public int AniListId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int EpisodeNumber { get; set; }

    /// <summary>"sub" oder "dub".</summary>
    public string Language { get; set; } = "sub";

    public string ProviderId { get; set; } = "anmx";

    public string PlayerBaseUrl { get; set; } = "https://plyr.animex.one";

    /// <summary>Adresse der Animex-Weboberfläche, für den Verweis auf die Serienseite.</summary>
    public string SiteBaseUrl { get; set; } = "https://animex.one";
}

/// <summary>Schnittstelle, über die Katalog und Bibliothek eine Wiedergabe-URL beziehen.</summary>
public interface IStreamResolver
{
    /// <summary>Name des Auflösers, für Bericht und Konfiguration.</summary>
    string Name { get; }

    /// <summary>Ermittelt die Wiedergabe-URL.</summary>
    Task<ResolvedStream> ResolveAsync(ResolveRequest request, CancellationToken cancellationToken);
}
