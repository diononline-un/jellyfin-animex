using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using Jellyfin.Plugin.Animex.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Animex;

/// <summary>Einstiegspunkt des Plugins.</summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>Feste Kennung des Plugins.</summary>
    public static readonly Guid PluginId = new("4f1c9b3e-7a52-4d8f-9c21-0d6b8e5a7f31");

    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<Plugin> _logger;

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Animex";

    /// <inheritdoc />
    public override string Description =>
        "Stellt Animes aus der Animex-Infrastruktur als Jellyfin-Bibliothek bereit. Pro Folge wird eine Verweisdatei "
        + "angelegt, kein Videomaterial. Wiedergabestatus und Fortsetzen führt Jellyfin.";

    /// <inheritdoc />
    public override Guid Id => PluginId;

    /// <summary>Aktuelle Instanz des Plugins.</summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>Pfad des Laufberichts.</summary>
    public string ReportFilePath => Path.Combine(_applicationPaths.DataPath, "animex", "last-run.json");

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
        };
    }

    /// <summary>Schreibt den letzten Laufbericht.</summary>
    public void SaveReport(Sync.SyncReport report)
    {
        try
        {
            var path = ReportFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var payload = new
            {
                report.StartedAt,
                report.FinishedAt,
                report.DryRun,
                report.CatalogueSize,
                report.TitlesProcessed,
                report.EpisodesWritten,
                report.EpisodesSkipped,
                report.ResolveFailures,
                report.OrphansFound,
                report.OrphansDeleted,
                report.AbortReason,
                Summary = report.Summary(),
                Errors = report.Errors,
                Notes = report.Notes
            };
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Laufbericht konnte nicht geschrieben werden");
        }
    }
}
