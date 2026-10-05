using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Animex.Catalogue;

namespace Jellyfin.Plugin.Animex.Library;

/// <summary>Eine geschriebene Folge.</summary>
public class LedgerEpisode
{
    public int Number { get; set; }

    /// <summary>"sub" oder "dub".</summary>
    public string Language { get; set; } = "sub";

    public string FileName { get; set; } = string.Empty;

    public string StreamUrl { get; set; } = string.Empty;

    /// <summary>Zeitpunkt der letzten erfolgreichen Auflösung.</summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>Meldung, falls die Auflösung nicht vollständig war.</summary>
    public string? Note { get; set; }
}

/// <summary>Ein Titel im Bestand, mit Herkunft und geschriebenen Folgen.</summary>
public class LedgerTitle
{
    public string AnimexId { get; set; } = string.Empty;

    public int AniListId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string FolderName { get; set; } = string.Empty;

    public int EpisodeCount { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    public List<LedgerEpisode> Episodes { get; set; } = new();

    /// <summary>Wurde der Titel im letzten Lauf im Katalog gefunden?</summary>
    public bool SeenInCatalogue { get; set; } = true;
}

/// <summary>
/// Bestand der geschriebenen Einträge. Macht Läufe wiederaufsetzbar, verhindert Duplikate
/// und lässt verwaiste Titel erkennen, ohne das Dateisystem zu durchsuchen.
/// </summary>
public class LibraryLedger
{
    [JsonPropertyName("titles")]
    public List<LedgerTitle> Titles { get; set; } = new();

    [JsonPropertyName("lastRun")]
    public DateTimeOffset? LastRun { get; set; }

    [JsonIgnore]
    public string? Path { get; private set; }

    public static LibraryLedger Load(string path)
    {
        if (!File.Exists(path))
        {
            return new LibraryLedger { Path = path };
        }

        try
        {
            var json = File.ReadAllText(path);
            var ledger = JsonSerializer.Deserialize<LibraryLedger>(json) ?? new LibraryLedger();
            ledger.Path = path;
            return ledger;
        }
        catch (JsonException)
        {
            return new LibraryLedger { Path = path };
        }
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(Path))
        {
            return;
        }

        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path, json);
    }

    public LedgerTitle? Find(string animexId) => Titles.FirstOrDefault(t => t.AnimexId == animexId);

    public LedgerTitle GetOrCreate(AnimeEntry entry, string folderName)
    {
        var existing = Find(entry.AnimexId);
        if (existing is not null)
        {
            existing.AniListId = entry.AniListId;
            existing.Title = entry.Title;
            existing.FolderName = folderName;
            existing.EpisodeCount = entry.EpisodeCount;
            existing.SeenInCatalogue = true;
            existing.UpdatedAt = DateTimeOffset.Now;
            return existing;
        }

        var created = new LedgerTitle
        {
            AnimexId = entry.AnimexId,
            AniListId = entry.AniListId,
            Title = entry.Title,
            FolderName = folderName,
            EpisodeCount = entry.EpisodeCount
        };
        Titles.Add(created);
        return created;
    }
}
