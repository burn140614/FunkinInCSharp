using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Funkin.Util;

namespace Funkin.Data.Song;

/// <summary>Serializable parameters attached to a note, used by custom note kinds.</summary>
public sealed class NoteParamData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public JsonNode? Value { get; set; }

    public NoteParamData() { }

    public NoteParamData(string name, JsonNode? value)
    {
        Name = name;
        Value = value;
    }
}

/// <summary>
/// Port of funkin.data.song.SongNoteData.
/// Times are always expressed in milliseconds once parsed; the chart format is normalised on load.
/// </summary>
public sealed class SongNoteData
{
    /// <summary>Timestamp of the note in milliseconds.</summary>
    public float Time { get; set; }

    /// <summary>Lane index: floor(data / strumlineSize) is the strumline, data % strumlineSize the direction.</summary>
    public int Data { get; set; }

    /// <summary>Length of the hold note in milliseconds. 0 for a tap note.</summary>
    public float Length { get; set; }

    /// <summary>Optional note kind id (e.g. 'alt-animation', 'mine').</summary>
    public string? Kind { get; set; }

    public List<NoteParamData> Params { get; set; } = new();

    public SongNoteData() { }

    public SongNoteData(float time, int data, float length = 0, string? kind = null, List<NoteParamData>? parameters = null)
    {
        Time = time;
        Data = data;
        Length = length;
        Kind = string.IsNullOrEmpty(kind) ? null : kind;
        Params = parameters ?? new List<NoteParamData>();
    }

    public int GetDirection(int strumlineSize = 4) => Data % strumlineSize;

    public int GetStrumlineIndex(int strumlineSize = 4) => Data / strumlineSize;

    public bool GetMustHitNote(int strumlineSize = 4) => GetStrumlineIndex(strumlineSize) == 0;

    public bool IsHoldNote => Length > 0;

    public static string BuildDirectionName(int data, int strumlineSize = 4)
    {
        return (data % strumlineSize) switch
        {
            0 => "left",
            1 => "down",
            2 => "up",
            3 => "right",
            _ => "unknown",
        };
    }

    public string GetDirectionName(int strumlineSize = 4) => BuildDirectionName(Data, strumlineSize);

    /// <summary>Position of the note in steps, calculated through the Conductor.</summary>
    public float GetStepTime(Play.Conductor? conductor = null) => (conductor ?? Play.Conductor.Instance).GetTimeInSteps(Time);

    /// <summary>Length of the hold note in steps.</summary>
    public float GetStepLength(Play.Conductor? conductor = null)
    {
        if (Length <= 0) return 0f;
        conductor ??= Play.Conductor.Instance;
        return conductor.GetTimeInSteps(Time + Length) - GetStepTime(conductor);
    }

    public SongNoteData Clone() => new(Time, Data, Length, Kind, Params.Select(p => new NoteParamData(p.Name, p.Value?.DeepClone())).ToList());

    public override string ToString() => $"SongNoteData({Time}ms, lane {Data}, len {Length}{(Kind is null ? string.Empty : $", kind {Kind}")})";
}

/// <summary>Custom converter for the compact chart note format: <c>{"t": 1200, "d": 0, "l": 500, "k": "alt-animation", "p": [{"name":"x","value":1}]}</c>.</summary>
public sealed class SongNoteDataConverter : JsonConverter<SongNoteData>
{
    public override SongNoteData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = JsonNode.Parse(ref reader);
        if (node is not JsonObject obj) throw new JsonException("Expected a note object");

        var note = new SongNoteData
        {
            Time = Json.Get<float>(obj, "t"),
            Data = Json.Get<int>(obj, "d"),
            Length = Json.Get<float>(obj, "l"),
            Kind = Json.Get<string?>(obj, "k"),
        };

        if (note.Kind == string.Empty) note.Kind = null;

        if (obj.TryGetPropertyValue("p", out var parameters) && parameters is JsonArray array)
        {
            foreach (var entry in array)
            {
                if (entry is not JsonObject paramObj) continue;
                note.Params.Add(new NoteParamData(Json.Get<string>(paramObj, "name") ?? string.Empty, paramObj.TryGetPropertyValue("value", out var v) ? v?.DeepClone() : null));
            }
        }

        return note;
    }

    public override void Write(Utf8JsonWriter writer, SongNoteData value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("t", value.Time);
        writer.WriteNumber("d", value.Data);
        if (value.Length != 0) writer.WriteNumber("l", value.Length);
        if (!string.IsNullOrEmpty(value.Kind)) writer.WriteString("k", value.Kind);
        if (value.Params.Count > 0)
        {
            writer.WritePropertyName("p");
            writer.WriteStartArray();
            foreach (var param in value.Params)
            {
                writer.WriteStartObject();
                writer.WriteString("name", param.Name);
                writer.WritePropertyName("value");
                param.Value?.WriteTo(writer, options);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }
}
