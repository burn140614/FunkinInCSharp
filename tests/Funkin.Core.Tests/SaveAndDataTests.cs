using Funkin.Data.Song;
using Funkin.Save;
using Funkin.Util;
using Xunit;

namespace Funkin.Tests;

public class SaveAndDataTests
{
    [Fact]
    public void VersionComparisonFollowsSemver()
    {
        Assert.True(GameVersion.Parse("0.10.0") > GameVersion.Parse("0.8.7"));
        Assert.True(GameVersion.Parse("1.0.0") > GameVersion.Parse("0.99.99"));
        Assert.Equal(0, GameVersion.Parse("2.1").CompareTo(GameVersion.Parse("2.1.0")));
        Assert.True(GameVersion.Parse("1.0.0-rc") < GameVersion.Parse("1.0.0"));
        Assert.False(GameVersion.TryParse("not-a-version", out _));
    }

    [Fact]
    public void SaveDataRoundTripsThroughJson()
    {
        var data = new SaveData();
        data.Options.Downscroll = true;
        data.Options.Framerate = 144;
        data.SetScore("bopeebo", "normal", "default", new SaveScoreData
        {
            Score = 12345,
            Tallies = new SaveScoreTallyData { Sick = 90, Good = 5, Bad = 2, Shit = 1, Missed = 2, TotalNotes = 100 },
        });
        data.WeekCompletion["week1"] = 0.5;

        var json = Json.Serialize(data);
        var reloaded = Json.Deserialize<SaveData>(json);

        Assert.NotNull(reloaded);
        Assert.True(reloaded!.Options.Downscroll);
        Assert.Equal(144, reloaded.Options.Framerate);
        Assert.Equal(12345, reloaded.GetScore("bopeebo", "normal", "default")!.Score);
        Assert.Equal(0.5, reloaded.WeekCompletion["week1"], 3);
    }

    [Fact]
    public void SaveManagerPersistsToDisk()
    {
        string dir = Path.Combine(Path.GetTempPath(), "funkin-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var manager = SaveManager.Instance;
            manager.Initialize(dir);
            manager.Data.Options.Botplay = true;
            manager.MarkDirty();
            manager.Flush();

            Assert.True(File.Exists(Path.Combine(dir, SaveManager.FileName)));

            manager.Initialize(dir);
            Assert.True(manager.Data.Options.Botplay);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SongMetadataTimeFormatParses()
    {
        Assert.Equal(SongTimeFormat.Ticks, SongTimeFormatExtensions.Parse("ticks"));
        Assert.Equal(SongTimeFormat.Float, SongTimeFormatExtensions.Parse("float"));
        Assert.Equal(SongTimeFormat.Milliseconds, SongTimeFormatExtensions.Parse("ms"));
        Assert.Equal(SongTimeFormat.Milliseconds, SongTimeFormatExtensions.Parse(null));
        Assert.Equal("ticks", SongTimeFormat.Ticks.ToJsonValue());
    }

    [Fact]
    public void DialogueParsesBothFormats()
    {
        const string json = """
        {
          "song": "bopeebo",
          "dialogue": [
            "dad: Well well well.",
            { "speaker": "bf", "text": "Beep bop!", "portrait": "bf-chill" }
          ]
        }
        """;

        var conversation = Data.Dialogue.ConversationData.Parse(json);
        Assert.Equal("bopeebo", conversation.SongId);
        Assert.Equal(2, conversation.Lines.Count);
        Assert.Equal("dad", conversation.Lines[0].Speaker);
        Assert.Equal("Well well well.", conversation.Lines[0].Text);
        Assert.Equal("bf-chill", conversation.Lines[1].Portrait);
    }

    [Fact]
    public void TallyDataTracksCombos()
    {
        var tallies = new SaveScoreTallyData();
        tallies.RegisterHit("sick", 1);
        tallies.RegisterHit("sick", 2);
        tallies.RegisterHit("miss", 0);

        Assert.Equal(2, tallies.Sick);
        Assert.Equal(1, tallies.Missed);
        Assert.Equal(2, tallies.MaxCombo);
        Assert.Equal(2, tallies.TotalNotesHit);
    }
}
