using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Funkin.Util;

/// <summary>
/// Thin wrapper over System.Text.Json tuned for the loose, mod-authored JSON the game consumes:
/// comments are allowed, unknown members are ignored, and property names are matched case-insensitively.
/// </summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    /// <summary>Loose options used for mod files where keys are dynamic (note kinds, events, params).</summary>
    public static readonly JsonSerializerOptions LooseOptions = new(Options)
    {
        PropertyNamingPolicy = null,
    };

    public static T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException ex)
        {
            Logger.Error($"Failed to parse JSON as {typeof(T).Name}: {ex.Message}");
            return default;
        }
    }

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    /// <summary>Parses a JSON document into a mutable node tree.</summary>
    public static JsonNode? ParseNode(string json)
    {
        try
        {
            return JsonNode.Parse(json, documentOptions: new System.Text.Json.JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
        }
        catch (JsonException ex)
        {
            Logger.Error($"Failed to parse JSON node: {ex.Message}");
            return null;
        }
    }

    /// <summary>Reads a value out of a node without throwing when the path is missing.</summary>
    public static T? Get<T>(JsonNode? node, string key, T? fallback = default)
    {
        if (node is not JsonObject obj || !obj.TryGetPropertyValue(key, out var value) || value is null) return fallback;
        try
        {
            return value.Deserialize<T>(Options) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }
}
