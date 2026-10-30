using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Funkin.Util;

namespace Funkin.Data.Song;

public enum SongTimeFormat
{
    /// <summary>Notes are stored as ticks (48 per beat), as used by the chart editor.</summary>
    Ticks = 0,

    /// <summary>Notes are stored as fractional steps (4 steps per beat).</summary>
    Float = 1,

    /// <summary>Notes are stored as millisecond timestamps.</summary>
    Milliseconds = 2,
}

public static class SongTimeFormatExtensions
{
    public static string ToJsonValue(this SongTimeFormat format) => format switch
    {
        SongTimeFormat.Ticks => "ticks",
        SongTimeFormat.Float => "float",
        _ => "ms",
    };

    public static SongTimeFormat Parse(string? value) => value switch
    {
        "ticks" => SongTimeFormat.Ticks,
        "float" => SongTimeFormat.Float,
        _ => SongTimeFormat.Milliseconds,
    };
}

/// <summary>
/// A single BPM / time signature change inside a song.
/// Port of funkin.data.song.SongData.SongTimeChange.
/// </summary>
public sealed class SongTimeChange
{
    [JsonPropertyName("t")]
    public float TimeStamp { get; set; }

    [JsonPropertyName("b")]
    public float BeatTime { get; set; }

    [JsonPropertyName("bpm")]
    public float Bpm { get; set; } = Constants.DEFAULT_BPM;

    [JsonPropertyName("n")]
    public int TimeSignatureNum { get; set; } = Constants.DEFAULT_TIME_SIGNATURE_NUM;

    [JsonPropertyName("d")]
    public int TimeSignatureDen { get; set; } = Constants.DEFAULT_TIME_SIGNATURE_DEN;

    [JsonPropertyName("bt")]
    public int[] BeatTuplets { get; set; } = { 4, 4, 4, 4 };

    public SongTimeChange() { }

    public SongTimeChange(float timeStamp, float bpm, float beatTime = -1, int num = 4, int den = 4)
    {
        TimeStamp = timeStamp;
        Bpm = bpm;
        BeatTime = beatTime;
        TimeSignatureNum = num;
        TimeSignatureDen = den;
    }

    public static readonly SongTimeChange Default = new(0, Constants.DEFAULT_BPM, 0);

    public SongTimeChange Clone() => new(TimeStamp, Bpm, BeatTime, TimeSignatureNum, TimeSignatureDen)
    {
        BeatTuplets = (int[])BeatTuplets.Clone(),
    };

    public override string ToString() => $"SongTimeChange({TimeStamp}ms, {Bpm}bpm, beat {BeatTime}, {TimeSignatureNum}/{TimeSignatureDen})";
}

/// <summary>Port of SongData.SongOffsets: audio sync offsets for a song.</summary>
public sealed class SongOffsets
{
    [JsonPropertyName("instrumental")]
    public float Instrumental { get; set; }

    [JsonPropertyName("altInstrumentals")]
    public Dictionary<string, float> AltInstrumentals { get; set; } = new();

    [JsonPropertyName("vocals")]
    public Dictionary<string, float> Vocals { get; set; } = new();

    [JsonPropertyName("altVocals")]
    public Dictionary<string, Dictionary<string, float>> AltVocals { get; set; } = new();

    public SongOffsets() { }

    public SongOffsets(float instrumental) => Instrumental = instrumental;
}

/// <summary>Port of SongData.SongCharacterData.</summary>
public sealed class SongCharacterData
{
    [JsonPropertyName("player")]
    public string Player { get; set; } = string.Empty;

    [JsonPropertyName("girlfriend")]
    public string Girlfriend { get; set; } = string.Empty;

    [JsonPropertyName("opponent")]
    public string Opponent { get; set; } = string.Empty;

    [JsonPropertyName("instrumental")]
    public string Instrumental { get; set; } = string.Empty;

