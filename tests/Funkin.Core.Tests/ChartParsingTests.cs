using Funkin.Data.Song;
using Funkin.Play;
using Xunit;

namespace Funkin.Tests;

public class ChartParsingTests
{
    private const string ChartJson = """
    {
      "version": "2.2.3",
      "generatedBy": "chart editor",
      "scrollSpeed": { "easy": 0.8, "normal": 1.4, "hard": 1.9 },
      "events": [
        { "t": 1000, "e": "Zoom Camera", "v": { "zoom": 0.8, "duration": 2 } }
      ],
      "notes": {
        "normal": [
          { "t": 0, "d": 0 },
          { "t": 250, "d": 1 },
          { "t": 500, "d": 2, "l": 500 },
          { "t": 750, "d": 3, "k": "alt-animation" },
          { "t": 1000, "d": 4 }
        ],
        "hard": [
          { "t": 0, "d": 0 }
        ]
      }
    }
    """;

    [Fact]
    public void ParsesNotesEventsAndScrollSpeeds()
    {
        var chart = Util.Json.Deserialize<SongChartData>(ChartJson);
        Assert.NotNull(chart);

        var notes = chart!.GetNotes("normal");
        Assert.Equal(5, notes.Count);
        Assert.Equal(0, notes[0].Data);
        Assert.Equal(500, notes[2].Length);
        Assert.True(notes[2].IsHoldNote);
        Assert.Equal("alt-animation", notes[3].Kind);
        // Lanes 0-3 belong to the player, lanes 4-7 to the opponent.
        Assert.True(notes[0].GetMustHitNote());
        Assert.True(notes[3].GetMustHitNote());
        Assert.False(notes[4].GetMustHitNote());

        Assert.Equal(1.4f, chart.GetScrollSpeed("normal"), 3);
        Assert.Equal(1.9f, chart.GetScrollSpeed("hard"), 3);
        Assert.Equal(Constants_DEFAULT, chart.GetScrollSpeed("missing"), 3);

        var events = chart.Events;
        Assert.Single(events);
        Assert.Equal("Zoom Camera", events[0].EventKind);
        Assert.Equal(0.8, events[0].GetValueAsFloat("zoom"), 4);
    }

    private const float Constants_DEFAULT = Util.Constants.DEFAULT_SCROLLSPEED;

    [Fact]
    public void NoteDataHelperMethodsWork()
    {
        var note = new SongNoteData(1000, 5, 500);
        Assert.Equal(1, note.GetDirection());
        Assert.Equal(1, note.GetStrumlineIndex());
        Assert.False(note.GetMustHitNote());

        var playerNote = new SongNoteData(1000, 2);
        Assert.Equal(2, playerNote.GetDirection());
        Assert.Equal(0, playerNote.GetStrumlineIndex());
        Assert.True(playerNote.GetMustHitNote());
        Assert.Equal("up", playerNote.GetDirectionName());
    }
}
