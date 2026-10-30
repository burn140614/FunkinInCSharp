using System.Text.Json.Nodes;
using Funkin.Play;
using Funkin.Util;

namespace Funkin.Data.Song;

/// <summary>
/// Port of funkin.play.song.Song: a playable song, composed of one or more variations, each with
/// its own metadata and chart.
/// </summary>
public sealed class Song
{
    public string Id { get; }

    /// <summary>Metadata per variation id.</summary>
    public Dictionary<string, SongMetadata> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Chart data per variation id.</summary>
    public Dictionary<string, SongChartData> Charts { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Song(string id)
    {
        Id = id;
    }

    public IReadOnlyList<string> Variations => Metadata.Keys.ToList();

    public string SongName => Metadata.Values.FirstOrDefault()?.SongName ?? Id;

    public string Artist => Metadata.Values.FirstOrDefault()?.Artist ?? Constants.DEFAULT_ARTIST;

    public string Charter => Metadata.Values.FirstOrDefault()?.Charter ?? Constants.DEFAULT_CHARTER;

    public bool HasVariation(string variation) => Metadata.ContainsKey(variation);

    /// <summary>Lists the difficulties available for a variation, optionally filtered to the "main" list.</summary>
    public List<string> ListDifficulties(string variation = Constants.DEFAULT_VARIATION)
    {
        if (!Metadata.TryGetValue(variation, out var metadata)) return new List<string>();
        return metadata.PlayData.Difficulties.Count > 0
            ? metadata.PlayData.Difficulties.ToList()
            : Constants.DEFAULT_DIFFICULTY_LIST.ToList();
    }

    /// <summary>Builds a fully-resolved difficulty for gameplay, or null when the data is missing.</summary>
    public SongDifficulty? GetDifficulty(string difficulty = Constants.DEFAULT_DIFFICULTY, string variation = Constants.DEFAULT_VARIATION)
    {
        if (!Metadata.TryGetValue(variation, out var metadata)) return null;
        if (!Charts.TryGetValue(variation, out var chart)) return null;
        if (!chart.HasDifficulty(difficulty)) return null;

        return new SongDifficulty(this, difficulty, variation, metadata, chart);
    }

    public bool HasDifficulty(string difficulty, string variation = Constants.DEFAULT_VARIATION)
        => Metadata.ContainsKey(variation) && Charts.TryGetValue(variation, out var chart) && chart.HasDifficulty(difficulty);

    public override string ToString() => $"Song({Id}, {string.Join('/', Variations)})";
}

/// <summary>Port of funkin.play.song.SongDifficulty: a single playable chart of a song.</summary>
public sealed class SongDifficulty
{
    public Song Parent { get; }
    public string Difficulty { get; }
    public string Variation { get; }

    public string SongName { get; }
    public string Artist { get; }
    public string Charter { get; }
    public string Stage { get; }
    public string NoteStyle { get; }
    public SongCharacterData Characters { get; }
    public SongOffsets Offsets { get; }
    public SongPlayData PlayData { get; }
    public List<SongTimeChange> TimeChanges { get; }
    public SongTimeFormat TimeFormat { get; }
    public float ScrollSpeed { get; }
    public int DifficultyRating { get; }
    public bool Looped { get; }
    public int? Divisions { get; }
    public string? Album { get; }
    public string? StickerPack { get; }
    public string? DiscordRpcImage { get; }
    public float PreviewStart { get; }
    public float PreviewEnd { get; }

    public List<SongNoteData> Notes { get; }
    public List<SongEventData> Events { get; }

    public SongDifficulty(Song parent, string difficulty, string variation, SongMetadata metadata, SongChartData chart)
    {
        Parent = parent;
        Difficulty = difficulty;
        Variation = variation;

        SongName = metadata.SongName;
        Artist = metadata.Artist;
        Charter = metadata.Charter ?? Constants.DEFAULT_CHARTER;
        Stage = metadata.PlayData.Stage;
        NoteStyle = metadata.PlayData.NoteStyle;
        Characters = metadata.PlayData.Characters;
        Offsets = metadata.Offsets ?? new SongOffsets();
        PlayData = metadata.PlayData;
        TimeChanges = metadata.TimeChanges.Count > 0
            ? metadata.TimeChanges.Select(tc => tc.Clone()).ToList()
            : new List<SongTimeChange> { SongTimeChange.Default.Clone() };
        TimeFormat = metadata.TimeFormat;
        Looped = metadata.Looped;
        Divisions = metadata.Divisions;
        Album = metadata.PlayData.Album;
        StickerPack = metadata.PlayData.StickerPack;
        DiscordRpcImage = metadata.PlayData.DiscordRpcImage;
        PreviewStart = metadata.PlayData.PreviewStart;
        PreviewEnd = metadata.PlayData.PreviewEnd > 0 ? metadata.PlayData.PreviewEnd : Constants.DEFAULT_PREVIEW_END_TIME;

        ScrollSpeed = chart.GetScrollSpeed(difficulty);
        DifficultyRating = metadata.PlayData.Ratings.TryGetValue(difficulty, out var rating) ? rating : 0;

        // Normalise every note/event timestamp to milliseconds.
        var conductor = new Conductor();
        conductor.MapTimeChanges(TimeChanges);

        Notes = chart.GetNotes(difficulty)
            .Select(n => NormaliseNote(n, conductor))
            .OrderBy(n => n.Time)
            .ToList();

        Events = chart.Events.Select(e => NormaliseEvent(e, conductor)).OrderBy(e => e.Time).ToList();
    }

    private SongNoteData NormaliseNote(SongNoteData note, Conductor conductor)
    {
        var clone = note.Clone();
        clone.Time = ToMilliseconds(clone.Time, conductor);
        if (clone.Length > 0) clone.Length = ToMilliseconds(note.Time + clone.Length, conductor) - clone.Time;
        return clone;
    }

    private SongEventData NormaliseEvent(SongEventData songEvent, Conductor conductor)
    {
        var clone = songEvent.Clone();
        clone.Time = ToMilliseconds(clone.Time, conductor);
        return clone;
    }

    /// <summary>Converts a timestamp in the chart's time format into milliseconds.</summary>
    private float ToMilliseconds(float value, Conductor conductor)
    {
        return TimeFormat switch
        {
            SongTimeFormat.Ticks => conductor.GetStepTimeInMs(value / (Constants.STEPS_PER_BEAT * 3)),
            SongTimeFormat.Float => conductor.GetStepTimeInMs(value),
            _ => value,
        };
    }

    public float GetStartingBpm() => TimeChanges.Count > 0 ? TimeChanges[0].Bpm : Constants.DEFAULT_BPM;

    /// <summary>Sum of notes on the player's side, i.e. the notes that count towards the score.</summary>
    public int CountPlayerNotes(int strumlineSize = 4) => Notes.Count(n => n.GetMustHitNote(strumlineSize));

    public override string ToString() => $"SongDifficulty({Parent.Id}, {Difficulty}, {Variation})";
}
