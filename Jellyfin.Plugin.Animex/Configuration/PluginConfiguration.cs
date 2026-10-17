using System.Text.Json;
using System.Text.Json.Serialization;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Animex.Configuration;

/// <summary>
/// Einstellungen des Plugins. Wird von Jellyfin als XML neben der Plugin-DLL abgelegt.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Ordner, in den die Verweis- und Metadatendateien geschrieben werden. Leer = kein Schreiben.</summary>
    public string LibraryPath { get; set; } = string.Empty;

    /// <summary>Sprache der Fassung: "sub", "dub" oder "both".</summary>
    public string AudioMode { get; set; } = "sub";

    /// <summary>Obergrenze der Titel je Lauf. 0 = keine Grenze.</summary>
    public int MaxTitles { get; set; } = 200;

    /// <summary>Mindestbewertung nach AniList-Skala (0 bis 100). 0 = keine Bedingung.</summary>
    public int MinAverageScore { get; set; }

    /// <summary>Erscheinungsjahr ab. 0 = keine Bedingung.</summary>
    public int MinYear { get; set; }

    /// <summary>Nur Titel dieser Genres. Leer = alle.</summary>
    public List<string> Genres { get; set; } = new();

    /// <summary>Titel mit erwachsenem Inhalt ausschließen.</summary>
    public bool ExcludeAdult { get; set; } = true;

    /// <summary>Wiedergabe-Provider, Vorgabe ist der Hausanbieter von Animex.</summary>
    public string ProviderId { get; set; } = "nero";

    /// <summary>Adresse des Players, aus der die Wiedergabe-URLs ermittelt werden.</summary>
    public string PlayerBaseUrl { get; set; } = "https://plyr.animex.one";

    /// <summary>Adresse des Animex-GraphQL-Backends.</summary>
    public string CatalogApiUrl { get; set; } = "https://graphql.animex.one/graphql";

    /// <summary>Adresse der AniList-GraphQL-API.</summary>
    public string AniListApiUrl { get; set; } = "https://graphql.anilist.co/";

    /// <summary>Gleichzeitige HTTP-Anfragen beim Auflösen.</summary>
    public int MaxParallelism { get; set; } = 4;

    /// <summary>Wartezeit je HTTP-Anfrage in Sekunden.</summary>
    public int HttpTimeoutSeconds { get; set; } = 30;

    /// <summary>Nur ermitteln, nichts schreiben.</summary>
    public bool DryRun { get; set; } = true;

    /// <summary>Verwaiste Einträge entfernen, statt sie nur zu melden.</summary>
    public bool DeleteOrphans { get; set; }

    /// <summary>Zusätzliche Verweise auf fremde Bibliotheksordner verweigern.</summary>
    public bool ProtectExistingLibraries { get; set; } = true;

    /// <summary>Titelgrenze für die Katalogabfrage je Seite.</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Verweisdatei für die Zuordnung (leer = Standardname im Bibliotheksordner).</summary>
    public string LedgerFileName { get; set; } = ".animex-ledger.json";

    /// <summary>
    /// Adresse, unter der Clients den Jellyfin-Server erreichen, ohne abschließenden Schrägstrich.
    /// Die Vorgabe <c>{serverAddress}</c> lässt Jellyfin die Adresse beim Lesen der Verweisdatei
    /// selbst einsetzen. Für Clients mit abweichender Adresse, etwa außerhalb des Heimnetzes,
    /// kann hier eine feste Adresse stehen.
    /// </summary>
    public string ServerBaseUrl { get; set; } = "{serverAddress}";
}
