using System.Text.Json.Serialization;
using Funkin.Data;
using Funkin.Paths;
using Funkin.Util;

namespace Funkin.Data.Freeplay;

/// <summary>Port of AlbumData: metadata for a Freeplay album (`data/albums/&lt;id&gt;.json`).</summary>
public sealed class AlbumData
{
    [JsonPropertyName("version")] public string Version { get; set; } = "1.0.0";
    [JsonPropertyName("name")] public string Name { get; set; } = "Unknown";
    [JsonPropertyName("artists")] public List<string> Artists { get; set; } = new();
    [JsonPropertyName("albumArtAsset")] public string AlbumArtAsset { get; set; } = string.Empty;
    [JsonPropertyName("albumTitleAsset")] public string AlbumTitleAsset { get; set; } = string.Empty;
    [JsonPropertyName("albumTitleOffsets")] public List<float>? AlbumTitleOffsets { get; set; }
    [JsonPropertyName("albumTitleAnimations")] public List<AnimationData> AlbumTitleAnimations { get; set; } = new();
    [JsonPropertyName("albumOSTName")] public string AlbumOstName { get; set; } = Constants.DEFAULT_OST_NAME;
    [JsonPropertyName("discordRPCImage")] public string DiscordRpcImage { get; set; } = string.Empty;

    [JsonIgnore] public string Id { get; set; } = string.Empty;
}

/// <summary>Port of AlbumRegistry.</summary>
public sealed class AlbumRegistry
{
    public static AlbumRegistry Instance { get; } = new();

    private readonly Dictionary<string, AlbumData> _cache = new(StringComparer.OrdinalIgnoreCase);

    public void LoadAll()
    {
        _cache.Clear();
        foreach (var id in FunkinPaths.Instance.ListDataFiles("data/albums/", ".json"))
        {
            var json = FunkinPaths.Instance.ReadText($"data/albums/{id}.json");
            if (json is null) continue;
            var data = Json.Deserialize<AlbumData>(json);
            if (data is null) continue;
            data.Id = id;
            _cache[id] = data;
        }

        Logger.Info($"Loaded {_cache.Count} albums.");
    }

    public AlbumData? Get(string? id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_cache.Count == 0) LoadAll();
        return _cache.TryGetValue(id, out var album) ? album : null;
    }
}
