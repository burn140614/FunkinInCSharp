using Funkin.Flixel;
using Microsoft.Xna.Framework;

namespace Funkin.Play;

/// <summary>
/// Port of the combo/judgement popups (`funkin.play.components.NoteSplash` + the 'Sick!'/'Good!'
/// popups): a short-lived text sprite that floats up and fades out when a note is hit.
/// </summary>
public sealed class JudgementPopup : FlxText
{
    private float _life;
    private float _maxLife = 0.7f;

    public JudgementPopup(float x, float y, string text, Color color)
        : base(x, y, 0, text, 44)
    {
        Alignment = TextAlign.Center;
        ScrollFactor = Vector2.Zero;
        Color = color;
    }

    public static Color ColorForJudgement(string judgement) => judgement switch
    {
        "killer" => new Color(255, 90, 90),
        "sick" => new Color(90, 255, 255),
        "good" => new Color(120, 255, 120),
        "bad" => new Color(255, 220, 90),
        "shit" => new Color(255, 140, 60),
        _ => new Color(200, 200, 200),
    };

    public override void Update(float elapsed)
    {
        base.Update(elapsed);

        _life += elapsed;
        Position = new Vector2(Position.X, Position.Y - elapsed * 60f);
        Alpha = Math.Max(0f, 1f - (_life / _maxLife));

        if (_life >= _maxLife) Destroy();
    }
}
