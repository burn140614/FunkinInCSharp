using Funkin.Flixel;
using Funkin.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Funkin.Play;

/// <summary>
/// Port of funkin.play.notes.StrumNote: one of the four receptors a note scrolls towards.
/// Animated through the static/press/confirm frames of the current note style.
/// </summary>
public sealed class StrumNote : FlxSprite
{
    public int Lane { get; }
    public bool IsPlayer { get; }
    public bool IsPressed { get; private set; }

    private readonly NoteStyleAssets.NoteStyleAssetsSet _style;
    private float _confirmTimer;

    public const int NoteSize = 112;

    public StrumNote(int lane, bool isPlayer, NoteStyleAssets.NoteStyleAssetsSet style)
    {
        Lane = lane;
        IsPlayer = isPlayer;
        _style = style;

        var frame = style.Arrows[lane, 0];
        if (frame is not null)
        {
            Texture = frame.Value.Texture;
            SourceRect = frame.Value.Source;
            Width = frame.Value.Source.Width;
            Height = frame.Value.Source.Height;
            Offset = new Vector2(frame.Value.Offset.X, frame.Value.Offset.Y);
            CenterOrigin();
        }
        else
        {
            // Placeholder arrow: a coloured square, still readable as a receptor.
            MakeGraphic(NoteSize, NoteSize, NoteStyleAssets.NoteStyleAssetsSet.LaneColor(lane) * 0.75f);
            CenterOrigin();
            Scale = new Vector2(0.8f);
        }
    }

    public void SetPressed(bool pressed)
    {
        IsPressed = pressed;
        if (pressed) ShowConfirm();
        else ShowStatic();
    }

    public void ShowStatic()
    {
        if (IsPressed) return;
        ApplyFrame(0);
        Scale = new Vector2(_style.HasArtwork ? 1f : 0.8f);
        Color = Color.White;
    }

    public void ShowPressed()
    {
        ApplyFrame(1);
        Scale = new Vector2(0.9f);
    }

    public void ShowConfirm()
    {
        ApplyFrame(2);
        Color = Color.White;
        Scale = new Vector2(1.1f);
        _confirmTimer = 0.08f;
    }

    private void ApplyFrame(int state)
    {
        var frame = _style.Arrows[Lane, state];
        if (frame is null)
        {
            Color = state == 0 ? Color.White : NoteStyleAssets.NoteStyleAssetsSet.LaneColor(Lane);
            return;
        }

        Texture = frame.Value.Texture;
        SourceRect = frame.Value.Source;
        Offset = new Vector2(frame.Value.Offset.X, frame.Value.Offset.Y);
    }

    public override void Update(float elapsed)
    {
        base.Update(elapsed);

        if (_confirmTimer > 0f)
        {
            _confirmTimer -= elapsed;
            if (_confirmTimer <= 0f) ShowStatic();
        }
    }
}
