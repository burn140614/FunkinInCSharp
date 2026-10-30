using Funkin.Data.Story;
using Funkin.Flixel;
using Funkin.Util;
using Microsoft.Xna.Framework;

namespace Funkin.Game.States;

/// <summary>Port of funkin.ui.story.StoryMenuState: the week selector.</summary>
public sealed class StoryMenuState : MenuListState
{
    private FlxText _songListText = null!;
    private readonly Dictionary<int, LevelData> _weekByItem = new();

    public override void Create()
    {
        FlxG.BackgroundColor = new Color(249, 207, 81) * 0.35f;
        MenuTitle = "STORY MODE";
        ItemStartY = 200f;
        ItemSpacing = 52f;
        ItemFontSize = 30;

        var weeks = LevelRegistry.Instance.ListVisibleWeeks();
        if (weeks.Count == 0)
        {
            AddItem("(NO WEEKS FOUND)", null, "Week data lives in assets/data/weeks/.");
        }
        else
        {
            int index = 0;
            foreach (var week in weeks)
            {
                _weekByItem[index] = week;
                AddItem(week.Name.ToUpperInvariant(), () => PlayWeek(week), $"{week.Songs.Count} song(s)");
                index++;
            }
        }

        AddItem("BACK", () => FlxG.SwitchState(new MainMenuState()), "Return to the main menu.");

        BuildMenuTexts();

        _songListText = new FlxText(FunkinGame.GameWidth / 2f + 200, ItemStartY, 460, string.Empty, 24)
        {
            ScrollFactor = Vector2.Zero,
            Color = new Color(240, 240, 240),
        };
        Add(_songListText);
        UpdateSongList();
    }

    private void PlayWeek(LevelData week)
    {
        if (week.Songs.Count == 0) return;
        FlxG.SwitchState(ConversationState.CreateForSong(week.Songs[0], Constants.DEFAULT_DIFFICULTY, week.Id, 0));
    }

    private void UpdateSongList()
    {
        if (_songListText is null) return;

        if (!_weekByItem.TryGetValue(Cursor, out var week))
        {
            _songListText.Text = string.Empty;
            return;
        }

        _songListText.Text = "SONGS:\n" + string.Join("\n", week.Songs.Select((id, i) => $"{i + 1}. {id}"));
    }

    public override void Update(float elapsed)
    {
        base.Update(elapsed);
        UpdateSongList();
    }

    protected override void OnBack() => FlxG.SwitchState(new MainMenuState());
}
