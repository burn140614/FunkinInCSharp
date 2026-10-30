using Funkin.Flixel;
using Funkin.Util;
using Microsoft.Xna.Framework;

namespace Funkin.Play;

/// <summary>
/// Port of funkin.play.HealthIcon + the health bar: a left/right divided bar driven by the player's
/// health (0..2, 1 = even), with icons that bounce on each beat.
/// </summary>
public sealed class HealthBar : FlxState
{
    private readonly FlxSprite _left;
    private readonly FlxSprite _right;
    private readonly FlxSprite _iconPlayer;
    private readonly FlxSprite _iconOpponent;

    public const int BarWidth = 1200;
    public const int BarHeight = 24;

    public float BarScale { get; set; } = 1f;

    public HealthBar(string playerIcon, string opponentIcon)
    {
        _left = new FlxSprite(0, 0).MakeGraphic(BarWidth, BarHeight, new Color(Constants.COLOR_HEALTH_BAR_RED));
        _right = new FlxSprite(0, 0).MakeGraphic(BarWidth, BarHeight, new Color(Constants.COLOR_HEALTH_BAR_GREEN));

        _iconPlayer = LoadIcon(playerIcon);
        _iconOpponent = LoadIcon(opponentIcon);

        Add(_left);
        Add(_right);
        Add(_iconPlayer);
        Add(_iconOpponent);
    }

    private static FlxSprite LoadIcon(string id)
    {
        var sprite = new FlxSprite();
        var atlas = sprite.LoadAtlas($"icons/icon-{id}");
        if (atlas is null || atlas.Frames.Count == 0)
        {
            sprite.MakeGraphic(80, 80, new Color(200, 200, 200));
            return sprite;
        }

        var frame = atlas.Frames[0];
        sprite.Texture = frame.Texture;
        sprite.SourceRect = frame.Source;
        sprite.Width = frame.Source.Width;
        sprite.Height = frame.Source.Height;
        sprite.CenterOrigin();
        return sprite;
    }

    /// <summary>Lays the bar out for the current screen size.</summary>
    public void Layout(int screenWidth, int screenHeight)
    {
        float width = BarWidth * BarScale;
        float height = BarHeight * BarScale;
        float x = (screenWidth - width) / 2f;
        float y = screenHeight - height - 24;

        _left.Position = new Vector2(x, y);
        _left.Scale = new Vector2(BarScale, BarScale);
        _right.Position = new Vector2(x, y);
        _right.Scale = new Vector2(BarScale, BarScale);

        _iconOpponent.Position = new Vector2(x - 40, y + height / 2f);
        _iconPlayer.Position = new Vector2(x + width + 40, y + height / 2f);
        _iconOpponent.Scale = new Vector2(0.6f);
        _iconPlayer.Scale = new Vector2(0.6f);
        _iconPlayer.FlipX = true;
    }

    /// <summary>Updates the divided bar from the player's health (0..2).</summary>
    public void SetHealth(float health)
    {
        float ratio = MathUtil.Clamp(health / Constants.HEALTH_MAX, 0f, 1f);
        float width = BarWidth * BarScale;

        // The player's icon grows while they are winning, the opponent's while they are losing,
        // mirroring the original's icon swapping behaviour.
        bool playerWinning = ratio > 0.5f;
        _iconPlayer.Alpha = playerWinning ? 1f : 0.55f;
        _iconOpponent.Alpha = playerWinning ? 0.55f : 1f;

        _right.Scale = new Vector2(Math.Max(0.0001f, ratio) * BarScale, BarScale);
        _right.Position = new Vector2(_left.Position.X + width * (1 - ratio), _left.Position.Y);
    }

    /// <summary>Bounces the icons on a beat, as the original does.</summary>
    public void OnBeat()
    {
        _iconPlayer.Scale = new Vector2(0.7f);
        _iconOpponent.Scale = new Vector2(0.7f);
    }

    public override void Update(float elapsed)
    {
        base.Update(elapsed);

        // Ease the icons back to their resting scale.
        _iconPlayer.Scale = Vector2.Lerp(_iconPlayer.Scale, new Vector2(0.6f), Math.Min(1f, elapsed * 8f));
        _iconOpponent.Scale = Vector2.Lerp(_iconOpponent.Scale, new Vector2(0.6f), Math.Min(1f, elapsed * 8f));
    }
}
