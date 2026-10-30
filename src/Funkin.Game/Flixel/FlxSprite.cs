using Funkin.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Funkin.Flixel;

/// <summary>
/// Port of flixel.FlxSprite: a textured, animated, transformable 2D object.
/// Draw transforms are computed manually per sprite so that per-sprite scroll factors work
/// (Flixel's parallax behaviour).
/// </summary>
public class FlxSprite : FlxObject
{
    public Texture2D? Texture { get; set; }
    public Rectangle? SourceRect { get; set; }

    public Vector2 Scale { get; set; } = Vector2.One;
    public Vector2 Origin { get; set; }
    public Vector2 Offset { get; set; }
    public float Angle { get; set; }
    public float Alpha { get; set; } = 1f;
    public Color Color { get; set; } = Color.White;
    public bool FlipX { get; set; }
    public bool FlipY { get; set; }
    public bool Antialiasing { get; set; } = true;
    public float LayerDepth { get; set; }

    public FlxAnimationController Animation { get; } = new();

    /// <summary>When set, the sprite is drawn with this colour instead of a texture (colour props, hitboxes).</summary>
    public bool IsColoredRectangle { get; set; }

    public bool HasTexture => Texture is not null;

    public FlxSprite() { }

    public FlxSprite(float x, float y)
    {
        Position = new Vector2(x, y);
    }

    public static FlxSprite FromTexture(Texture2D texture, float x = 0, float y = 0)
    {
        var sprite = new FlxSprite(x, y) { Texture = texture };
        sprite.Width = texture.Width;
        sprite.Height = texture.Height;
        sprite.SourceRect = new Rectangle(0, 0, texture.Width, texture.Height);
        return sprite;
    }

    /// <summary>Loads a sprite sheet from `images/&lt;path&gt;.png` (or `&lt;path&gt;.png` when an extension is given).</summary>
    public bool LoadGraphic(string imagePath, bool antialiasing = true)
    {
        var path = imagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            ? Paths.FunkinPaths.Instance.Image(imagePath[..^4])
            : Paths.FunkinPaths.Instance.Image(imagePath);

        var texture = FlxG.Textures.Load(path);
        if (texture is null) return false;

        Texture = texture;
        SourceRect = new Rectangle(0, 0, texture.Width, texture.Height);
        Width = texture.Width;
        Height = texture.Height;
        Antialiasing = antialiasing;
        return true;
    }

    /// <summary>Loads a Sparrow atlas (`images/&lt;path&gt;.png` + `.xml`) and registers every animation described.</summary>
    public FlxAtlasFrames? LoadAtlas(string imagePath)
    {
        string basePath = imagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? imagePath[..^4] : imagePath;
        var png = Paths.FunkinPaths.Instance.Image(basePath);
        var xml = Paths.FunkinPaths.Instance.Xml(basePath);

        var atlas = FlxAtlasFrames.Load(png, xml, FlxG.Textures);
        if (atlas is null) return null;

        Texture = atlas.Texture;
        SourceRect = atlas.Frames.Count > 0 ? atlas.Frames[0].Source : null;
        Width = atlas.Texture.Width;
        Height = atlas.Texture.Height;
        return atlas;
    }

    /// <summary>Builds a solid colour rectangle sprite.</summary>
    public FlxSprite MakeGraphic(int width, int height, Color color)
    {
        Texture = FlxG.Textures.CreateSolid(width, height, color);
        SourceRect = new Rectangle(0, 0, width, height);
        Width = width;
        Height = height;
        IsColoredRectangle = true;
        Color = color;
        return this;
    }

    public void CenterOrigin()
    {
        Origin = new Vector2(Width / 2f, Height / 2f);
    }

    public void SetOriginToCorner() => Origin = Vector2.Zero;

    public void Play(string animationName, bool force = false)
    {
        Animation.Play(animationName, force);
        var frame = Animation.CurrentFrame;
        if (frame is not null) ApplyFrame(frame.Value);
    }

    public bool HasAnimation(string name) => Animation.Has(name);

    private void ApplyFrame(FrameRegion frame)
    {
        Texture = frame.Texture;
        SourceRect = frame.Source;
        Width = frame.Source.Width;
        Height = frame.Source.Height;
        Offset = new Vector2(frame.Offset.X, frame.Offset.Y);
    }

    public override void Update(float elapsed)
    {
        base.Update(elapsed);

        if (X + Velocity.X * elapsed < float.MaxValue) Position += Velocity * elapsed;

        Animation.Update(elapsed);
        var frame = Animation.CurrentFrame;
        if (frame is not null) ApplyFrame(frame.Value);
    }

    /// <summary>Screen position of the sprite, honouring the camera's scroll/zoom.</summary>
    public Vector2 GetScreenPosition(FlxCamera camera)
    {
        var originOffset = Origin;
        var world = Position - originOffset + Offset;
        var screen = camera.WorldToScreen(world, ScrollFactor);
        return screen;
    }

    public virtual void Draw(SpriteBatch batch, FlxCamera camera)
    {
        if (!Visible || IsDestroyed || Texture is null) return;

        var frameOffset = Animation.CurrentFrame is null ? Vector2.Zero : Animation.CurrentFrameOffset;
        if (frameOffset != Vector2.Zero) frameOffset = new Vector2(frameOffset.X, frameOffset.Y);

        var world = Position - Origin + Offset + frameOffset;
        var screen = camera.WorldToScreen(world, ScrollFactor);
        var scale = Scale * camera.Zoom;
        float radians = MathHelper.ToRadians(Angle + camera.Angle);

        var effects = SpriteEffects.None;
        if (FlipX) effects |= SpriteEffects.FlipHorizontally;
        if (FlipY) effects |= SpriteEffects.FlipVertically;

        // Flip the origin so rotations and scales mirror correctly.
        var origin = Vector2.Zero;
        if (Origin != Vector2.Zero)
        {
            origin = FlipX ? new Vector2(Width - Origin.X, Origin.Y) : Origin;
            origin *= camera.Zoom;
        }

        batch.Draw(
            Texture,
            screen,
            SourceRect,
            Color * Alpha * camera.Alpha,
            radians,
            origin,
            scale,
            effects,
            LayerDepth);
    }
}
