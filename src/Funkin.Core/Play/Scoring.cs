using Funkin.Save;
using Funkin.Util;

namespace Funkin.Play;

/// <summary>Which system to use when scoring and judging notes.</summary>
public enum ScoringSystem
{
    /// <summary>The scoring system used in Week 6 and older: a step function based on judgement.</summary>
    Legacy,

    /// <summary>The Week 7 system: tighter windows, also a step function.</summary>
    Week7,

    /// <summary>Points Based On Timing (v1): a sigmoid curve based on the timing offset.</summary>
    Pbot1,
}

/// <summary>Port of funkin.play.scoring.Scoring.</summary>
public static class Scoring
{
    public const int Pbot1MaxScore = 500;
    public const float Pbot1ScoringOffset = 54.99f;
    public const float Pbot1ScoringSlope = 0.080f;
    public const float Pbot1MinScore = 9.0f;
    public const int Pbot1MissScore = -100;
    public const float Pbot1PerfectThreshold = 5.0f;
    public const float Pbot1MissThreshold = 160.0f;
    public const float Pbot1KillerThreshold = 12.5f;
    public const float Pbot1SickThreshold = 45.0f;
    public const float Pbot1GoodThreshold = 90.0f;
    public const float Pbot1BadThreshold = 135.0f;
    public const float Pbot1ShitThreshold = 160.0f;

    public const float LegacyHitWindow = (10f / 60f) * 1000f;
    public const float LegacySickThreshold = 0.2f;
    public const float LegacyGoodThreshold = 0.75f;
    public const float LegacyBadThreshold = 0.9f;
    public const float LegacyShitThreshold = 1.0f;
    public const int LegacySickScore = 350;
    public const int LegacyGoodScore = 200;
    public const int LegacyBadScore = 100;
    public const int LegacyShitScore = 50;
    public const int LegacyMissScore = 0;

    public const float Week7HitWindow = (10f / 60f) * 1000f;
    public const float Week7BadThreshold = 0.75f;
    public const float Week7GoodThreshold = 0.55f;
    public const float Week7SickThreshold = 0.2f;
    public const int Week7MissScore = -10;
    public const int Week7ShitScore = 50;
    public const int Week7BadScore = 100;
    public const int Week7GoodScore = 200;
    public const int Week7SickScore = 350;

    /// <summary>Determine the score a note receives for a given timing offset (ms).</summary>
    public static int ScoreNote(double msTiming, ScoringSystem system = ScoringSystem.Pbot1) => system switch
    {
        ScoringSystem.Legacy => ScoreNoteLegacy(msTiming),
        ScoringSystem.Week7 => ScoreNoteWeek7(msTiming),
        _ => ScoreNotePbot1(msTiming),
    };

    /// <summary>Determine the judgement a note receives for a given timing offset (ms).</summary>
    public static string JudgeNote(double msTiming, ScoringSystem system = ScoringSystem.Pbot1) => system switch
    {
        ScoringSystem.Legacy => JudgeNoteLegacy(msTiming),
        ScoringSystem.Week7 => JudgeNoteWeek7(msTiming),
        _ => JudgeNotePbot1(msTiming),
    };

    public static int GetMissScore(ScoringSystem system = ScoringSystem.Pbot1) => system switch
    {
        ScoringSystem.Legacy => LegacyMissScore,
        ScoringSystem.Week7 => Week7MissScore,
        _ => Pbot1MissScore,
    };

    /// <summary>The hit window (in ms) for a system, used for input polling and note movement.</summary>
    public static float GetHitWindow(ScoringSystem system = ScoringSystem.Pbot1) => system switch
    {
        ScoringSystem.Legacy => LegacyHitWindow,
        ScoringSystem.Week7 => Week7HitWindow,
        _ => Pbot1MissThreshold,
    };

    private static int ScoreNotePbot1(double msTiming)
    {
        double absTiming = Math.Abs(msTiming);

        if (absTiming > Pbot1MissThreshold) return Pbot1MissScore;
        if (absTiming < Pbot1PerfectThreshold) return Pbot1MaxScore;

        double factor = 1.0 - (1.0 / (1.0 + Math.Exp(-Pbot1ScoringSlope * (absTiming - Pbot1ScoringOffset))));
        return (int)(Pbot1MaxScore * factor + Pbot1MinScore);
    }

    private static string JudgeNotePbot1(double msTiming)
    {
        double absTiming = Math.Abs(msTiming);

        if (absTiming <= Pbot1SickThreshold) return "sick";
        if (absTiming <= Pbot1GoodThreshold) return "good";
        if (absTiming <= Pbot1BadThreshold) return "bad";
        if (absTiming <= Pbot1ShitThreshold) return "shit";
        return "miss";
    }

