using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Funkin.Flixel;

public enum TextAlign
{
    Left,
    Center,
    Right,
}

/// <summary>
/// Port of flixel.text.FlxText, backed by FontStashSharp dynamic fonts.
/// Supports alignment, colour, outline-free scaling and camera transforms.
/// </summary>
public class FlxText : FlxSprite
{
    private string _text = string.Empty;

    public FlxText(float x = 0, float y = 0, int width = 0, string text = null!, int size = 24)
    {
        Position = new Vector2(x, y);
        FieldWidth = width;
        FontSize = size;
        _text = text ?? string.Empty;
    }

    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    public int FontSize { get; set; } = 24;

    public int FieldWidth { get; set; }

    public TextAlign Alignment { get; set; } = TextAlign.Left;

    public float LineSpacing { get; set; } = 1.0f;

    public bool Bold { get; set; }

    public Vector2 Measure()
    {
        var font = FlxG.Fonts.GetFont(FontSize);
        if (font is null || _text.Length == 0) return Vector2.Zero;
        var size = font.MeasureString(_text);
        return new Vector2(size.X, size.Y);
    }

    public override void Draw(SpriteBatch batch, FlxCamera camera)
    {
        if (!Visible || IsDestroyed || Alpha <= 0f) return;

        var font = FlxG.Fonts.GetFont(FontSize);
        if (font is null || _text.Length == 0) return;

        var textSize = font.MeasureString(_text);
        float scaledWidth = textSize.X * Scale.X;

        float offsetX = Alignment switch
        {
            TextAlign.Center => (FieldWidth > 0 ? FieldWidth : scaledWidth) / 2f - scaledWidth / 2f,
            TextAlign.Right => (FieldWidth > 0 ? FieldWidth : scaledWidth) - scaledWidth,
            _ => 0f,
        };

        var world = Position + new Vector2(offsetX, 0) - Origin;
        var screen = camera.WorldToScreen(world, ScrollFactor);
        float scale = Scale.X * camera.Zoom;

        // FontStashSharp renders at native size, so scale by drawing at a size adjusted for zoom.
        if (Math.Abs(scale - 1f) > 0.01f)
        {
            int scaledSize = Math.Max(1, (int)(FontSize * scale));
            var scaledFont = FlxG.Fonts.GetFont(scaledSize) ?? font;
            var origin = new Vector2(Origin.X * camera.Zoom, Origin.Y * camera.Zoom);
            scaledFont.DrawText(batch, _text, screen - origin * (scale - 1f), Color * Alpha * camera.Alpha);
            return;
        }

        font.DrawText(batch, _text, screen, Color * Alpha * camera.Alpha);
    }
}
