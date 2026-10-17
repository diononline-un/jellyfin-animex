using System.Collections.Concurrent;

namespace Jellyfin.Plugin.Animex.Sync;

/// <summary>Ergebnis eines Synchronisationslaufs.</summary>
public class SyncReport
{
    private readonly ConcurrentQueue<string> _errors = new();
    private readonly ConcurrentQueue<string> _notes = new();

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset? FinishedAt { get; set; }

    public int CatalogueSize { get; set; }

    public int TitlesProcessed { get; set; }

    public int TitlesSkipped { get; set; }

    public int EpisodesWritten { get; set; }

    public int EpisodesSkipped { get; set; }

    public int ResolveFailures { get; set; }

    public int OrphansFound { get; set; }

    public int OrphansDeleted { get; set; }

    public bool DryRun { get; set; }

    public string? AbortReason { get; set; }

    public IReadOnlyCollection<string> Errors => _errors.ToArray();

    public IReadOnlyCollection<string> Notes => _notes.ToArray();

    public void AddError(string message) => _errors.Enqueue(message);

    public void AddNote(string message) => _notes.Enqueue(message);

    /// <summary>Kurzfassung für Protokoll und Statusseite.</summary>
    public string Summary()
    {
        var duration = (FinishedAt ?? DateTimeOffset.Now) - StartedAt;
        return $"Lauf {(DryRun ? "(Trockenlauf) " : string.Empty)}beendet in {duration.TotalSeconds:F0}s: "
            + $"{CatalogueSize} Titel im Katalog, {TitlesProcessed} verarbeitet, {TitlesSkipped} unverändert, "
            + $"{EpisodesWritten} Folgen geschrieben, {EpisodesSkipped} übersprungen, "
            + $"{ResolveFailures} Auflösungsfehler, {OrphansFound} verwaist"
            + (OrphansDeleted > 0 ? $", {OrphansDeleted} entfernt" : string.Empty)
            + (AbortReason is null ? string.Empty : $", Abbruch: {AbortReason}");
    }
}
