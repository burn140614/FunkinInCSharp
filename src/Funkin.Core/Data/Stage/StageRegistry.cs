using Funkin.Paths;
using Funkin.Util;

namespace Funkin.Data.Stage;

/// <summary>Port of StageRegistry: loads and caches stage JSON.</summary>
public sealed class StageRegistry
{
    public const string StageDataVersion = "1.0.3";

    public static StageRegistry Instance { get; } = new();

    private readonly Dictionary<string, StageData> _cache = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, StageData> Cache => _cache;

    public List<string> ListStageIds()
    {
        var ids = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in FunkinPaths.Instance.ListDataFiles("data/stages/", ".json")) ids.Add(Path.GetFileName(path));
        return ids.ToList();
    }

    public void LoadAll()
    {
        _cache.Clear();
        foreach (var id in ListStageIds())
        {
            var data = Load(id);
            if (data is not null) _cache[id] = data;
        }

        Logger.Info($"Loaded {_cache.Count} stages.");
    }

    public StageData? Load(string stageId)
    {
        if (_cache.TryGetValue(stageId, out var cached)) return cached;

        var json = FunkinPaths.Instance.ReadText($"data/stages/{stageId}.json")
                   ?? FunkinPaths.Instance.ReadText($"data/stages/{stageId}/{stageId}.json");

        if (json is null)
        {
            Logger.Warn($"Stage data not found for '{stageId}'.");
            return null;
        }

        var data = Json.Deserialize<StageData>(json);
        if (data is null) return null;

        _cache[stageId] = data;
        return data;
    }

    public StageData GetOrFallback(string stageId)
    {
        return Load(stageId) ?? Load(Constants.DEFAULT_STAGE) ?? new StageData { Name = stageId };
    }
}
