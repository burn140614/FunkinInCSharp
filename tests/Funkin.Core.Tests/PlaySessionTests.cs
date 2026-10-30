using Funkin.Data.Song;
using Funkin.Play;
using Funkin.Util;
using Xunit;

namespace Funkin.Tests;

public class PlaySessionTests
{
    private static SongDifficulty BuildDifficulty(params SongNoteData[] notes)
    {
        var chart = new SongChartData();
        chart.Notes["normal"] = notes.ToList();
        chart.ScrollSpeed["normal"] = 1.0f;

        var metadata = new SongMetadata("test", "tester")
        {
            TimeChanges = { new SongTimeChange(0, 120f, 0) },
        };
        metadata.PlayData.Difficulties.Add("normal");

        return new SongDifficulty(new Song("test"), "normal", "default", metadata, chart);
    }

    private static PlaySession BuildSession(params SongNoteData[] notes)
    {
        var session = new PlaySession(BuildDifficulty(notes), new PlaySessionOptions { GhostTapping = true });
        return session;
    }

    [Fact]
    public void PerfectHitIncrementsComboAndScore()
    {
        var session = BuildSession(new SongNoteData(1000, 0));
        session.Update(1000);
        session.PressLane(0);
        session.Update(1000);

        Assert.Equal(1, session.Combo);
        Assert.Equal(1, session.Tallies.Sick);
        Assert.Equal(Scoring.Pbot1MaxScore, session.Score);
        Assert.True(session.Health > Constants.HEALTH_STARTING);
    }

    [Fact]
    public void EarlyHitIsJudgedGood()
    {
        var session = BuildSession(new SongNoteData(1000, 0));
        session.Update(940);
        session.PressLane(0);
        session.Update(940);

        Assert.Equal(1, session.Tallies.Good);
        Assert.Equal(0, session.Tallies.Missed);
        Assert.True(session.Score > 0);
        Assert.True(session.Score < Scoring.Pbot1MaxScore);
    }

    [Fact]
    public void UnhitNoteCountsAsMissAndDropsHealth()
    {
        var session = BuildSession(new SongNoteData(1000, 0));
        session.Update(1000 + Constants.HIT_WINDOW_MS + 50);

        Assert.Equal(1, session.Tallies.Missed);
        Assert.Equal(0, session.Combo);
        Assert.True(session.Health < Constants.HEALTH_STARTING);
    }

    [Fact]
    public void GhostTappingDoesNotPenaliseEmptyPresses()
    {
        var session = BuildSession(new SongNoteData(5000, 0));
        session.Update(1000);
        session.PressLane(0);
        session.Update(1000);

        Assert.Equal(Constants.HEALTH_STARTING, session.Health, 3);
        Assert.Equal(0, session.Tallies.Missed);
    }

    [Fact]
    public void GhostTappingDisabledCostsHealth()
    {
        var difficulty = BuildDifficulty(new SongNoteData(5000, 0));
        var session = new PlaySession(difficulty, new PlaySessionOptions { GhostTapping = false });
        session.Update(1000);
        session.PressLane(0);
        session.Update(1000);

        Assert.True(session.Health < Constants.HEALTH_STARTING);
    }

    [Fact]
    public void HoldNoteDropIsRecordedWhenReleasedEarly()
    {
        var session = BuildSession(new SongNoteData(1000, 0, 1000));
        session.Update(1000);
        session.PressLane(0);
        session.Update(1000);
        Assert.True(session.Notes[0].Holding);

        session.ReleaseLane(0);
        session.Update(1200);
        Assert.True(session.Notes[0].HoldDropped);
    }

    [Fact]
    public void BotplayHitsEveryNote()
    {
        var difficulty = BuildDifficulty(
            new SongNoteData(1000, 0),
            new SongNoteData(1500, 1),
            new SongNoteData(2000, 2),
            new SongNoteData(2500, 3));

        var session = new PlaySession(difficulty, new PlaySessionOptions { Botplay = true });
        for (int ms = 0; ms <= 3000; ms += 16)
        {
            session.Update(ms);
        }

        Assert.Equal(4, session.Tallies.TotalNotesHit);
        Assert.Equal(4, session.Combo);
        Assert.Equal(0, session.Tallies.Missed);
        Assert.Equal(4 * Scoring.Pbot1MaxScore, session.Score);
    }

    [Fact]
    public void OpponentNotesAreNotScored()
    {
        var session = BuildSession(new SongNoteData(1000, 4), new SongNoteData(1000, 0));
        session.Update(2000);

        Assert.Equal(1, session.TotalPlayerNotes);
        Assert.Equal(1, session.Tallies.Missed);
    }

    [Fact]
    public void NotesAreSpawnedAheadOfTime()
    {
        var session = BuildSession(new SongNoteData(5000, 0));
        int spawned = 0;
        session.OnNoteSpawn += (_, _) => spawned++;

        session.Update(1000);
        Assert.Equal(0, spawned);

        session.Update(4000);
        Assert.Equal(1, spawned);
    }

