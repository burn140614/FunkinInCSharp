using Funkin.Flixel;
using Funkin.Util;
using Microsoft.Xna.Framework;

namespace Funkin.Game.States;

/// <summary>Port of funkin.ui.credits.CreditsState, rendered as a scrolling text column.</summary>
public sealed class CreditsState : FlxState
{
    private static readonly string[] Lines =
    {
        "FRIDAY NIGHT FUNKIN'",
        string.Empty,
        "A C# PORT OF THE HAXEFLIXEL ORIGINAL",
        string.Empty,
        "ORIGINAL GAME",
        "FunkinCrew",
        string.Empty,
        "PROGRAMMING",
        "ninjamuffin99, EliteMasterEric, and contributors",
        string.Empty,
        "ART",
        "PhantomArcade, evilsk8r",
        string.Empty,
        "MUSIC",
        "Kawai Sprite",
        string.Empty,
        "C# PORT",
        "Built on MonoGame + NVorbis",
        string.Empty,
        string.Empty,
        "PRESS ESC TO RETURN",
    };

    private FlxText _text = null!;
    private float _offset;

    public override void Create()
    {
        FlxG.BackgroundColor = new Color(12, 12, 20);
        _text = new FlxText(0, FunkinGame.GameHeight, FunkinGame.GameWidth, string.Join("\n", Lines), 32)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
            Color = new Color(220, 220, 230),
        };
        Add(_text);
    }

    public override void Update(float elapsed)
    {
        _offset += elapsed * 40f;
        _text.Position = new Vector2(0, FunkinGame.GameHeight - _offset);

        if (FlxG.Input.JustPressed(Funkin.Input.GameAction.Back))
        {
            FlxG.SwitchState(new MainMenuState());
            return;
        }

        base.Update(elapsed);
    }
}
