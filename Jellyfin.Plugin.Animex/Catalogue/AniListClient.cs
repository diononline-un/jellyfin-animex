using System.Globalization;
using System.Text;
using System.Text.Json;
using Jellyfin.Plugin.Animex.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Animex.Catalogue;

/// <summary>Zusätzliche Metadaten aus AniList, verknüpft über die AniList-Kennung.</summary>
public class AniListMetadata
{
    public string Description { get; set; } = string.Empty;

    public string CoverUrl { get; set; } = string.Empty;

    public string BannerUrl { get; set; } = string.Empty;

    public List<string> Genres { get; set; } = new();

    public int Year { get; set; }

    public int AverageScore { get; set; }

    public int Duration { get; set; }

    public string Status { get; set; } = string.Empty;
}

/// <summary>Reichert Katalogeinträge über die AniList-GraphQL-API an.</summary>
public class AniListClient
{
    private const string Query = """
        query ($id: Int) {
          Media(id: $id, type: ANIME) {
            description(asHtml: false)
            averageScore
            duration
            status
            seasonYear
            startDate { year }
            genres
            coverImage { extraLarge large }
            bannerImage
          }
        }
        """;

    private readonly HttpClient _http;
    private readonly ILogger _logger;

    public AniListClient(HttpClient http, ILogger logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Holt Metadaten zu einer AniList-Kennung. Gibt bei Fehlern null zurück.</summary>
    public async Task<AniListMetadata?> FetchAsync(PluginConfiguration config, int aniListId, CancellationToken cancellationToken)
    {
        if (aniListId <= 0)
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Serialize(new { query = Query, variables = new { id = aniListId } });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(config.AniListApiUrl, content, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AniList antwortete für {Id} mit HTTP {Status}", aniListId, (int)response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("data", out var data)
                || !data.TryGetProperty("Media", out var media)
                || media.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var metadata = new AniListMetadata
            {
                Description = GetString(media, "description"),
                BannerUrl = GetString(media, "bannerImage"),
                AverageScore = GetInt(media, "averageScore"),
                Duration = GetInt(media, "duration"),
                Status = GetString(media, "status"),
                Year = GetInt(media, "seasonYear")
            };

            if (media.TryGetProperty("coverImage", out var cover) && cover.ValueKind == JsonValueKind.Object)
            {
                metadata.CoverUrl = GetString(cover, "extraLarge");
                if (string.IsNullOrWhiteSpace(metadata.CoverUrl))
                {
                    metadata.CoverUrl = GetString(cover, "large");
                }
            }

            if (metadata.Year == 0
                && media.TryGetProperty("startDate", out var startDate)
                && startDate.ValueKind == JsonValueKind.Object)
            {
                metadata.Year = GetInt(startDate, "year");
            }

            if (media.TryGetProperty("genres", out var genres) && genres.ValueKind == JsonValueKind.Array)
            {
                foreach (var genre in genres.EnumerateArray())
                {
                    if (genre.ValueKind == JsonValueKind.String)
                    {
                        metadata.Genres.Add(genre.GetString()!);
                    }
                }
            }

            return metadata;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "AniList-Anreicherung für {Id} fehlgeschlagen", aniListId);
            return null;
        }
    }

    private static string GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static int GetInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0
        };
    }
}
