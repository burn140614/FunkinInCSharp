using Funkin.Data.Song;
using Funkin.Play;
using Funkin.Flixel;
using Funkin.Save;
using Funkin.Util;
using Microsoft.Xna.Framework;

namespace Funkin.Game.States;

/// <summary>
/// Port of funkin.ui.freeplay.FreeplayState: a song list with a difficulty selector and the
/// player's best score/rank per song.
/// </summary>
public sealed class FreeplayState : MenuListState
{
    private readonly List<string> _songIds = new();
    private string _difficulty = Constants.DEFAULT_DIFFICULTY;
    private FlxText _difficultyText = null!;
    private FlxText _scoreText = null!;

    public override void Create()
    {
        FlxG.BackgroundColor = new Color(20, 24, 36);
        MenuTitle = "FREEPLAY";
        ItemStartY = 220f;
        ItemSpacing = 52f;
        ItemFontSize = 32;

        var ids = SongRegistry.Instance.ListSongIds();
        if (ids.Count == 0)
        {
            AddItem("(NO SONGS FOUND - CHECK YOUR assets/ FOLDER)", null, "The assets submodule is required for songs.");
        }
        else
        {
            foreach (var id in ids)
            {
                var song = SongRegistry.Instance.LoadSong(id);
                if (song is null) continue;

                _songIds.Add(id);
                AddItem(song.SongName.ToUpperInvariant(), () => PlaySong(id), $"{song.Artist} - {id}");
            }
        }

        AddItem("BACK", () => FlxG.SwitchState(new MainMenuState()), "Return to the main menu.");

        BuildMenuTexts();

        _difficultyText = new FlxText(FunkinGame.GameWidth / 2f + 240, ItemStartY, 320, "", 32)
        {
            ScrollFactor = Vector2.Zero,
            Color = new Color(255, 220, 60),
        };

        _scoreText = new FlxText(FunkinGame.GameWidth / 2f + 240, ItemStartY + 48, 420, "", 22)
        {
            ScrollFactor = Vector2.Zero,
            Color = new Color(190, 190, 200),
        };

        Add(_difficultyText);
        Add(_scoreText);
        UpdateSidePanel();
    }

    private void PlaySong(string songId)
    {
        FlxG.SwitchState(ConversationState.CreateForSong(songId, _difficulty));
    }

    private void ChangeDifficulty(int delta)
    {
        var options = Constants.DEFAULT_DIFFICULTY_LIST;
        int index = Array.IndexOf(options, _difficulty);
        index = (index + delta + options.Length) % options.Length;
        _difficulty = options[index];
        FlxG.Sound.PlaySfx(Paths.FunkinPaths.Instance.Sound("scrollMenu"));
        UpdateSidePanel();
    }

    private void UpdateSidePanel()
    {
        if (_difficultyText is null || _scoreText is null) return;

        _difficultyText.Text = CoolUtil.DifficultyToString(_difficulty).ToUpperInvariant();

        string? currentSong = CurrentSongId();
        if (currentSong is null)
        {
            _scoreText.Text = string.Empty;
            return;
        }

        var best = SaveManager.Instance.Data.GetScore(currentSong, _difficulty, Constants.DEFAULT_VARIATION);
        if (best is null)
        {
            _scoreText.Text = "NO SCORE YET";
            return;
        }

        var rank = Play.Scoring.CalculateRank(best);
        _scoreText.Text = $"BEST: {best.Score:N0}\nRANK: {rank?.ToDisplayString() ?? "-"}\nACCURACY: {Play.Scoring.TallyCompletion(best.Tallies) * 100:0.0}%";
    }

    private string? CurrentSongId()
    {
        // Items are songs followed by the BACK entry.
        return Cursor >= 0 && Cursor < _songIds.Count ? _songIds[Cursor] : null;
    }

    public override void Update(float elapsed)
    {
        if (FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.Left) || FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.A))
        {
            ChangeDifficulty(-1);
        }

        if (FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.Right) || FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.D))
        {
            ChangeDifficulty(1);
        }

        if (FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.Up) || FlxG.Input.JustPressed(Microsoft.Xna.Framework.Input.Keys.Down))
        {
            base.Update(elapsed);
            UpdateSidePanel();
            return;
        }

        base.Update(elapsed);
        UpdateSidePanel();
    }

    protected override void OnBack() => FlxG.SwitchState(new MainMenuState());
}
