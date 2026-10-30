using Microsoft.Xna.Framework;

namespace Funkin.Flixel;

/// <summary>Minimal port of flixel.FlxObject: anything with a position and a hitbox.</summary>
public class FlxObject
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public Vector2 ScrollFactor { get; set; } = Vector2.One;
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Visible { get; set; } = true;
    public bool Active { get; set; } = true;
    public bool IsDestroyed { get; private set; }

    public float X
    {
        get => Position.X;
        set => Position = new Vector2(value, Position.Y);
    }

    public float Y
    {
        get => Position.Y;
        set => Position = new Vector2(Position.X, value);
    }

    public Rectangle GetBounds() => new((int)Position.X, (int)Position.Y, Width, Height);

    public virtual void Update(float elapsed)
    {
    }

    public virtual void Destroy() => IsDestroyed = true;
}
