using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Jellyfin.Plugin.Animex.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Animex.Catalogue;

/// <summary>Ein Anime, wie er für die Bibliothek gebraucht wird.</summary>
public class AnimeEntry
{
    /// <summary>Interne Kennung bei Animex.</summary>
    public string AnimexId { get; set; } = string.Empty;

    /// <summary>AniList-Kennung, Grundlage für Metadaten und Episodenauflösung.</summary>
    public int AniListId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string SortTitle { get; set; } = string.Empty;

    public string Overview { get; set; } = string.Empty;

    public string CoverUrl { get; set; } = string.Empty;

    public string BannerUrl { get; set; } = string.Empty;

    public List<string> Genres { get; set; } = new();

    public List<string> Studios { get; set; } = new();

    public int Year { get; set; }

    public int AverageScore { get; set; }

    public int EpisodeCount { get; set; }

    public int SubCount { get; set; }

    public int DubCount { get; set; }

    public bool IsAdult { get; set; }

    /// <summary>Staffelnummer für die Dateibenennung. Animex liefert keine Staffeln, daher immer 1.</summary>
    public int SeasonNumber => 1;
}

/// <summary>Liest den Katalog aus dem Animex-GraphQL-Backend.</summary>
public class AnimexCatalogueClient
{
    /// <summary>Pause zwischen zwei Katalogseiten, gegen die Anfragebegrenzung der Gegenstelle.</summary>
    private static readonly TimeSpan PageDelay = TimeSpan.FromSeconds(1);

    private const string Query = """
        query Catalogue($filter: AnimeCatalogFilterInput, $limit: Int, $offset: Int) {
          catalogAnime(filter: $filter, limit: $limit, offset: $offset) {
            totalCount
            hasNextPage
            items {
              id
              anilistId
              titleEnglish
              titleRomaji
              description
              coverImage
              bannerImage
              genres
              studios
              seasonYear
              averageScore
              isAdult
              episodeCount
              subCount
              dubCount
            }
          }
        }
        """;

    private readonly HttpClient _http;
    private readonly ILogger _logger;

