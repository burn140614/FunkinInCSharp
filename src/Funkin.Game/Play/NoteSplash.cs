using Funkin.Flixel;
using Microsoft.Xna.Framework;

namespace Funkin.Play;

/// <summary>
/// Port of funkin.play.notes.NoteSplash (simplified): a burst of colour at the receptor when a note
/// is hit, which scales up and fades out. The original plays a spritesheet animation; this version
/// renders a fading quad so it works with or without note-style artwork.
/// </summary>
public sealed class NoteSplash : FlxSprite
{
    private float _life;
    private const float MaxLife = 0.25f;
    private readonly Vector2 _baseScale;

    public NoteSplash(float x, float y, int lane)
    {
        MakeGraphic(StrumNote.NoteSize, StrumNote.NoteSize, NoteStyleAssets.NoteStyleAssetsSet.LaneColor(lane));
        Position = new Vector2(x, y);
        CenterOrigin();
        ScrollFactor = Vector2.Zero;
        _baseScale = new Vector2(0.6f);
        Scale = _baseScale;
    }

    public override void Update(float elapsed)
    {
        base.Update(elapsed);

        _life += elapsed;
        float t = Math.Clamp(_life / MaxLife, 0f, 1f);
        Scale = _baseScale * (1f + t * 0.8f);
        Alpha = 1f - t;

        if (_life >= MaxLife) Destroy();
    }
}
