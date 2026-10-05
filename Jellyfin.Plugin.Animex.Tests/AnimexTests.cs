using System.Text.Json;
using Jellyfin.Plugin.Animex.Catalogue;
using Jellyfin.Plugin.Animex.Configuration;
using Jellyfin.Plugin.Animex.Library;
using Jellyfin.Plugin.Animex.Playback;
using Jellyfin.Plugin.Animex.Sync;
using Xunit;

namespace Jellyfin.Plugin.Animex.Tests;

public class AnimexUrlsTests
{
    private static ResolveRequest Request() => new()
    {
        AnimexId = "21",
        AniListId = 21,
        Title = "ONE PIECE",
        EpisodeNumber = 1,
        Language = "sub",
        ProviderId = "anmx",
        PlayerBaseUrl = "https://plyr.animex.one",
        SiteBaseUrl = "https://animex.one"
    };

    [Fact]
    public void SeriesPage_uses_animex_id()
    {
        Assert.Equal("https://animex.one/anime/21", AnimexUrls.SeriesPage(Request()));
    }

    [Fact]
    public void WatchUrl_follows_the_observed_pattern()
    {
        Assert.Equal("https://animex.one/watch/one-piece-21-episode-1", AnimexUrls.WatchUrl(Request(), 1));
    }

    [Fact]
    public void WatchUrl_numbering_is_carried_over()
    {
        Assert.EndsWith("-episode-42", AnimexUrls.WatchUrl(Request(), 42));
    }

    [Fact]
    public void PlayerEpisodeApi_uses_anilist_id_and_episode()
    {
        var request = Request();
        Assert.Equal(
            "https://plyr.animex.one/api/anmx/21/1",
            AnimexUrls.PlayerEpisodeApi(request, request.AniListId, 1));
    }