    [Fact]
    public void SongEndsAfterLastNote()
    {
        var session = BuildSession(new SongNoteData(1000, 0));
        bool ended = false;
        session.OnSongEnd += (_, _) => ended = true;

        session.Update(1000);
        session.PressLane(0);
        session.Update(1000);
        session.Update(5000);

        Assert.True(ended);
        Assert.True(session.IsFinished);
    }

    [Fact]
    public void RankUsesTallies()
    {
        var session = BuildSession(new SongNoteData(1000, 0));
        session.Update(1000);
        session.PressLane(0);
        session.Update(1000);

        Assert.Equal(ScoringRank.PerfectGold, session.GetRank());
    }
}

public class PlaySessionHoldTests
{
    private static PlaySession BuildSession(params Funkin.Data.Song.SongNoteData[] notes)
    {
        var chart = new Funkin.Data.Song.SongChartData();
        chart.Notes["normal"] = notes.ToList();
        chart.ScrollSpeed["normal"] = 1f;

        var metadata = new Funkin.Data.Song.SongMetadata("test", "tester")
        {
            TimeChanges = { new Funkin.Data.Song.SongTimeChange(0, 120f, 0) },
        };
        metadata.PlayData.Difficulties.Add("normal");

        var difficulty = new Funkin.Data.Song.SongDifficulty(new Funkin.Data.Song.Song("test"), "normal", "default", metadata, chart);
        return new PlaySession(difficulty, new PlaySessionOptions { Botplay = true });
    }

    [Fact]
    public void HoldNotesResolveAndTheSongEnds()
    {
        var session = BuildSession(new Funkin.Data.Song.SongNoteData(1000, 0, 1000));
        bool ended = false;
        session.OnSongEnd += (_, _) => ended = true;

        for (int ms = 0; ms <= 4000; ms += 16) session.Update(ms);

        Assert.True(session.Notes[0].Hit);
        Assert.True(session.Notes[0].HoldCompleted);
        Assert.True(session.Notes[0].Resolved);
        Assert.Equal(0, session.Tallies.Missed);
        Assert.True(ended);
    }
}

public class PlaySessionNoteKindTests : IDisposable
{
    private readonly string _root;

    public PlaySessionNoteKindTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "notekinds-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "assets", "data", "notekinds"));

        File.WriteAllText(Path.Combine(_root, "assets", "data", "notekinds", "mine.json"),
            """{ "description": "Mine", "isMine": true, "noHit": false }""");

        File.WriteAllText(Path.Combine(_root, "assets", "data", "notekinds", "bonus.json"),
            """{ "description": "Bonus note", "scoreBonus": 250, "healthGain": 0.1 }""");

        Paths.FunkinPaths.Initialize(_root);
        Play.Notes.NoteKindManager.Instance.LoadAll();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private static PlaySession BuildSession(params Funkin.Data.Song.SongNoteData[] notes)
    {
        var chart = new Funkin.Data.Song.SongChartData();
        chart.Notes["normal"] = notes.ToList();
        chart.ScrollSpeed["normal"] = 1f;

        var metadata = new Funkin.Data.Song.SongMetadata("test", "tester")
        {
            TimeChanges = { new Funkin.Data.Song.SongTimeChange(0, 120f, 0) },
        };
        metadata.PlayData.Difficulties.Add("normal");

        var difficulty = new Funkin.Data.Song.SongDifficulty(new Funkin.Data.Song.Song("test"), "normal", "default", metadata, chart);
        return new PlaySession(difficulty, new PlaySessionOptions { GhostTapping = true });
    }

    [Fact]
    public void MinesDamageThePlayerAndResetCombo()
    {
        var session = BuildSession(
            new Funkin.Data.Song.SongNoteData(1000, 0),
            new Funkin.Data.Song.SongNoteData(1500, 0, 0, "mine"));

        // Register a hit first so the combo is non-zero.
        session.Update(1000);
        session.PressLane(0);
        session.Update(1000);
        Assert.True(session.Combo == 1, $"combo={session.Combo} score={session.Score} sick={session.Tallies.Sick} missed={session.Tallies.Missed} h0={session.Notes[0].Hit} j0={session.Notes[0].Judgement} j1={session.Notes[1].Judgement} health={session.Health}");

        session.Update(1500);
        session.PressLane(0);
        session.Update(1500);

        Assert.Equal(0, session.Combo);
        Assert.Equal("mine", session.Notes[1].Judgement);
        Assert.True(session.Health < Funkin.Util.Constants.HEALTH_STARTING);
        Assert.Equal(1, session.Tallies.Missed);
    }

    [Fact]
    public void CustomKindScoreBonusIsApplied()
    {
        var session = BuildSession(new Funkin.Data.Song.SongNoteData(1000, 0, 0, "bonus"));
        session.Update(1000);
        session.PressLane(0);
        session.Update(1000);

        Assert.Equal(Play.Scoring.Pbot1MaxScore + 250, session.Score);
        Assert.True(session.Health > Funkin.Util.Constants.HEALTH_STARTING);
    }
}
