using System.Text.Json.Serialization;
using Funkin.Data;
using Funkin.Paths;
using Funkin.Util;

namespace Funkin.Data.Story;

/// <summary>Port of LevelData.LevelCapsuleData.</summary>
public sealed class LevelCapsuleData
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("offsets")] public List<float>? Offsets { get; set; }
}

/// <summary>Port of LevelData.LevelPropData.</summary>
public sealed class LevelPropData
{
    [JsonPropertyName("assetPath")] public string AssetPath { get; set; } = string.Empty;
    [JsonPropertyName("position")] public List<float> Position { get; set; } = new() { 0, 0 };
    [JsonPropertyName("scale")] public float Scale { get; set; } = 1f;
    [JsonPropertyName("animations")] public List<AnimationData> Animations { get; set; } = new();
    [JsonPropertyName("flipX")] public bool FlipX { get; set; }
    [JsonPropertyName("isPixel")] public bool IsPixel { get; set; }
}

/// <summary>
/// Port of LevelData: a story mode week (`data/weeks/&lt;id&gt;.json`).
/// </summary>
public sealed class LevelData
{
    [JsonPropertyName("version")] public string Version { get; set; } = "1.0.1";
    [JsonPropertyName("name")] public string Name { get; set; } = "Unknown";
    [JsonPropertyName("capsule")] public LevelCapsuleData? Capsule { get; set; }
    [JsonPropertyName("titleAsset")] public string TitleAsset { get; set; } = string.Empty;
    [JsonPropertyName("props")] public List<LevelPropData> Props { get; set; } = new();
    [JsonPropertyName("visible")] public bool Visible { get; set; } = true;
    [JsonPropertyName("songs")] public List<string> Songs { get; set; } = new();
    [JsonPropertyName("background")] public string Background { get; set; } = "#F9CF51";

    [JsonIgnore] public string Id { get; set; } = string.Empty;
}

/// <summary>Port of LevelRegistry: loads story mode weeks.</summary>
public sealed class LevelRegistry
{
    public static LevelRegistry Instance { get; } = new();

    private readonly Dictionary<string, LevelData> _cache = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, LevelData> Cache => _cache;

    public List<string> ListWeekIds()
    {
        var ids = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in FunkinPaths.Instance.ListDataFiles("data/weeks/", ".json")) ids.Add(Path.GetFileName(path));
        return ids.ToList();
    }

    public void LoadAll()
    {
        _cache.Clear();
        foreach (var id in ListWeekIds())
        {
            var data = Load(id);
            if (data is not null) _cache[id] = data;
        }

        Logger.Info($"Loaded {_cache.Count} weeks.");
    }

    public LevelData? Load(string weekId)
    {
        if (_cache.TryGetValue(weekId, out var cached)) return cached;

        var json = FunkinPaths.Instance.ReadText($"data/weeks/{weekId}.json");
        if (json is null)
        {
            Logger.Warn($"Week data not found for '{weekId}'.");
            return null;
        }

        var data = Json.Deserialize<LevelData>(json);
        if (data is null) return null;

        data.Id = weekId;
        _cache[weekId] = data;
        return data;
    }

    /// <summary>Weeks visible in the Story menu, in load order.</summary>
    public List<LevelData> ListVisibleWeeks()
    {
        if (_cache.Count == 0) LoadAll();
        return _cache.Values.Where(w => w.Visible).OrderBy(w => w.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
