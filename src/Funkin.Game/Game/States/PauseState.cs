using Funkin.Flixel;
using Funkin.Save;
using Microsoft.Xna.Framework;

namespace Funkin.Game.States;

/// <summary>Port of funkin.play.PauseSubState: resume / restart / exit, drawn over the frozen song.</summary>
public sealed class PauseState : MenuListState
{
    private readonly PlayState _playState;

    public PauseState(PlayState playState)
    {
        _playState = playState;
        BlocksParentUpdate = true;
    }

    public override void Create()
    {
        FlxG.BackgroundColor = new Color(0, 0, 0);

        MenuTitle = "PAUSED";
        ItemStartY = 260f;
        ItemSpacing = 64f;
        ItemFontSize = 38;

        AddItem("RESUME", () => Close(), "Continue the song.");
        AddItem("RESTART SONG", () =>
        {
            FlxG.CloseSubState(this);
            _playState.RestartSong();
        }, "Start the song over.");
        AddItem("OPTIONS", () =>
        {
            // Leaving to the options menu destroys the song state, so remember where to come back to.
            FlxG.CloseSubState(this);
            FlxG.SwitchState(new OptionsState(() => new MainMenuState()));
        }, "Adjust settings (this ends the current song).");
        AddItem("QUIT TO FREEPLAY", () =>
        {
            FlxG.CloseSubState(this);
            _playState.ExitToFreeplay();
        }, "Abandon the song.");

        BuildMenuTexts();
    }

    private void Close()
    {
        FlxG.CloseSubState(this);
        _playState.ClosePause();
    }

    public override void Update(float elapsed)
    {
        if (FlxG.Input.JustPressed(Funkin.Input.GameAction.Pause))
        {
            Close();
            return;
        }

        base.Update(elapsed);
    }

    protected override void OnBack() => Close();
}
