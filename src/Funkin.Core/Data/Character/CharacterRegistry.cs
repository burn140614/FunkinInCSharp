using Funkin.Paths;
using Funkin.Util;

namespace Funkin.Data.Character;

/// <summary>
/// Port of CharacterDataParser: loads and caches every character's JSON data.
/// </summary>
public sealed class CharacterRegistry
{
    public static CharacterRegistry Instance { get; } = new();

    private readonly Dictionary<string, CharacterData> _cache = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, CharacterData> Cache => _cache;

    public IReadOnlyList<string> ListCharacterIds()
    {
        var ids = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in FunkinPaths.Instance.ListDataFiles("data/characters/", ".json"))
        {
            ids.Add(Path.GetFileName(path));
        }

        return ids.ToList();
    }

    public void LoadAll()
    {
        _cache.Clear();
        foreach (var charId in ListCharacterIds())
        {
            var data = Load(charId);
            if (data is not null) _cache[charId] = data;
        }

        Logger.Info($"Loaded {_cache.Count} characters.");
    }

    public CharacterData? Load(string charId)
    {
        if (_cache.TryGetValue(charId, out var cached)) return cached;

        var json = FunkinPaths.Instance.ReadText($"data/characters/{charId}.json");
        if (json is null)
        {
            // Also support the `data/characters/<id>/<id>.json` layout.
            json = FunkinPaths.Instance.ReadText($"data/characters/{charId}/{charId}.json");
        }

        if (json is null)
        {
            Logger.Warn($"Character data not found for '{charId}'.");
            return null;
        }

        var data = Json.Deserialize<CharacterData>(json);
        if (data is null) return null;

        if (data.AssetPath.Length == 0) data.AssetPath = charId;
        _cache[charId] = data;
        return data;
    }

    public CharacterData GetOrFallback(string charId, string fallbackId = Constants.FallbackCharacter)
    {
        return Load(charId) ?? Load(fallbackId) ?? new CharacterData
        {
            Name = charId,
            AssetPath = charId,
        };
    }
}
