using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Funkin.Play.Notes;

/// <summary>Port of funkin.play.notes.notekind.NoteKind: a data-driven custom note behaviour.</summary>
public sealed class NoteKind
{
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

    /// <summary>Invert the note's colour (used by 'alt-animation').</summary>
    [JsonPropertyName("invertColor")] public bool InvertColor { get; set; }

    /// <summary>Invert the note's scroll direction (used by mines).</summary>
    [JsonPropertyName("invertScroll")] public bool InvertScroll { get; set; }

    /// <summary>Animation suffix to use for the strumline and character, e.g. 'alt' for 'singLEFT-alt'.</summary>
    [JsonPropertyName("animationSuffix")] public string? AnimationSuffix { get; set; }

    [JsonPropertyName("healthGain")] public float HealthGain { get; set; } = 0.023f;
    [JsonPropertyName("healthLoss")] public float HealthLoss { get; set; } = 0.0475f;
    [JsonPropertyName("scoreBonus")] public int ScoreBonus { get; set; }
    [JsonPropertyName("hitSound")] public string? HitSound { get; set; }
    [JsonPropertyName("hitSoundVolume")] public float HitSoundVolume { get; set; } = 1f;

    /// <summary>If true, hitting this note damages the player.</summary>
    [JsonPropertyName("isMine")] public bool IsMine { get; set; }

    /// <summary>If true, the note can't be hit and is purely visual.</summary>
    [JsonPropertyName("noHit")] public bool NoHit { get; set; }

    [JsonPropertyName("custom")] public JsonNode? Custom { get; set; }

    [JsonIgnore] public string Id { get; set; } = string.Empty;
}

/// <summary>Port of NoteKindManager: loads note kinds from `data/notekinds/`.</summary>
public sealed class NoteKindManager
{
    public static NoteKindManager Instance { get; } = new();

    public const string AltAnimationKind = "alt-animation";

    private readonly Dictionary<string, NoteKind> _kinds = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, NoteKind> Kinds => _kinds;

    public void LoadAll()
    {
        _kinds.Clear();

        foreach (var id in Paths.FunkinPaths.Instance.ListDataFiles("data/notekinds/", ".json"))
        {
            var json = Paths.FunkinPaths.Instance.ReadText($"data/notekinds/{id}.json");
            if (json is null) continue;

            var kind = Util.Json.Deserialize<NoteKind>(json);
            if (kind is null) continue;

            kind.Id = id;
            _kinds[id] = kind;
        }

        // 'alt-animation' is a built-in kind even without a data file.
        if (!_kinds.ContainsKey(AltAnimationKind))
        {
            _kinds[AltAnimationKind] = new NoteKind
            {
                Id = AltAnimationKind,
                Description = "Alternate animation",
                AnimationSuffix = "alt",
                InvertColor = true,
            };
        }

        Util.Logger.Info($"Loaded {_kinds.Count} note kinds.");
    }

    public NoteKind? GetNoteKind(string? id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _kinds.TryGetValue(id, out var kind) ? kind : null;
    }
}