    public AnimexCatalogueClient(HttpClient http, ILogger logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Holt den gefilterten Katalog.</summary>
    public async Task<IReadOnlyList<AnimeEntry>> FetchAsync(PluginConfiguration config, CancellationToken cancellationToken)
    {
        var result = new List<AnimeEntry>();
        var offset = 0;
        var pageSize = Math.Clamp(config.PageSize, 1, 100);
        var limit = config.MaxTitles > 0 ? config.MaxTitles : int.MaxValue;
        var filter = BuildFilter(config);

        while (result.Count < limit)
        {
            var take = (int)Math.Min(pageSize, limit - result.Count);
            var items = await FetchPageAsync(config, filter, take, offset, cancellationToken).ConfigureAwait(false);
            if (items.Count == 0)
            {
                break;
            }

            result.AddRange(items);
            offset += items.Count;
            _logger.LogInformation("Katalog gelesen: {Count} Titel bisher", result.Count);

            // Die Gegenstelle begrenzt die Anfragen. Eine kurze Pause zwischen den Seiten hält
            // den Lauf unter dem Kontingent; die Wiederholung in PostAsync fängt den Rest ab.
            if (items.Count == take)
            {
                await Task.Delay(PageDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        return result;
    }

    /// <summary>
    /// Holt einen einzelnen Titel über seine Kennung. Nutzt den Filter der Gegenstelle, damit
    /// genau eine Anfrage nötig ist. Der Wiedergabe-Endpunkt braucht das bei jedem Abspielen.
    /// </summary>
    public async Task<AnimeEntry?> FetchByIdAsync(string animexId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(animexId))
        {
            return null;
        }

        var filter = new Dictionary<string, object?> { ["ids"] = animexId };
        var items = await FetchPageAsync(new PluginConfiguration(), filter, 1, 0, cancellationToken).ConfigureAwait(false);
        return items.FirstOrDefault();
    }

    private async Task<List<AnimeEntry>> FetchPageAsync(
        PluginConfiguration config,
        Dictionary<string, object?> filter,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            query = Query,
            variables = new { filter, limit, offset }
        });

        var body = await PostAsync(config, payload, cancellationToken).ConfigureAwait(false);
        using var document = JsonDocument.Parse(body);
        if (document.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
        {
            throw new InvalidOperationException($"Katalogabfrage lieferte Fehler: {Truncate(errors.ToString())}");
        }

        var connection = document.RootElement.GetProperty("data").GetProperty("catalogAnime");
        var entries = new List<AnimeEntry>();
        if (!connection.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return entries;
        }

        foreach (var item in items.EnumerateArray())
        {
            var entry = Map(item);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private Dictionary<string, object?> BuildFilter(PluginConfiguration config)
    {
        var filter = new Dictionary<string, object?>
        {
            ["includeAdult"] = !config.ExcludeAdult
        };

        if (config.MinYear > 0)
        {
            filter["seasonYearMin"] = config.MinYear;
        }

        if (config.MinAverageScore > 0)
        {
            filter["averageScoreMin"] = config.MinAverageScore;
        }

        if (config.Genres.Count > 0)
        {
            filter["genres"] = string.Join(",", config.Genres);
        }

        var subDub = config.AudioMode.ToLowerInvariant() switch
        {
            "sub" => "SUB",
            "dub" => "DUB",
            _ => "ANY"
        };
        filter["subDubFilter"] = subDub;

        return filter;
    }

    /// <summary>
    /// Sendet eine Katalogabfrage und wiederholt sie bei Überlastung. Die Gegenstelle antwortet
    /// bei zu vielen Anfragen mit dem Status 429 und nennt in <c>retry_after</c> die Wartezeit in Sekunden. Ohne diese Behandlung bricht der Lauf ab, sobald das Kontingent erschöpft ist.
    /// </summary>
    private async Task<string> PostAsync(PluginConfiguration config, string payload, CancellationToken cancellationToken)
    {
        const int maxAttempts = 4;
        for (var attempt = 1; ; attempt++)
        {
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(config.CatalogApiUrl, content, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return body;
            }

            var status = (int)response.StatusCode;
            var retryable = status == 429 || status >= 500;
            if (!retryable || attempt >= maxAttempts)
            {
                throw new HttpRequestException($"Katalogabfrage fehlgeschlagen: HTTP {status} {Truncate(body)}");
            }

            var wait = CatalogueRetry.DelayFor(response, body, attempt);
            _logger.LogWarning(
                "Katalogabfrage mit HTTP {Status} beantwortet, neuer Versuch {Attempt} von {Max} in {Wait}s",
                status,
                attempt + 1,
                maxAttempts,
                (int)wait.TotalSeconds);
            await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
        }
    }

    private AnimeEntry? Map(JsonElement item)
    {
        var id = item.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var english = GetString(item, "titleEnglish");
        var romaji = GetString(item, "titleRomaji");
        var title = !string.IsNullOrWhiteSpace(english) ? english : romaji;
        if (string.IsNullOrWhiteSpace(title))
        {
            title = $"Animex {id}";
        }

        return new AnimeEntry
        {
            AnimexId = id,
            AniListId = GetInt(item, "anilistId"),
            Title = title,
            SortTitle = title,
            Overview = GetString(item, "description"),
            CoverUrl = GetImageUrl(item, "coverImage"),
            BannerUrl = GetString(item, "bannerImage"),
            Genres = GetStringList(item, "genres"),
            Studios = GetStringList(item, "studios"),
            Year = GetInt(item, "seasonYear"),
            AverageScore = GetInt(item, "averageScore"),
            EpisodeCount = GetInt(item, "episodeCount"),
            SubCount = GetInt(item, "subCount"),
            DubCount = GetInt(item, "dubCount"),
            IsAdult = item.TryGetProperty("isAdult", out var adult) && adult.ValueKind == JsonValueKind.True
        };
    }

    private static string GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    /// <summary>
    /// Bildadresse lesen. Animex liefert das Feld mal als Zeichenkette, mal als Objekt mit
    /// mehreren Größen. Beide Formen kommen live vor, deshalb werden beide behandelt.
    /// </summary>
    private static string GetImageUrl(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return string.Empty;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        foreach (var key in new[] { "extraLarge", "large", "medium", "url", "small" })
        {
            var candidate = GetString(value, key);
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

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

    private static List<string> GetStringList(JsonElement element, string name)
    {
        var list = new List<string>();
        if (!element.TryGetProperty(name, out var value))
        {
            return list;
        }

        switch (value.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var text = item.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            list.Add(text);
                        }
                    }
                    else if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("name", out var nested) && nested.ValueKind == JsonValueKind.String)
                    {
                        list.Add(nested.GetString()!);
                    }
                }

                break;
            case JsonValueKind.String:
                list.AddRange(value.GetString()!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                break;
        }

        return list;
    }

    private static string Truncate(string value) => value.Length <= 300 ? value : value[..300];
}

/// <summary>Berechnet die Wartezeit vor einem erneuten Versuch.</summary>
public static class CatalogueRetry
{
    public static TimeSpan DelayFor(HttpResponseMessage response, string body, int attempt)
    {
        var seconds = 0;
        if (response.Headers.RetryAfter?.Delta is { } delta)
        {
            seconds = (int)delta.TotalSeconds;
        }

        if (seconds <= 0)
        {
            var match = System.Text.RegularExpressions.Regex.Match(body, "\"retry_after\"\\s*:\\s*(?<seconds>[0-9]+)");
            if (match.Success && int.TryParse(match.Groups["seconds"].Value, out var parsed))
            {
                seconds = parsed;
            }
        }

        if (seconds <= 0)
        {
            // Wachsende Wartezeit, falls die Gegenstelle keine Angabe macht.
            seconds = (int)Math.Pow(2, attempt) * 5;
        }

        // Obergrenze, damit ein Lauf nicht unbegrenzt hängt.
        return TimeSpan.FromSeconds(Math.Clamp(seconds, 1, 180));
    }
}
