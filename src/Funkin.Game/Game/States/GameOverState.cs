using Funkin.Flixel;
using Microsoft.Xna.Framework;

namespace Funkin.Game.States;

/// <summary>
/// Port of funkin.play.GameOverSubState: shown when the player runs out of health, offering a retry
/// or a way back to the menus.
/// </summary>
public sealed class GameOverState : MenuListState
{
    private readonly PlayState _playState;

    public GameOverState(PlayState playState)
    {
        _playState = playState;
        BlocksParentUpdate = true;
    }

    public override void Create()
    {
        FlxG.BackgroundColor = new Color(0, 0, 0);

        MenuTitle = "GAME OVER";
        ItemStartY = 300f;
        ItemSpacing = 70f;
        ItemFontSize = 40;

        AddItem("RETRY", () =>
        {
            FlxG.CloseSubState(this);
            _playState.RestartSong();
        }, "Try the song again.");

        AddItem("QUIT TO FREEPLAY", () =>
        {
            FlxG.CloseSubState(this);
            _playState.ExitToFreeplay();
        }, "Give up and go back.");

        BuildMenuTexts();
        FlxG.Sound.PlaySfx(Funkin.Paths.FunkinPaths.Instance.Sound("gameOver"));
    }

    // Enter retries and Escape quits, both handled by the base menu state.
}
