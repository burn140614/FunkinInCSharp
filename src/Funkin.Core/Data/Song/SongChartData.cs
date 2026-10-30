using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Funkin.Util;

namespace Funkin.Data.Song;

/// <summary>
/// Port of funkin.data.song.SongChartData: the contents of `data/songs/&lt;id&gt;/&lt;id&gt;-chart.json`.
/// Holds every difficulty's notes, the shared event list and per-difficulty scroll speeds.
/// </summary>
public sealed class SongChartData
{
    [JsonPropertyName("version")]
    public string VersionString { get; set; } = "2.2.3";

    [JsonPropertyName("scrollSpeed")]
    public Dictionary<string, float> ScrollSpeed { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("events")]
    public List<SongEventData> Events { get; set; } = new();

    [JsonPropertyName("notes")]
    [JsonConverter(typeof(SongNoteCollectionConverter))]
    public Dictionary<string, List<SongNoteData>> Notes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("generatedBy")]
    public string GeneratedBy { get; set; } = Constants.GENERATED_BY;

    [JsonIgnore]
    public string Variation { get; set; } = Constants.DEFAULT_VARIATION;

    /// <summary>The note data for a difficulty; empty when the difficulty is absent.</summary>
    public List<SongNoteData> GetNotes(string difficulty)
    {
        if (Notes.TryGetValue(difficulty, out var notes)) return notes;

        // Difficulty suffixes are conventionally case-insensitive.
        foreach (var pair in Notes)
        {
            if (string.Equals(pair.Key, difficulty, StringComparison.OrdinalIgnoreCase)) return pair.Value;
        }

        return new List<SongNoteData>();
    }

    public bool HasDifficulty(string difficulty) => Notes.Keys.Any(k => string.Equals(k, difficulty, StringComparison.OrdinalIgnoreCase));

    public float GetScrollSpeed(string difficulty, float fallback = Constants.DEFAULT_SCROLLSPEED)
    {
        if (ScrollSpeed.TryGetValue(difficulty, out var speed)) return speed;
        foreach (var pair in ScrollSpeed)
        {
            if (string.Equals(pair.Key, difficulty, StringComparison.OrdinalIgnoreCase)) return pair.Value;
        }

        return fallback;
    }

    public List<string> ListDifficulties() => Notes.Keys.ToList();

    public SongEventData? GetEvent(string eventKind)
        => Events.FirstOrDefault(e => string.Equals(e.EventKind, eventKind, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Reads the `notes` map, where each difficulty maps to an array of compact note objects.</summary>
public sealed class SongNoteCollectionConverter : JsonConverter<Dictionary<string, List<SongNoteData>>>
{
    private static readonly SongNoteDataConverter NoteConverter = new();

    public override Dictionary<string, List<SongNoteData>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new Dictionary<string, List<SongNoteData>>(StringComparer.OrdinalIgnoreCase);
        var node = JsonNode.Parse(ref reader);
        if (node is not JsonObject obj) return result;

        foreach (var pair in obj)
        {
            var list = new List<SongNoteData>();
            if (pair.Value is JsonArray array)
            {
                foreach (var entry in array)
                {
                    if (entry is null) continue;
                    var note = entry.Deserialize<SongNoteData>(options.WithAddedConverter(NoteConverter));
                    if (note is not null) list.Add(note);
                }
            }

            result[pair.Key] = list;
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, List<SongNoteData>> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var pair in value)
        {
            writer.WritePropertyName(pair.Key);
            writer.WriteStartArray();
            foreach (var note in pair.Value) NoteConverter.Write(writer, note, options);
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }
}

internal static class JsonSerializerOptionsExtensions
{
    /// <summary>Builds a copy of the options with an extra converter, preserving the rest of the configuration.</summary>
    public static JsonSerializerOptions WithAddedConverter(this JsonSerializerOptions options, JsonConverter converter)
    {
        var copy = new JsonSerializerOptions(options);
        copy.Converters.Add(converter);
        return copy;
    }
}
