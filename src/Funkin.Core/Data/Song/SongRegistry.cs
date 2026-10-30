using Funkin.Paths;
using Funkin.Util;

namespace Funkin.Data.Song;

/// <summary>
/// Port of SongRegistry: discovers songs on disk and parses their metadata + chart files.
/// File layout (matching the Haxe original):
///   data/songs/&lt;id&gt;/&lt;id&gt;-metadata[-variation].json
///   data/songs/&lt;id&gt;/&lt;id&gt;-chart[-variation].json
/// </summary>
public sealed class SongRegistry
{
    public const string DefaultGeneratedBy = Constants.GENERATED_BY;

    public static SongRegistry Instance { get; } = new();

    private readonly Dictionary<string, Song> _cache = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, Song> Cache => _cache;

    public event Action<string>? OnSongLoaded;

    /// <summary>Lists every song id with a metadata file.</summary>
    public List<string> ListSongIds()
    {
        var ids = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in FunkinPaths.Instance.ListDataFiles("data/songs/", "-metadata.json"))
        {
            ids.Add(StripSuffix(path));
        }

        return ids.ToList();
    }

    private static string StripSuffix(string fileName)
    {
        const string suffix = "-metadata.json";
        return fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ? fileName[..^suffix.Length] : fileName;
    }

    /// <summary>Lists the variations for a song, by looking for `&lt;id&gt;-metadata-&lt;variation&gt;.json`.</summary>
    public List<string> ListVariations(string songId)
    {
        var variations = new List<string> { Constants.DEFAULT_VARIATION };

        foreach (var path in FunkinPaths.Instance.ListDataFiles("data/songs/", "-metadata.json"))
        {
            if (!string.Equals(path, songId, StringComparison.OrdinalIgnoreCase)) continue;
        }

        // Variations are discovered by probing the folder for known filenames.
        string folder = $"data/songs/{songId}";
        var resolved = FunkinPaths.Instance.Resolve(folder);
        string? directory = resolved is not null && Directory.Exists(resolved) ? resolved : Path.Combine(FunkinPaths.Instance.AssetsRoot, folder);

        if (Directory.Exists(directory))
        {
            foreach (var file in Directory.GetFiles(directory, $"{songId}-metadata-*.json"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string prefix = $"{songId}-metadata-";
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    variations.Add(name[prefix.Length..]);
                }
            }
        }

        return variations.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Loads (and caches) a song with all of its variations.</summary>
    public Song? LoadSong(string songId, bool force = false)
    {
        if (!force && _cache.TryGetValue(songId, out var cached)) return cached;

        var song = new Song(songId);

        foreach (var variation in ListVariations(songId))
        {
            var metadata = LoadMetadata(songId, variation);
            if (metadata is null) continue;

            var chart = LoadChart(songId, variation);
            if (chart is null) continue;

            song.Metadata[variation] = metadata;
            song.Charts[variation] = chart;
        }

        if (song.Metadata.Count == 0)
        {
            Logger.Warn($"No valid data found for song '{songId}'.");
            return null;
        }

        _cache[songId] = song;
        OnSongLoaded?.Invoke(songId);
        return song;
    }

    public SongMetadata? LoadMetadata(string songId, string variation = Constants.DEFAULT_VARIATION)
    {
        string suffix = variation == Constants.DEFAULT_VARIATION ? string.Empty : $"-{variation}";
        var json = FunkinPaths.Instance.ReadText($"data/songs/{songId}/{songId}-metadata{suffix}.json");
        if (json is null) return null;

        var metadata = Json.Deserialize<SongMetadata>(json);
        if (metadata is null) return null;

        metadata.Variation = variation;
        if (metadata.TimeChanges.Count == 0) metadata.TimeChanges.Add(SongTimeChange.Default.Clone());
        if (metadata.PlayData.Difficulties.Count == 0) metadata.PlayData.Difficulties.AddRange(Constants.DEFAULT_DIFFICULTY_LIST);

        return metadata;
    }

    public SongChartData? LoadChart(string songId, string variation = Constants.DEFAULT_VARIATION)
    {
        string suffix = variation == Constants.DEFAULT_VARIATION ? string.Empty : $"-{variation}";
        var json = FunkinPaths.Instance.ReadText($"data/songs/{songId}/{songId}-chart{suffix}.json");
        if (json is null) return null;

        var chart = Json.Deserialize<SongChartData>(json);
        if (chart is null) return null;

        chart.Variation = variation;
        return chart;
    }

    /// <summary>Loads every song into the cache (used by Freeplay and the preloader).</summary>
    public void LoadAll()
    {
        _cache.Clear();
        foreach (var id in ListSongIds()) LoadSong(id);
        Logger.Info($"Loaded {_cache.Count} songs.");
    }

    public List<string> ListAllDifficulties(string songId, string variation = Constants.DEFAULT_VARIATION)
    {
        var song = LoadSong(songId);
        return song?.ListDifficulties(variation) ?? new List<string>();
    }
}
