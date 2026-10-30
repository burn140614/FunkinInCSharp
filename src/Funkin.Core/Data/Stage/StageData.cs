using System.Text.Json.Serialization;
using Funkin.Data;

namespace Funkin.Data.Stage;

/// <summary>Port of StageData.StageDataCharacter.</summary>
public sealed class StageCharacterData
{
    [JsonPropertyName("zIndex")] public int ZIndex { get; set; }
    [JsonPropertyName("position")] public List<float> Position { get; set; } = new() { 0, 0 };
    [JsonPropertyName("scale")] public float Scale { get; set; } = 1f;
    [JsonPropertyName("cameraOffsets")] public List<float>? CameraOffsets { get; set; }
    [JsonPropertyName("scroll")] public List<float> Scroll { get; set; } = new() { 1, 1 };
    [JsonPropertyName("alpha")] public float Alpha { get; set; } = 1f;
    [JsonPropertyName("angle")] public float Angle { get; set; }

    [JsonIgnore] public float X => Position.Count > 0 ? Position[0] : 0f;
    [JsonIgnore] public float Y => Position.Count > 1 ? Position[1] : 0f;
    [JsonIgnore] public float ScrollX => Scroll.Count > 0 ? Scroll[0] : 1f;
    [JsonIgnore] public float ScrollY => Scroll.Count > 1 ? Scroll[1] : 1f;
}

/// <summary>Port of StageData.StageDataCharacters.</summary>
public sealed class StageCharactersData
{
    [JsonPropertyName("bf")] public StageCharacterData Bf { get; set; } = new();
    [JsonPropertyName("dad")] public StageCharacterData Dad { get; set; } = new();
    [JsonPropertyName("gf")] public StageCharacterData Gf { get; set; } = new();

    public StageCharacterData Get(string id) => id.ToLowerInvariant() switch
    {
        "bf" or "boyfriend" or "player" => Bf,
        "gf" or "girlfriend" => Gf,
        _ => Dad,
    };
}

/// <summary>Port of StageData.StageDataProp.</summary>
public sealed class StagePropData
{
    [JsonPropertyName("name")] public string? Name { get; set; }

    /// <summary>Either a spritesheet path relative to `images/`, or a colour like `#ff0000`.</summary>
    [JsonPropertyName("assetPath")] public string AssetPath { get; set; } = string.Empty;

    [JsonPropertyName("position")] public List<float> Position { get; set; } = new() { 0, 0 };
    [JsonPropertyName("zIndex")] public int ZIndex { get; set; }
    [JsonPropertyName("isPixel")] public bool IsPixel { get; set; }
    [JsonPropertyName("flipX")] public bool FlipX { get; set; }
    [JsonPropertyName("flipY")] public bool FlipY { get; set; }

    /// <summary>Scale can be either a single float or a [x, y] pair in the JSON.</summary>
    [JsonPropertyName("scale")] public List<float>? Scale { get; set; }

    [JsonPropertyName("alpha")] public float Alpha { get; set; } = 1f;
    [JsonPropertyName("danceEvery")] public float DanceEvery { get; set; }
    [JsonPropertyName("scroll")] public List<float> Scroll { get; set; } = new() { 1, 1 };
    [JsonPropertyName("animations")] public List<AnimationData> Animations { get; set; } = new();
    [JsonPropertyName("startingAnimation")] public string? StartingAnimation { get; set; }
    [JsonPropertyName("animType")] public string AnimType { get; set; } = "sparrow";
    [JsonPropertyName("angle")] public float Angle { get; set; }
    [JsonPropertyName("blend")] public string Blend { get; set; } = string.Empty;
    [JsonPropertyName("color")] public string Color { get; set; } = "#FFFFFF";

    [JsonIgnore] public float X => Position.Count > 0 ? Position[0] : 0f;
    [JsonIgnore] public float Y => Position.Count > 1 ? Position[1] : 0f;
    [JsonIgnore] public float ScaleX => Scale is { Count: > 0 } ? Scale[0] : 1f;
    [JsonIgnore] public float ScaleY => Scale is { Count: > 1 } ? Scale[1] : ScaleX;
    [JsonIgnore] public float ScrollX => Scroll.Count > 0 ? Scroll[0] : 1f;
    [JsonIgnore] public float ScrollY => Scroll.Count > 1 ? Scroll[1] : 1f;

    /// <summary>True when `assetPath` is a colour rectangle rather than a spritesheet.</summary>
    [JsonIgnore] public bool IsColorProp => AssetPath.StartsWith('#');
}

/// <summary>
/// Port of funkin.data.stage.StageData: the contents of `data/stages/&lt;id&gt;.json`.
/// </summary>
public sealed class StageData
{
    [JsonPropertyName("version")] public string Version { get; set; } = "1.0.3";
    [JsonPropertyName("name")] public string Name { get; set; } = "Unknown";
    [JsonPropertyName("props")] public List<StagePropData> Props { get; set; } = new();
    [JsonPropertyName("characters")] public StageCharactersData Characters { get; set; } = new();
    [JsonPropertyName("cameraZoom")] public float? CameraZoom { get; set; }
    [JsonPropertyName("directory")] public string? Directory { get; set; }

    [JsonIgnore] public float CameraZoomOrDefault => CameraZoom ?? 1f;

    public StagePropData? GetProp(string name)
        => Props.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
}
