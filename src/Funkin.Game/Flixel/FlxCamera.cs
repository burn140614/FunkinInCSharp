using Microsoft.Xna.Framework;

namespace Funkin.Flixel;

/// <summary>
/// Port of flixel.FlxCamera, limited to what a 2D rhythm game needs:
/// scroll, zoom, rotation, shake, follow-with-lerp and bounds.
/// </summary>
public sealed class FlxCamera
{
    public float X { get; set; }
    public float Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Camera scroll position in world coordinates (top-left of the view before zoom).</summary>
    public Vector2 Scroll { get; set; }

    private float _zoom = 1f;

    public float Zoom
    {
        get => _zoom;
        set => _zoom = Math.Clamp(value, 0.01f, 20f);
    }

    /// <summary>Rotation in degrees.</summary>
    public float Angle { get; set; }

    public float Alpha { get; set; } = 1f;

    public Color BackgroundColor { get; set; } = Color.Black;

    public FlxObject? Target { get; set; }

    public Vector2 TargetOffset { get; set; }

    /// <summary>How quickly the camera catches up to its target, in seconds (0 = instant).</summary>
    public float Lerp { get; set; }

    public Rectangle? Bounds { get; set; }

    public bool FollowBounds { get; set; } = true;

    public Vector2 Shake { get; private set; }

    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeTimer;
    private readonly Random _random = new();

    public FlxCamera(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>Center of the view in world coordinates.</summary>
    public Vector2 Center => Scroll + new Vector2(Width / 2f, Height / 2f);

    public void ShakeFor(float intensity, float duration)
    {
        _shakeIntensity = intensity;
        _shakeDuration = duration;
        _shakeTimer = duration;
    }

    public void Update(float elapsed)
    {
        if (Target is not null)
        {
            var targetPosition = Target.Position + TargetOffset;
            var desiredScroll = targetPosition - new Vector2(Width / 2f, Height / 2f);

            if (Lerp <= 0f)
            {
                Scroll = desiredScroll;
            }
            else
            {
                float t = Math.Clamp(elapsed / Lerp, 0f, 1f);
                Scroll = Vector2.Lerp(Scroll, desiredScroll, t);
            }
        }

        if (Bounds is { } bounds && FollowBounds)
        {
            // Keep the visible area inside the stage bounds where possible.
            float viewWidth = Width / Zoom;
            float viewHeight = Height / Zoom;

            float maxX = Math.Max(bounds.X, bounds.Right - viewWidth);
            float maxY = Math.Max(bounds.Y, bounds.Bottom - viewHeight);

            Scroll = new Vector2(
                Math.Clamp(Scroll.X + (Width - viewWidth) / 2f, bounds.X, maxX),
                Math.Clamp(Scroll.Y + (Height - viewHeight) / 2f, bounds.Y, maxY));
        }

        if (_shakeTimer > 0f)
        {
            _shakeTimer -= elapsed;
            float amount = _shakeIntensity * (_shakeDuration <= 0f ? 0f : Math.Max(0f, _shakeTimer / _shakeDuration));
            Shake = new Vector2(
                (float)((_random.NextDouble() * 2 - 1) * amount),
                (float)((_random.NextDouble() * 2 - 1) * amount));

            if (_shakeTimer <= 0f) Shake = Vector2.Zero;
        }
    }

    /// <summary>Converts a world position into screen space, applying scroll, zoom and shake.</summary>
    public Vector2 WorldToScreen(Vector2 worldPosition, Vector2 scrollFactor)
    {
        var center = new Vector2(Width / 2f, Height / 2f);
        var relative = worldPosition - (Scroll * scrollFactor) + Shake;
        return ((relative - center) * Zoom) + center + new Vector2(X, Y);
    }
}
