using Funkin.Data.Stage;
using Funkin.Flixel;
using Funkin.Util;
using Microsoft.Xna.Framework;

namespace Funkin.Play;

/// <summary>A prop on the stage: a spritesheet with optional animations and parallax scrolling.</summary>
public sealed class StageProp : FlxSprite
{
    public StagePropData Data { get; }
    public float DanceEvery { get; }
    private int _danceCounter;

    public StageProp(StagePropData data)
    {
        Data = data;
        DanceEvery = data.DanceEvery;
        Position = new Vector2(data.X, data.Y);
        ScrollFactor = new Vector2(data.ScrollX, data.ScrollY);
        Alpha = data.Alpha;
        Angle = data.Angle;
        FlipX = data.FlipX;
        FlipY = data.FlipY;
        Scale = new Vector2(data.ScaleX, data.ScaleY);

        if (data.IsColorProp)
        {
            var color = ParseColor(data.AssetPath);
            MakeGraphic((int)Math.Max(1, 100 * Scale.X), (int)Math.Max(1, 100 * Scale.Y), color);
            Scale = data.Scale is null ? Vector2.One : new Vector2(data.ScaleX, data.ScaleY);
            return;
        }

        var atlas = LoadAtlas(data.AssetPath);
        if (atlas is null)
        {
            // Invisible placeholder: a missing prop should not break the stage.
            Visible = false;
            Logger.Debug($"Stage prop '{data.AssetPath}' not found; skipping.");
            return;
        }

        foreach (var animation in data.Animations)
        {
            var frames = atlas.GetFramesByPrefix(animation.Prefix);
            if (frames.Count == 0) frames = atlas.GetFramesByPrefix(animation.Name);
            if (frames.Count == 0) continue;

            var built = Animation.Add(animation.Name, frames, animation.FrameRate, animation.Looped);
            if (animation.Offsets is { Count: > 0 })
            {
                built.FrameOffsets = animation.Offsets
                    .Select(p => new Vector2(p.Count > 0 ? p[0] : 0, p.Count > 1 ? p[1] : 0))
                    .ToList();
            }
        }

        if (data.StartingAnimation is not null) Play(data.StartingAnimation);
    }

    private static Color ParseColor(string hex)
    {
        try
        {
            string value = hex.TrimStart('#');
            uint rgb = Convert.ToUInt32(value, 16);
            return value.Length switch
            {
                6 => new Color((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, (byte)255),
                8 => new Color((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, (byte)(rgb >> 24)),
                _ => Color.White,
            };
        }
        catch
        {
            return Color.White;
        }
    }

    /// <summary>Advances the prop's idle animation on beats when `danceEvery` is set.</summary>
    public void OnBeat()
    {
        if (DanceEvery <= 0f || Animation.Count == 0) return;

        int every = Math.Max(1, (int)Math.Round(DanceEvery * 4));
        _danceCounter++;
        if (_danceCounter % every != 0) return;

        if (Animation.Has("danceRight") && Animation.Has("danceLeft"))
        {
            bool right = (_danceCounter / every) % 2 == 0;
            Play(right ? "danceRight" : "danceLeft");
        }
        else if (Animation.Has("idle"))
        {
            Play("idle");
        }
    }
}

/// <summary>
/// Port of funkin.play.stage.Stage: builds the props and character positions described by the
/// stage's JSON data, applies parallax scrolling and drives beat-synced animations.
/// </summary>
public sealed class StageSprite : FlxState
{
    public StageData Data { get; }
    public List<StageProp> Props { get; } = new();

    public CharacterSprite? Boyfriend { get; private set; }
    public CharacterSprite? Dad { get; private set; }
    public CharacterSprite? Girlfriend { get; private set; }

    public float DefaultCameraZoom => Data.CameraZoomOrDefault;

    public StageSprite(string stageId)
    {
        Data = StageRegistry.Instance.GetOrFallback(stageId);
    }

    public override void Create()
    {
        // Props are sorted by zIndex so lower layers draw first.
        foreach (var propData in Data.Props.OrderBy(p => p.ZIndex))
        {
            var prop = new StageProp(propData);
            Props.Add(prop);
            Add(prop);
        }
    }

    /// <summary>Adds the three characters to the stage at the positions defined by the stage data.</summary>
    public void AddCharacters(string playerId, string girlfriendId, string opponentId)
    {
        Boyfriend = CreateCharacter(playerId, isPlayer: true, Data.Characters.Bf);
        Girlfriend = CreateCharacter(girlfriendId, isPlayer: false, Data.Characters.Gf);
        Dad = CreateCharacter(opponentId, isPlayer: false, Data.Characters.Dad);

        // Insert in zIndex order relative to props by simply appending; characters are above props.
        Add(Girlfriend);
        Add(Dad);
        Add(Boyfriend);
    }

    private CharacterSprite CreateCharacter(string id, bool isPlayer, StageCharacterData placement)
    {
        var character = new CharacterSprite(id, isPlayer)
        {
            Position = new Vector2(placement.X, placement.Y),
            Scale = character_scale(placement.Scale),
            ScrollFactor = new Vector2(placement.ScrollX, placement.ScrollY),
            Alpha = placement.Alpha,
            Angle = placement.Angle,
        };

        return character;
    }

    private static Vector2 character_scale(float scale) => new(scale);

    public void OnBeat(int beat)
    {
        foreach (var prop in Props) prop.OnBeat();

        Boyfriend?.OnBeat(beat);
        Dad?.OnBeat(beat);
        Girlfriend?.OnBeat(beat);
    }

    /// <summary>Camera offsets for focusing a character, mirroring BaseCharacter.getCameraOffsets.</summary>
    public Vector2 GetCameraOffsets(string role)
    {
        var placement = Data.Characters.Get(role);
        if (placement.CameraOffsets is { Count: >= 2 }) return new Vector2(placement.CameraOffsets[0], placement.CameraOffsets[1]);

        return role.ToLowerInvariant() switch
        {
            "bf" or "player" => new Vector2(-100, -100),
            "gf" or "girlfriend" => Vector2.Zero,
            _ => new Vector2(100, -100),
        };
    }

    public Vector2 GetCharacterPosition(string role)
    {
        var character = role.ToLowerInvariant() switch
        {
            "bf" or "player" => Boyfriend,
            "gf" or "girlfriend" => Girlfriend,
            _ => Dad,
        };

        return character is null ? Vector2.Zero : character.Position - character.Origin + character.Offset;
    }
}
