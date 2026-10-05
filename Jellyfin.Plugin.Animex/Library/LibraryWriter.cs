using System.Globalization;
using System.Security;
using System.Text;
using System.Xml.Linq;
using Jellyfin.Plugin.Animex.Catalogue;
using Jellyfin.Plugin.Animex.Configuration;
using Jellyfin.Plugin.Animex.Playback;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Animex.Library;

/// <summary>
/// Schreibt die Bibliotheksdateien: je Folge eine Verweisdatei mit der Wiedergabe-Adresse
/// und eine Metadatendatei, dazu je Titel eine Seriendatei mit Cover und Beschreibung.
/// Es wird kein Videomaterial geschrieben.
/// </summary>
public class LibraryWriter
{
    private readonly PluginConfiguration _config;
    private readonly ILogger _logger;

    public LibraryWriter(PluginConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>Ordner einer Serie, mit Schutz gegen unzulässige Zeichen.</summary>
    public static string FolderNameFor(AnimeEntry entry)
    {
        var baseName = string.IsNullOrWhiteSpace(entry.Title) ? $"Animex {entry.AnimexId}" : entry.Title;
        var cleaned = CleanName(baseName);
        return $"{cleaned} [{entry.AnimexId}]";
    }

    /// <summary>Dateiname einer Folge nach Jellyfin-Konvention.</summary>
    public static string FileNameFor(AnimeEntry entry, int season, int episode, string language, int languageCount)
    {
        var suffix = languageCount > 1
            ? $" - {language.ToLowerInvariant()}"
            : string.Empty;
        return $"{CleanName(entry.Title)} S{season:D2}E{episode:D2}{suffix}";
    }

    /// <summary>Adresse, die in der Verweisdatei steht.</summary>
    public static string StreamReference(string serverBaseUrl, string animexId, int episode, string language)
        => $"{serverBaseUrl.TrimEnd('/')}/Animex/Playback/{Uri.EscapeDataString(animexId)}/{episode}?language={Uri.EscapeDataString(language)}";

    public string SeriesFolder(string libraryPath, AnimeEntry entry)
        => Path.Combine(libraryPath, FolderNameFor(entry));

    /// <summary>Legt den Serienordner an.</summary>
    public string EnsureSeriesFolder(string libraryPath, AnimeEntry entry)
    {
        var folder = SeriesFolder(libraryPath, entry);
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>Schreibt die Seriendatei mit den Metadaten.</summary>
    public void WriteSeriesMetadata(string folder, AnimeEntry entry, AniListMetadata? metadata)
    {
        var root = new XElement("tvshow");

        Add(root, "title", entry.Title);
        Add(root, "sorttitle", entry.SortTitle);
        Add(root, "originaltitle", entry.Title);

        var plot = !string.IsNullOrWhiteSpace(metadata?.Description) ? metadata!.Description : entry.Overview;
        Add(root, "plot", StripHtml(plot));
        Add(root, "outline", StripHtml(plot));

        var year = metadata?.Year > 0 ? metadata!.Year : entry.Year;
        if (year > 0)
        {
            Add(root, "year", year.ToString(CultureInfo.InvariantCulture));
            Add(root, "premiered", $"{year}-01-01");
        }

        var rating = metadata?.AverageScore > 0 ? metadata!.AverageScore : entry.AverageScore;
        if (rating > 0)
        {
            Add(root, "rating", (rating / 10.0).ToString("0.0", CultureInfo.InvariantCulture));
        }

        foreach (var genre in Distinct(entry.Genres.Concat(metadata?.Genres ?? new List<string>())))
        {
            Add(root, "genre", genre);
        }

        foreach (var studio in Distinct(entry.Studios))
        {
            Add(root, "studio", studio);
        }

        Add(root, "id", entry.AniListId > 0 ? entry.AniListId.ToString(CultureInfo.InvariantCulture) : entry.AnimexId);

        var cover = FirstNonEmpty(metadata?.CoverUrl, entry.CoverUrl);
        if (!string.IsNullOrWhiteSpace(cover))
        {
            root.Add(new XElement("thumb", new XAttribute("aspect", "poster"), cover));
        }

        var banner = FirstNonEmpty(metadata?.BannerUrl, entry.BannerUrl);
        if (!string.IsNullOrWhiteSpace(banner))
        {
            root.Add(new XElement("fanart", new XElement("thumb", banner)));
        }

        WriteXml(Path.Combine(folder, "tvshow.nfo"), root);
    }

    /// <summary>Schreibt Verweis- und Metadatendatei einer Folge.</summary>
    public string WriteEpisode(
        string folder,
        AnimeEntry entry,
        int episode,
        string language,
        int languageCount,
        string streamReference,
        string? subtitleNote)
    {
        var fileName = FileNameFor(entry, entry.SeasonNumber, episode, language, languageCount);

        // Verweisdatei: Inhalt ist genau eine Adresse.
        File.WriteAllText(Path.Combine(folder, fileName + ".strm"), streamReference, new UTF8Encoding(false));

        var episodeRoot = new XElement("episodedetails");
        Add(episodeRoot, "title", $"Episode {episode}");
        Add(episodeRoot, "season", entry.SeasonNumber.ToString(CultureInfo.InvariantCulture));
        Add(episodeRoot, "episode", episode.ToString(CultureInfo.InvariantCulture));
        Add(episodeRoot, "plot", StripHtml(entry.Overview));
        if (languageCount > 1)
        {
            Add(episodeRoot, "sorttitle", $"{entry.Title} {language} {episode:D2}");
        }

        WriteXml(Path.Combine(folder, fileName + ".nfo"), episodeRoot);

        if (!string.IsNullOrWhiteSpace(subtitleNote))
        {
            _logger.LogDebug("Untertitelhinweis für {File}: {Note}", fileName, subtitleNote);
        }

        return fileName;
    }

    /// <summary>Entfernt die Dateien eines Titels.</summary>
    public int RemoveTitle(string libraryPath, LedgerTitle title)
    {
        var folder = Path.Combine(libraryPath, title.FolderName);
        if (!Directory.Exists(folder))
        {
            return 0;
        }

        var count = Directory.GetFiles(folder, "*.strm").Length;
        Directory.Delete(folder, true);
        return count;
    }

    /// <summary>
    /// Prüft, ob im Zielordner bereits eine fremde Bibliothek liegt. Verhindert, dass das
    /// Plugin in einen bestehenden Bestand schreibt.
    /// </summary>
    public bool ContainsForeignLibrary(string libraryPath, IReadOnlyCollection<string> knownFolders)
    {
        if (!Directory.Exists(libraryPath))
        {
            return false;
        }

        var entries = Directory.GetFileSystemEntries(libraryPath);
        foreach (var entry in entries)
        {
            var name = Path.GetFileName(entry);
            if (name.StartsWith('.') || knownFolders.Contains(name))
            {
                continue;
            }

            if (Directory.Exists(entry))
            {
                var hasMedia = Directory.EnumerateFiles(entry, "*.*", SearchOption.AllDirectories)
                    .Take(50)
                    .Any(file => file.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase)
                        || file.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                        || file.EndsWith(".avi", StringComparison.OrdinalIgnoreCase));
                if (hasMedia)
                {
                    return true;
                }
            }
            else if (entry.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) || entry.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void WriteXml(string path, XElement root)
    {
        var document = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        document.Save(writer);
    }

    private static void Add(XElement parent, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parent.Add(new XElement(name, value));
        }
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    private static IEnumerable<string> Distinct(IEnumerable<string> values)
        => values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(12);

    private static string StripHtml(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var withoutTags = System.Text.RegularExpressions.Regex.Replace(value, "<[^>]+>", " ");
        var decoded = System.Net.WebUtility.HtmlDecode(withoutTags);
        return System.Text.RegularExpressions.Regex.Replace(decoded, "\\s+", " ").Trim();
    }

    private static string CleanName(string value)
    {
        // Unter Linux sind nur / und Null unzulässig. Für NAS mit Windows-Dateisystem und für
        // Clients, die die Namen weiterverwenden, werden zusätzlich die dort reservierten
        // Zeichen ersetzt. Ein fester Satz ist verlässlicher als die Plattformabfrage.
        char[] reserved = { '<', '>', ':', '"', '/', '\\', '|', '?', '*', '\0' };
        var invalid = reserved.Concat(Path.GetInvalidFileNameChars().Where(c => c != '/' && c != '\0')).Distinct().ToArray();

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character == ':' || character == '/' || character == '\\')
            {
                builder.Append(" - ");
            }
            else if (Array.IndexOf(invalid, character) >= 0)
            {
                builder.Append('_');
            }
            else
            {
                builder.Append(character);
            }
        }

        var cleaned = System.Text.RegularExpressions.Regex.Replace(builder.ToString(), "\\s+", " ").Trim();
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "(\\s-\\s)+", " - ").Trim(' ', '-', '.', '_');
        return string.IsNullOrWhiteSpace(cleaned) ? "Unbenannt" : cleaned;
    }
}
