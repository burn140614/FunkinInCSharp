using Funkin.Data.Character;
using Funkin.Flixel;
using Funkin.Util;
using Microsoft.Xna.Framework;

namespace Funkin.Play;

/// <summary>
/// Port of funkin.play.character.BaseCharacter: a dancing/singing character sprite driven by its
/// JSON animation data. Falls back to a coloured placeholder when the spritesheet is unavailable so
/// the game remains playable without the art assets checked out.
/// </summary>
public class CharacterSprite : FlxSprite
{
    public CharacterData Data { get; }
    public string CharacterId { get; }

    /// <summary>0 = player, 1 = opponent (used to decide which strumline drives the singing).</summary>
    public bool IsPlayer { get; }

    public float DanceEvery { get; set; }
    public float SingTime { get; set; }

    public bool IsSinging { get; private set; }

    public bool IsHolding { get; set; }

    private double _singTimer;
    private bool _idleAlternate;

    public string IdleSuffix => _idleAlternate ? "-alt" : string.Empty;

    public CharacterSprite(string characterId, bool isPlayer)
    {
        CharacterId = characterId;
        IsPlayer = isPlayer;
        Data = CharacterRegistry.Instance.GetOrFallback(characterId);
        DanceEvery = Math.Max(0.25f, Data.DanceEveryOrDefault);
        SingTime = Math.Max(0.25f, Data.SingTimeOrDefault);

        BuildVisual();
    }

    private void BuildVisual()
    {
        var atlas = LoadAtlas(Data.AssetPath);
        if (atlas is null)
        {
            // Placeholder so the gameplay loop still reads position and animation state.
            MakeGraphic(180, 320, IsPlayer ? new Color(80, 200, 255) : new Color(230, 80, 120));
            Logger.Warn($"Character '{CharacterId}' has no spritesheet; using a placeholder.");
            BuildFallbackAnimations();
            return;
        }

        foreach (var animation in Data.Animations)
        {
            var frames = animation.FrameIndices is { Count: > 0 }
                ? atlas.GetFramesByName(animation.FrameIndices.Select(i => $"{animation.Prefix}{i:D4}"))
                : atlas.GetFramesByPrefix(animation.Prefix);

            if (frames.Count == 0)
            {
                frames = atlas.GetFramesByPrefix(animation.Name);
            }

            if (frames.Count == 0) continue;

            var built = Animation.Add(animation.Name, frames, animation.FrameRate, animation.Looped);
            if (animation.Offsets is { Count: > 0 })
            {
                built.FrameOffsets = animation.Offsets
                    .Select(pair => new Vector2(pair.Count > 0 ? pair[0] : 0f, pair.Count > 1 ? pair[1] : 0f))
                    .ToList();
            }
        }

        if (Animation.Count == 0)
        {
            MakeGraphic(180, 320, IsPlayer ? new Color(80, 200, 255) : new Color(230, 80, 120));
            BuildFallbackAnimations();
            return;
        }

        Scale = new Vector2(Data.ScaleOrDefault);
        FlipX = Data.FlipXOrDefault;
        CenterOrigin();
        Position += new Vector2(Width / 2f, Height);

        PlayAnimation(Data.StartingAnimation ?? "idle");
    }

    private void BuildFallbackAnimations()
    {
        var frame = new Graphics.FrameRegion
        {
            Texture = Texture!,
            Source = SourceRect ?? new Rectangle(0, 0, Width, Height),
            Offset = Point.Zero,
            Name = "placeholder",
        };

        Animation.Add("idle", new[] { frame }, 24, true);
        foreach (var direction in new[] { "left", "down", "up", "right" })
        {
            Animation.Add($"sing{direction.ToUpperInvariant()}", new[] { frame }, 24, false);
            Animation.Add($"sing{direction.ToUpperInvariant()}-alt", new[] { frame }, 24, false);
        }
    }

    public void PlayAnimation(string name, bool force = false)
    {
        if (!Animation.Has(name))
        {
            // Fall back to idle so missing animations never leave a blank sprite.
            if (Animation.Has("idle")) name = "idle";
            else return;
        }

        Play(name, force);
    }

    /// <summary>Plays the dance/idle animation, alternating `idle-alt` when present.</summary>
    public void Dance(bool force = false)
    {
        if (IsHolding) return;

        _idleAlternate = !_idleAlternate;
        string name = $"idle{IdleSuffix}";
        if (!Animation.Has(name)) name = "idle";
        PlayAnimation(name, force);
        IsSinging = false;
    }

    /// <summary>Plays a sing animation for a lane, optionally using the `-alt` variant.</summary>
    public void Sing(int lane, bool altAnimation = false, bool miss = false)
    {
        string direction = lane switch
        {
            0 => "LEFT",
            1 => "DOWN",
            2 => "UP",
            _ => "RIGHT",
        };

        string name = $"sing{direction}" + (miss ? "miss" : altAnimation ? "-alt" : string.Empty);

        if (!Animation.Has(name)) name = $"sing{direction}"
            + (altAnimation ? "-alt" : string.Empty);

        PlayAnimation(name, force: true);
        IsSinging = true;
        _singTimer = SingTime;
    }

    /// <summary>Called on every beat by the stage, so characters dance in time with the music.</summary>
    public void OnBeat(int beat)
    {
        if (IsHolding) return;

        // danceEvery supports 0.25 precision, so compare on a quarter-beat grid.
        int danceEveryQuarterBeats = Math.Max(1, (int)Math.Round(DanceEvery * 4));
        if ((beat * 4) % danceEveryQuarterBeats != 0) return;

        Dance();
    }

    public void ResetIdle()
    {
        IsSinging = false;
        IsHolding = false;
        PlayAnimation("idle", force: true);
    }

    public override void Update(float elapsed)
    {
        base.Update(elapsed);

        if (IsSinging && !IsHolding)
        {
            _singTimer -= elapsed;
            if (_singTimer <= 0) IsSinging = false;
        }
    }
}
