using System.Text.Json.Serialization;
using Funkin.Data;

namespace Funkin.Data.Character;

public enum CharacterRenderType
{
    Sparrow,
    Packer,
    AnimateAtlas,
}

/// <summary>Port of CharacterData.HealthIconData.</summary>
public sealed class HealthIconData
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("shouldBop")] public bool? ShouldBop { get; set; }
    [JsonPropertyName("scale")] public float? Scale { get; set; }
    [JsonPropertyName("flipX")] public bool? FlipX { get; set; }
    [JsonPropertyName("isPixel")] public bool? IsPixel { get; set; }
    [JsonPropertyName("offsets")] public List<float>? Offsets { get; set; }
}

/// <summary>Port of CharacterData.DeathData.</summary>
public sealed class DeathData
{
    [JsonPropertyName("cameraOffsets")] public List<float>? CameraOffsets { get; set; }
    [JsonPropertyName("cameraZoom")] public float? CameraZoom { get; set; }
    [JsonPropertyName("preTransitionDelay")] public float? PreTransitionDelay { get; set; }
}

/// <summary>
/// Port of funkin.data.character.CharacterData: the contents of `data/characters/&lt;id&gt;.json`.
/// </summary>
public sealed class CharacterData
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.2";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Unknown";

    [JsonPropertyName("renderType")]
    public string RenderTypeString { get; set; } = "sparrow";

    /// <summary>Path (relative to `images/`) of the character's spritesheet.</summary>
    [JsonPropertyName("assetPath")]
    public string AssetPath { get; set; } = string.Empty;

    [JsonPropertyName("scale")]
    public float? Scale { get; set; }

    [JsonPropertyName("healthIcon")]
    public HealthIconData? HealthIcon { get; set; }

    [JsonPropertyName("death")]
    public DeathData? Death { get; set; }

    [JsonPropertyName("offsets")]
    public List<float>? Offsets { get; set; }

    [JsonPropertyName("cameraOffsets")]
    public List<float> CameraOffsets { get; set; } = new() { 0, 0 };

    [JsonPropertyName("isPixel")]
    public bool? IsPixel { get; set; }

    [JsonPropertyName("danceEvery")]
    public float? DanceEvery { get; set; }

    [JsonPropertyName("singTime")]
    public float? SingTime { get; set; }

    [JsonPropertyName("animations")]
    public List<AnimationData> Animations { get; set; } = new();

    [JsonPropertyName("startingAnimation")]
    public string? StartingAnimation { get; set; }

    [JsonPropertyName("flipX")]
    public bool? FlipX { get; set; }

    [JsonPropertyName("applyStageMatrix")]
    public bool? ApplyStageMatrix { get; set; }

    [JsonIgnore]
    public CharacterRenderType RenderType => RenderTypeString.ToLowerInvariant() switch
    {
        "packer" => CharacterRenderType.Packer,
        "animateatlas" or "animate atlas" => CharacterRenderType.AnimateAtlas,
        _ => CharacterRenderType.Sparrow,
    };

    [JsonIgnore] public float ScaleOrDefault => Scale ?? 1f;
    [JsonIgnore] public float DanceEveryOrDefault => DanceEvery ?? 1f;
    [JsonIgnore] public float SingTimeOrDefault => SingTime ?? 1f;
    [JsonIgnore] public bool FlipXOrDefault => FlipX ?? false;
    [JsonIgnore] public bool IsPixelOrDefault => IsPixel ?? false;

    public (float X, float Y) OffsetsPair => Offsets is { Count: >= 2 } ? (Offsets[0], Offsets[1]) : (0f, 0f);

    public AnimationData? GetAnimation(string name)
        => Animations.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

    public string GetHealthIconId() => HealthIcon?.Id ?? "face";
}
