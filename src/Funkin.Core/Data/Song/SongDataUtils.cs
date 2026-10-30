namespace Funkin.Data.Song;

/// <summary>Port of funkin.data.song.SongDataUtils.</summary>
public static class SongDataUtils
{
    /// <summary>Returns a new note array with every timestamp shifted by <paramref name="offset"/> ms.</summary>
    public static List<SongNoteData> OffsetSongNoteData(IEnumerable<SongNoteData> notes, float offset)
    {
        return notes.Select(note => new SongNoteData(note.Time + offset, note.Data, note.Length, note.Kind, note.Params)).ToList();
    }

    public static List<SongEventData> OffsetSongEventData(IEnumerable<SongEventData> events, float offset)
        => events.Select(e => new SongEventData(e.Time + offset, e.EventKind, e.Value?.DeepClone())).ToList();

    public static List<SongNoteData> ClampSongNoteData(IEnumerable<SongNoteData> notes, float startTime, float endTime)
        => notes.Where(n => n.Time >= startTime && n.Time <= endTime).ToList();

    public static List<SongEventData> ClampSongEventData(IEnumerable<SongEventData> events, float startTime, float endTime)
        => events.Where(e => e.Time >= startTime && e.Time <= endTime).ToList();

    /// <summary>Removes a subset of notes from a note array.</summary>
    public static List<SongNoteData> SubtractSongNoteData(IEnumerable<SongNoteData> notes, IEnumerable<SongNoteData> toRemove)
    {
        var remove = toRemove.ToHashSet();
        return notes.Where(n => !remove.Contains(n)).ToList();
    }

    public static void SortTimeChanges(List<SongTimeChange> timeChanges)
    {
        timeChanges.Sort((a, b) => a.TimeStamp.CompareTo(b.TimeStamp));
    }

    /// <summary>Returns the total duration of a chart in ms (last note, event, or fallback).</summary>
    public static float GetChartDuration(SongDifficulty difficulty)
    {
        float lastNote = difficulty.Notes.Count > 0 ? difficulty.Notes.Max(n => n.Time + n.Length) : 0f;
        float lastEvent = difficulty.Events.Count > 0 ? difficulty.Events.Max(e => e.Time) : 0f;
        return Math.Max(lastNote, lastEvent);
    }
}
