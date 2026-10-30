using System.Text.Json.Serialization;

namespace Funkin.Data;

/// <summary>Port of funkin.data.animation.AnimationData, used by characters, stage props and icons.</summary>
public sealed class AnimationData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Prefix of the frames in the spritesheet (Sparrow XML frame names).</summary>
    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Optional alternate spritesheet for this animation, relative to `images/`.</summary>
    [JsonPropertyName("assetPath")]
    public string? AssetPath { get; set; }

    /// <summary>Per-frame offsets applied to the sprite while this animation plays.</summary>
    [JsonPropertyName("offsets")]
    public List<List<float>>? Offsets { get; set; }

    [JsonPropertyName("looped")]
    public bool Looped { get; set; } = true;

    [JsonPropertyName("flipX")]
    public bool FlipX { get; set; }

    [JsonPropertyName("flipY")]
    public bool FlipY { get; set; }

    [JsonPropertyName("frameRate")]
    public int FrameRate { get; set; } = 24;

    /// <summary>Explicit frame indices to use, in order. When null the prefix match order is used.</summary>
    [JsonPropertyName("frameIndices")]
    public List<int>? FrameIndices { get; set; }

    public AnimationData() { }

    public AnimationData(string name, string prefix, int frameRate = 24, bool looped = true)
    {
        Name = name;
        Prefix = prefix;
        FrameRate = frameRate;
        Looped = looped;
    }

    public bool HasOffsets => Offsets is { Count: > 0 };
}
