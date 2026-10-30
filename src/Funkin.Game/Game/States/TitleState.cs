using Funkin.Flixel;
using Funkin.Util;
using Microsoft.Xna.Framework;

namespace Funkin.Game.States;

/// <summary>
/// Port of funkin.ui.title.TitleState. The original plays an animated intro; this port shows the
/// title logo (or a text stand-in) and waits for the player to continue.
/// </summary>
public sealed class TitleState : FlxState
{
    private FlxText _title = null!;
    private FlxText _prompt = null!;
    private float _timer;
    private bool _introDone;

    public override void Create()
    {
        FlxG.BackgroundColor = new Color(0, 0, 0);

        _title = new FlxText(0, FunkinGame.GameHeight / 2f - 80, FunkinGame.GameWidth, Constants.TITLE.ToUpperInvariant(), 84)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
            Color = new Color(255, 80, 160),
        };

        _prompt = new FlxText(0, FunkinGame.GameHeight / 2f + 40, FunkinGame.GameWidth, "", 32)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
        };

        Add(_title);
        Add(_prompt);

        FlxG.Sound.PlayMusic(Funkin.Paths.FunkinPaths.Instance.Music("freakyMenu"), loop: true);
    }

    public override void Update(float elapsed)
    {
        _timer += elapsed;

        if (!_introDone && _timer > 1.0f)
        {
            _introDone = true;
            _prompt.Text = "PRESS ENTER";
        }

        if (_introDone)
        {
            float alpha = 0.5f + 0.5f * MathF.Sin((float)FlxG.Time * 4f);
            _prompt.Alpha = alpha;

            if (FlxG.Input.JustPressed(Funkin.Input.GameAction.Accept))
            {
                FlxG.SwitchState(new MainMenuState());
                return;
            }
        }

        base.Update(elapsed);
    }
}
