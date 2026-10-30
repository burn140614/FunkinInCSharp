using Funkin.Data.Song;
using Funkin.Data.Story;
using Funkin.Flixel;
using Funkin.Play;
using Funkin.Save;
using Funkin.Util;
using Microsoft.Xna.Framework;

namespace Funkin.Game.States;

/// <summary>
/// Port of funkin.play.ResultState: the end-of-song score screen, including the rank and per-judgement
/// tallies, and the story-mode progression to the next song.
/// </summary>
public sealed class ResultState : MenuListState
{
    private readonly string _songId;
    private readonly string _difficulty;
    private readonly string _variation;
    private readonly SaveScoreData _score;
    private readonly string? _weekId;
    private readonly int _weekSongIndex;

    private FlxText _details = null!;

    public ResultState(string songId, string difficulty, string variation, SaveScoreData score, string? weekId, int weekSongIndex)
    {
        _songId = songId;
        _difficulty = difficulty;
        _variation = variation;
        _score = score;
        _weekId = weekId;
        _weekSongIndex = weekSongIndex;
    }

    public override void Create()
    {
        FlxG.BackgroundColor = new Color(14, 16, 24);
        MenuTitle = "RESULTS";
        ItemStartY = 480f;
        ItemSpacing = 60f;
        ItemFontSize = 34;

        var rank = Scoring.CalculateRank(_score);

        _details = new FlxText(0, 200, FunkinGame.GameWidth, BuildDetails(rank), 32)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
        };
        Add(_details);

        bool hasNextSong = HasNextWeekSong();

        if (hasNextSong)
        {
            AddItem("NEXT SONG", () => LoadNextSong(), "Continue the week.");
        }
        else
        {
            AddItem("CONTINUE", () => FlxG.SwitchState(_weekId is null ? new FreeplayState() : new StoryMenuState()),
                "Return to the menus.");
        }

        AddItem("RETRY", () => FlxG.SwitchState(ConversationState.CreateForSong(_songId, _difficulty, _weekId, _weekSongIndex)),
            "Play the song again.");

        AddItem("FREEPLAY", () => FlxG.SwitchState(new FreeplayState()), "Back to the song list.");

        BuildMenuTexts();

        if (CommandLine.IsSmokeTest)
        {
            Logger.Info("SMOKE TEST RESULT: " + BuildDetails(rank).Replace("\n", " | "));
            Logger.Info("Smoke test completed successfully.");
            SaveManager.Instance.Flush(true);
            Environment.Exit(0);
        }

        // Rank jingle.
        FlxG.Sound.PlaySfx(Paths.FunkinPaths.Instance.Sound($"result{rank?.ToDisplayString().Replace(" ", string.Empty).ToLowerInvariant() ?? "good"}"));
    }

    private string BuildDetails(ScoringRank? rank)
    {
        var tallies = _score.Tallies;
        double accuracy = Scoring.TallyCompletion(tallies) * 100;

        return string.Join("\n",
            $"SCORE: {_score.Score:N0}",
            $"RANK: {rank?.ToDisplayString() ?? "-"}",
            $"ACCURACY: {accuracy:0.00}%",
            string.Empty,
            $"SICK {tallies.Sick}   GOOD {tallies.Good}   BAD {tallies.Bad}",
            $"SHIT {tallies.Shit}   MISS {tallies.Missed}   MAX COMBO {tallies.MaxCombo}");
    }

    private bool HasNextWeekSong()
    {
        if (_weekId is null) return false;
        var week = LevelRegistry.Instance.Load(_weekId);
        return week is not null && _weekSongIndex + 1 < week.Songs.Count;
    }

    private void LoadNextSong()
    {
        var week = LevelRegistry.Instance.Load(_weekId!);
        if (week is null) return;

        int nextIndex = _weekSongIndex + 1;
        if (nextIndex >= week.Songs.Count) return;

        FlxG.SwitchState(ConversationState.CreateForSong(week.Songs[nextIndex], _difficulty, week.Id, nextIndex));
    }

    protected override void OnBack() => FlxG.SwitchState(_weekId is null ? new FreeplayState() : new StoryMenuState());
}
