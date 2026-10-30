using Funkin.Flixel;
using Funkin.Graphics;
using Microsoft.Xna.Framework;

namespace Funkin.Play;

/// <summary>
/// The visual representation of a note: the arrow head plus, for hold notes, a sustain trail.
/// Positions are driven by <see cref="PlaySession.GetNoteYOffset"/> so the view and simulation agree.
/// </summary>
public sealed class NoteSprite : FlxSprite
{
    public PlayNote Note { get; }
    public int Lane { get; }
    public bool IsPlayer { get; }

    public FlxSprite? Sustain { get; private set; }

    private readonly NoteStyleAssets.NoteStyleAssetsSet _style;

    public NoteSprite(PlayNote note, bool isPlayer, NoteStyleAssets.NoteStyleAssetsSet style)
    {
        Note = note;
        Lane = note.Lane;
        IsPlayer = isPlayer;
        _style = style;

        var frame = style.Arrows[note.Lane, 0];
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
            MakeGraphic(StrumNote.NoteSize, StrumNote.NoteSize, NoteStyleAssets.NoteStyleAssetsSet.LaneColor(note.Lane));
            CenterOrigin();
            Scale = new Vector2(0.8f);
        }

        // Notes belonging to the opponent are dimmed, matching the original's shader.
        if (!isPlayer) Color = new Color(0.65f, 0.65f, 0.65f);

        if (note.IsHold) BuildSustain();
    }

    private void BuildSustain()
    {
        var frame = _style.Sustains[Lane, 0];
        if (frame is not null)
        {
            Sustain = new FlxSprite
            {
                Texture = frame.Value.Texture,
                SourceRect = frame.Value.Source,
                Width = frame.Value.Source.Width,
                Height = frame.Value.Source.Height,
            };
        }
        else
        {
            Sustain = new FlxSprite().MakeGraphic(24, 16, NoteStyleAssets.NoteStyleAssetsSet.LaneColor(Lane) * 0.8f);
        }

        if (!IsPlayer) Sustain.Color = new Color(0.65f, 0.65f, 0.65f);
    }

    /// <summary>Updates the sprite's screen-space placement for the current song position.</summary>
    public void UpdatePlacement(PlaySession session, StrumNote receptor)
    {
        float yOffset = session.GetNoteYOffset(Note, session.SongPosition);

        var receptorPosition = receptor.Position;
        Position = receptorPosition + new Vector2(0, yOffset);

        if (Sustain is null) return;

        float lengthPixels = Note.Length * session.PixelsPerMs;
        int direction = session.Options.Downscroll ? 1 : -1;
        int height = Math.Max(1, (int)lengthPixels);

        Sustain.Position = Position + new Vector2(0, direction > 0 ? 0 : -lengthPixels);
        Sustain.Scale = new Vector2(Sustain.SourceRect?.Width ?? 24, height / (float)Math.Max(1, Sustain.SourceRect?.Height ?? 16));
        Sustain.Visible = !Note.Hit;
    }
}