    [Fact]
    public void PlayerUrl_contains_language_and_provider()
    {
        var url = AnimexUrls.PlayerUrl(Request(), "1059592", 1);
        Assert.Contains("/e/1059592/1", url, StringComparison.Ordinal);
        Assert.Contains("lang=sub", url, StringComparison.Ordinal);
        Assert.Contains("s=anmx", url, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseEpisodeId_reads_the_observed_payload()
    {
        const string payload = """{"episode":{"episodeId":1059592,"audioLayout":"both"}}""";
        Assert.Equal("1059592", AnimexUrls.ParseEpisodeId(payload));
        Assert.Equal("both", AnimexUrls.ParseAudioLayout(payload));
    }

    [Fact]
    public void ParseEpisodeId_tolerates_missing_field()
    {
        Assert.Null(AnimexUrls.ParseEpisodeId("""{"episode":{}}"""));
    }

    [Fact]
    public void FindPlaylistUrl_reads_a_master_playlist_reference()
    {
        const string body = """{"file":"https:\/\/s-95.bcdn2.se\/i\/abc\/master.txt?token=XYZ"}""";
        var found = AnimexUrls.FindPlaylistUrl(body);
        Assert.NotNull(found);
        Assert.Contains("master.txt?token=XYZ", found!, StringComparison.Ordinal);
        Assert.DoesNotContain("\\/", found!, StringComparison.Ordinal);
    }

    [Fact]
    public void LooksLikeHls_detects_a_playlist()
    {
        Assert.True(AnimexUrls.LooksLikeHls("#EXTM3U\n#EXT-X-VERSION:3"));
        Assert.False(AnimexUrls.LooksLikeHls("<html></html>"));
    }

    [Fact]
    public void SeriesKey_slugifies_the_title()
    {
        Assert.Equal("one-piece-21", AnimexUrls.SeriesKey("21", "ONE PIECE"));
        Assert.Equal(
            "re-zero-starting-life-in-another-world-30803",
            AnimexUrls.SeriesKey("30803", "Re:ZERO -Starting Life in Another World-"));
    }
}

public class LibraryWriterTests
{
    private static AnimeEntry Entry() => new()
    {
        AnimexId = "21",
        AniListId = 21,
        Title = "ONE PIECE",
        EpisodeCount = 12,
        SubCount = 12,
        DubCount = 12
    };

    [Fact]
    public void FolderName_contains_id_for_traceability()
    {
        Assert.Equal("ONE PIECE [21]", LibraryWriter.FolderNameFor(Entry()));
    }

    [Fact]
    public void FolderName_removes_invalid_characters()
    {
        var entry = Entry();
        entry.Title = "Re:ZERO / Start?";
        var folder = LibraryWriter.FolderNameFor(entry);
        Assert.DoesNotContain("/", folder, StringComparison.Ordinal);
        Assert.DoesNotContain("?", folder, StringComparison.Ordinal);
        Assert.Contains("[21]", folder, StringComparison.Ordinal);
    }

    [Fact]
    public void FileName_follows_jellyfin_convention()
    {
        var name = LibraryWriter.FileNameFor(Entry(), 1, 3, "sub", 1);
        Assert.Equal("ONE PIECE S01E03", name);
    }

    [Fact]
    public void FileName_marks_language_when_two_versions_exist()
    {
        Assert.Equal("ONE PIECE S01E03 - dub", LibraryWriter.FileNameFor(Entry(), 1, 3, "dub", 2));
    }

    [Fact]
    public void StreamReference_uses_server_placeholder_by_default()
    {
        var reference = LibraryWriter.StreamReference("{serverAddress}", "21", 3, "sub");
        Assert.Equal("{serverAddress}/Animex/Playback/21/3?language=sub", reference);
    }
}

public class CatalogueSyncTests
{
    private static AnimeEntry Entry(int episodeCount, int sub, int dub) => new()
    {
        AnimexId = "21",
        AniListId = 21,
        Title = "ONE PIECE",
        EpisodeCount = episodeCount,
        SubCount = sub,
        DubCount = dub
    };

    [Fact]
    public void Episode_count_falls_back_to_language_counts()
    {
        var config = new PluginConfiguration { AudioMode = "sub" };
        var count = CatalogueSync.EpisodesFor(config, Entry(0, 12, 0), 1, out var languages);
        Assert.Equal(12, count);
        Assert.Equal(new List<string> { "sub" }, languages);
    }

    [Fact]
    public void Sub_mode_uses_sub_count()
    {
        var config = new PluginConfiguration { AudioMode = "sub" };
        var count = CatalogueSync.EpisodesFor(config, Entry(24, 12, 24), 1, out _);
        Assert.Equal(12, count);
    }

    [Fact]
    public void Dub_mode_falls_back_to_sub_when_no_dub_exists()
    {
        var config = new PluginConfiguration { AudioMode = "dub" };
        var count = CatalogueSync.EpisodesFor(config, Entry(12, 12, 0), 1, out var languages);
        Assert.Equal(12, count);
        Assert.Equal(new List<string> { "sub" }, languages);
    }

    [Fact]
    public void Both_mode_lists_only_existing_versions()
    {
        var config = new PluginConfiguration { AudioMode = "both" };
        var count = CatalogueSync.EpisodesFor(config, Entry(12, 12, 0), 2, out var languages);
        Assert.Equal(12, count);
        Assert.Equal(new List<string> { "sub" }, languages);
    }

    [Fact]
    public void Both_mode_lists_both_versions_when_present()
    {
        var config = new PluginConfiguration { AudioMode = "both" };
        _ = CatalogueSync.EpisodesFor(config, Entry(12, 12, 6), 2, out var languages);
        Assert.Equal(new List<string> { "sub", "dub" }, languages);
    }
}

public class LibraryLedgerTests
{
    [Fact]
    public void GetOrCreate_reuses_existing_entry()
    {
        var ledger = new LibraryLedger();
        var entry = new AnimeEntry { AnimexId = "21", AniListId = 21, Title = "ONE PIECE", EpisodeCount = 12 };

        var first = ledger.GetOrCreate(entry, "ONE PIECE [21]");
        var second = ledger.GetOrCreate(entry, "ONE PIECE [21]");

        Assert.Same(first, second);
        Assert.Single(ledger.Titles);
    }

    [Fact]
    public void Save_and_load_round_trip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"animex-ledger-{Guid.NewGuid():N}.json");
        try
        {
            var ledger = LibraryLedger.Load(path);
            var entry = new AnimeEntry { AnimexId = "21", AniListId = 21, Title = "ONE PIECE", EpisodeCount = 12 };
            var title = ledger.GetOrCreate(entry, "ONE PIECE [21]");
            title.Episodes.Add(new LedgerEpisode { Number = 1, Language = "sub", FileName = "ONE PIECE S01E01", StreamUrl = "https://example.invalid/x.m3u8" });
            ledger.Save();

            var reloaded = LibraryLedger.Load(path);
            Assert.Equal("21", Assert.Single(reloaded.Titles).AnimexId);
            Assert.Equal("https://example.invalid/x.m3u8", Assert.Single(reloaded.Titles[0].Episodes).StreamUrl);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Load_returns_empty_ledger_for_broken_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"animex-ledger-broken-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ not json");
        try
        {
            var ledger = LibraryLedger.Load(path);
            Assert.Empty(ledger.Titles);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