    private static int ScoreNoteLegacy(double msTiming)
    {
        double absTiming = Math.Abs(msTiming);

        if (absTiming < LegacyHitWindow * LegacySickThreshold) return LegacySickScore;
        if (absTiming < LegacyHitWindow * LegacyGoodThreshold) return LegacyGoodScore;
        if (absTiming < LegacyHitWindow * LegacyBadThreshold) return LegacyBadScore;
        if (absTiming < LegacyHitWindow * LegacyShitThreshold) return LegacyShitScore;
        return LegacyMissScore;
    }

    private static string JudgeNoteLegacy(double msTiming)
    {
        double absTiming = Math.Abs(msTiming);

        if (absTiming <= LegacyHitWindow * LegacySickThreshold) return "sick";
        if (absTiming <= LegacyHitWindow * LegacyGoodThreshold) return "good";
        if (absTiming <= LegacyHitWindow * LegacyBadThreshold) return "bad";
        if (absTiming <= LegacyHitWindow * LegacyShitThreshold) return "shit";
        return "miss";
    }

    private static int ScoreNoteWeek7(double msTiming)
    {
        double absTiming = Math.Abs(msTiming);

        if (absTiming < Week7HitWindow * Week7SickThreshold) return Week7SickScore;
        if (absTiming < Week7HitWindow * Week7GoodThreshold) return Week7GoodScore;
        if (absTiming < Week7HitWindow * Week7BadThreshold) return Week7BadScore;
        if (absTiming < Week7HitWindow) return Week7ShitScore;
        return 0;
    }

    private static string JudgeNoteWeek7(double msTiming)
    {
        double absTiming = Math.Abs(msTiming);

        if (absTiming <= Week7HitWindow * Week7SickThreshold) return "sick";
        if (absTiming <= Week7HitWindow * Week7GoodThreshold) return "good";
        if (absTiming <= Week7HitWindow * Week7BadThreshold) return "bad";
        if (absTiming <= Week7HitWindow) return "shit";
        return "miss";
    }

    /// <summary>Calculates the completion of a song, from 0 to 1.</summary>
    public static float TallyCompletion(SaveScoreTallyData? tallies)
    {
        if (tallies is null || tallies.TotalNotes <= 0) return 0f;
        return MathUtil.Clamp01((float)(tallies.Sick + tallies.Good - tallies.Missed) / tallies.TotalNotes);
    }

    /// <summary>Calculates the letter rank for a play, or null when the player never played the song.</summary>
    public static ScoringRank? CalculateRank(SaveScoreData? scoreData)
    {
        if (scoreData is null || scoreData.Tallies.TotalNotes == 0) return null;

        if (scoreData.Tallies.Sick == scoreData.Tallies.TotalNotes) return ScoringRank.PerfectGold;

        float completion = TallyCompletion(scoreData.Tallies);

        if (Math.Abs(completion - Constants.RANK_PERFECT_THRESHOLD) < 0.0001f) return ScoringRank.Perfect;
        if (completion >= Constants.RANK_EXCELLENT_THRESHOLD) return ScoringRank.Excellent;
        if (completion >= Constants.RANK_GREAT_THRESHOLD) return ScoringRank.Great;
        if (completion >= Constants.RANK_GOOD_THRESHOLD) return ScoringRank.Good;
        return ScoringRank.Shit;
    }
}

/// <summary>Port of Scoring.ScoringRank.</summary>
public enum ScoringRank
{
    PerfectGold,
    Perfect,
    Excellent,
    Great,
    Good,
    Shit,
}

public static class ScoringRankExtensions
{
    public static string ToDisplayString(this ScoringRank rank) => rank switch
    {
        ScoringRank.PerfectGold => "PERFECT GOLD",
        ScoringRank.Perfect => "PERFECT",
        ScoringRank.Excellent => "EXCELLENT",
        ScoringRank.Great => "GREAT",
        ScoringRank.Good => "GOOD",
        _ => "SHIT",
    };

    /// <summary>Asset name for the rank's letter graphic (matches the original's `rankings/` assets).</summary>
    public static string ToAssetName(this ScoringRank rank) => rank switch
    {
        ScoringRank.PerfectGold => "rankingP",
        ScoringRank.Perfect => "rankingP",
        ScoringRank.Excellent => "rankingE",
        ScoringRank.Great => "rankingG",
        ScoringRank.Good => "rankingG",
        _ => "rankingS",
    };

    /// <summary>Value used for comparisons: higher is better.</summary>
    public static int GetValue(this ScoringRank rank) => rank switch
    {
        ScoringRank.PerfectGold => 6,
        ScoringRank.Perfect => 5,
        ScoringRank.Excellent => 4,
        ScoringRank.Great => 3,
        ScoringRank.Good => 2,
        _ => 1,
    };

    public static bool IsBetterThan(this ScoringRank a, ScoringRank b) => a.GetValue() > b.GetValue();
}
