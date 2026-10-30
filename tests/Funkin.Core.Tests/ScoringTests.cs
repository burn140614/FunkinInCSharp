using Funkin.Play;
using Funkin.Save;
using Xunit;

namespace Funkin.Tests;

public class ScoringTests
{
    [Theory]
    [InlineData(0, "sick")]
    [InlineData(40, "sick")]
    [InlineData(60, "good")]
    [InlineData(120, "bad")]
    [InlineData(150, "shit")]
    [InlineData(400, "miss")]
    public void Pbot1Judgements(double timing, string expected)
    {
        Assert.Equal(expected, Scoring.JudgeNote(timing));
        Assert.Equal(expected, Scoring.JudgeNote(-timing));
    }

    [Fact]
    public void Pbot1PerfectHitsGiveMaxScore()
    {
        Assert.Equal(Scoring.Pbot1MaxScore, Scoring.ScoreNote(0));
        Assert.Equal(Scoring.Pbot1MaxScore, Scoring.ScoreNote(-4.9));
        Assert.Equal(Scoring.Pbot1MissScore, Scoring.ScoreNote(200));
    }

    [Fact]
    public void Pbot1ScoreDecreasesWithTimingError()
    {
        int perfect = Scoring.ScoreNote(6);
        int good = Scoring.ScoreNote(80);
        int bad = Scoring.ScoreNote(130);
        Assert.True(perfect > good);
        Assert.True(good > bad);
        Assert.True(bad > 0);
    }

    [Fact]
    public void LegacyAndWeek7WindowsDiffer()
    {
        Assert.Equal("sick", Scoring.JudgeNote(20, ScoringSystem.Legacy));
        Assert.Equal("good", Scoring.JudgeNote(80, ScoringSystem.Legacy));
        Assert.Equal("shit", Scoring.JudgeNote(160, ScoringSystem.Legacy));

        Assert.Equal("good", Scoring.JudgeNote(80, ScoringSystem.Week7));
        Assert.Equal("miss", Scoring.JudgeNote(200, ScoringSystem.Week7));
    }

    [Fact]
    public void PerfectRankRequiresFullComboOfSickNotes()
    {
        var gold = new SaveScoreData { Tallies = new SaveScoreTallyData { Sick = 100, TotalNotes = 100 } };
        Assert.Equal(ScoringRank.PerfectGold, Scoring.CalculateRank(gold));

        var perfect = new SaveScoreData { Tallies = new SaveScoreTallyData { Sick = 99, Good = 1, TotalNotes = 100 } };
        Assert.Equal(ScoringRank.Perfect, Scoring.CalculateRank(perfect));

        var great = new SaveScoreData { Tallies = new SaveScoreTallyData { Sick = 80, Good = 10, Bad = 5, Missed = 5, TotalNotes = 100 } };
        Assert.Equal(ScoringRank.Great, Scoring.CalculateRank(great));

        Assert.Null(Scoring.CalculateRank(null));
        Assert.Null(Scoring.CalculateRank(new SaveScoreData()));
    }
}