    [JsonPropertyName("altInstrumentals")]
    public List<string> AltInstrumentals { get; set; } = new();

    [JsonPropertyName("opponentVocals")]
    public List<string>? OpponentVocals { get; set; }

    [JsonPropertyName("playerVocals")]
    public List<string>? PlayerVocals { get; set; }
}

/// <summary>Port of SongData.SongPlayData: everything needed to launch the song.</summary>
public sealed class SongPlayData
{
    [JsonPropertyName("songVariations")]
    public List<string> SongVariations { get; set; } = new();

    [JsonPropertyName("difficulties")]
    public List<string> Difficulties { get; set; } = new();

    [JsonPropertyName("characters")]
    public SongCharacterData Characters { get; set; } = new();

    [JsonPropertyName("stage")]
    public string Stage { get; set; } = Constants.DEFAULT_STAGE;

    [JsonPropertyName("noteStyle")]
    public string NoteStyle { get; set; } = Constants.DEFAULT_NOTE_STYLE;

    [JsonPropertyName("ratings")]
    public Dictionary<string, int> Ratings { get; set; } = new();

    [JsonPropertyName("album")]
    public string? Album { get; set; }

    [JsonPropertyName("previewStart")]
    public float PreviewStart { get; set; }

    [JsonPropertyName("previewEnd")]
    public float PreviewEnd { get; set; } = Constants.DEFAULT_PREVIEW_END_TIME;

    [JsonPropertyName("discordRPCImage")]
    public string? DiscordRpcImage { get; set; }

    [JsonPropertyName("stickerPack")]
    public string? StickerPack { get; set; }
}

/// <summary>
/// Port of funkin.data.song.SongMetadata: the contents of `data/songs/&lt;song&gt;/&lt;song&gt;-metadata.json`.
/// </summary>
public sealed class SongMetadata
{
    [JsonPropertyName("version")]
    public string VersionString { get; set; } = "2.2.3";

    [JsonPropertyName("songName")]
    public string SongName { get; set; } = Constants.DEFAULT_SONGNAME;

    [JsonPropertyName("artist")]
    public string Artist { get; set; } = Constants.DEFAULT_ARTIST;

    [JsonPropertyName("charter")]
    public string? Charter { get; set; }

    [JsonPropertyName("divisions")]
    public int? Divisions { get; set; }

    [JsonPropertyName("looped")]
    public bool Looped { get; set; }

    [JsonPropertyName("offsets")]
    public SongOffsets? Offsets { get; set; }

    [JsonPropertyName("playData")]
    public SongPlayData PlayData { get; set; } = new();

    [JsonPropertyName("generatedBy")]
    public string GeneratedBy { get; set; } = Constants.GENERATED_BY;

    [JsonPropertyName("timeFormat")]
    public string TimeFormatString { get; set; } = Constants.DEFAULT_TIMEFORMAT.ToJsonValue();

    [JsonPropertyName("timeChanges")]
    public List<SongTimeChange> TimeChanges { get; set; } = new();

    [JsonPropertyName("variation")]
    public string Variation { get; set; } = Constants.DEFAULT_VARIATION;

    [JsonIgnore]
    public GameVersion Version
    {
        get => GameVersion.TryParse(VersionString, out var v) ? v : GameVersion.Zero;
    }

    [JsonIgnore]
    public SongTimeFormat TimeFormat
    {
        get => SongTimeFormatExtensions.Parse(TimeFormatString);
        set => TimeFormatString = value.ToJsonValue();
    }

    public SongMetadata() { }

    public SongMetadata(string songName, string artist, string? charter = null, string variation = Constants.DEFAULT_VARIATION)
    {
        SongName = songName;
        Artist = artist;
        Charter = charter;
        Variation = variation;
    }

    public SongTimeChange GetTimeChangeOrThrow()
    {
        if (TimeChanges.Count == 0) throw new InvalidOperationException($"Song {SongName} has no time changes!");
        return TimeChanges[0];
    }
}
