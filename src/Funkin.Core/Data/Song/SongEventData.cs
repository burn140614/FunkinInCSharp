using System.Text.Json;
using System.Text.Json.Nodes;

namespace Funkin.Data.Song;

/// <summary>
/// Port of funkin.data.song.SongEventData: an event fired at a point in time during a song.
/// Events can drive stage props, camera zooms, character animations, etc.
/// </summary>
public sealed class SongEventData
{
    /// <summary>Timestamp of the event in milliseconds.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("t")]
    public float Time { get; set; }

    /// <summary>The event kind id, e.g. 'Zoom Camera' or 'Hey!'.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("e")]
    public string EventKind { get; set; } = string.Empty;

    /// <summary>The event payload. Either a plain value or a map of field name to value.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("v")]
    public JsonNode? Value { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool Activated { get; set; }

    public SongEventData() { }

    public SongEventData(float time, string eventKind, JsonNode? value = null)
    {
        Time = time;
        EventKind = eventKind;
        Value = value;
    }

    public float GetStepTime(Play.Conductor? conductor = null) => (conductor ?? Play.Conductor.Instance).GetTimeInSteps(Time);

    /// <summary>Read a field out of the event payload.</summary>
    public T? GetValueAs<T>(string key, T? fallback = default)
    {
        string lower = key.ToLowerInvariant();
        if (Value is JsonObject obj)
        {
            foreach (var pair in obj)
            {
                if (pair.Key.ToLowerInvariant() != lower) continue;
                try
                {
                    return pair.Value is null ? fallback : pair.Value.Deserialize<T>();
                }
                catch
                {
                    return fallback;
                }
            }

            return fallback;
        }

        // Scalar payloads answer to the conventional field name used by simple events.
        if (Value is not null && lower is "value" or "val")
        {
            try
            {
                return Value.Deserialize<T>();
            }
            catch
            {
                return fallback;
            }
        }

        return fallback;
    }

    public double GetValueAsFloat(string key, double fallback = 0.0)
    {
        if (Value is JsonObject obj)
        {
            foreach (var pair in obj)
            {
                if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase) || pair.Value is null) continue;
                if (double.TryParse(pair.Value.ToJsonString().Trim('"'), out var parsed)) return parsed;
            }

            return fallback;
        }

        if (Value is not null && double.TryParse(Value.ToJsonString().Trim('"'), out var scalar)) return scalar;
        return fallback;
    }

    public bool GetValueAsBool(string key, bool fallback = false)
    {
        if (Value is JsonObject obj)
        {
            foreach (var pair in obj)
            {
                if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase) || pair.Value is null) continue;
                if (bool.TryParse(pair.Value.ToJsonString().Trim('"'), out var parsed)) return parsed;
            }

            return fallback;
        }

        return fallback;
    }

    public string GetValueAsString(string key, string fallback = "")
    {
        if (Value is JsonObject obj)
        {
            foreach (var pair in obj)
            {
                if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase) || pair.Value is null) continue;
                return pair.Value.ToJsonString().Trim('"');
            }

            return fallback;
        }

        return Value?.ToJsonString().Trim('"') ?? fallback;
    }

    public SongEventData Clone() => new(Time, EventKind, Value?.DeepClone());

    public override string ToString() => $"SongEventData({Time}ms, {EventKind}: {Value?.ToJsonString()})";
}
