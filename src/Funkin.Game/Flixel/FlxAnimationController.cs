using Funkin.Graphics;
using Microsoft.Xna.Framework;

namespace Funkin.Flixel;

/// <summary>A named animation: a list of frames plus playback settings.</summary>
public sealed class FlxAnimation
{
    public required string Name { get; init; }
    public List<FrameRegion> Frames { get; } = new();
    public int FrameRate { get; set; } = 24;
    public bool Looped { get; set; } = true;

    /// <summary>Per-frame XY offsets, as authored in the character's JSON.</summary>
    public List<Vector2>? FrameOffsets { get; set; }

    public int FrameCount => Frames.Count;

    public float FrameDuration => FrameRate <= 0 ? 0f : 1f / FrameRate;
}

/// <summary>Port of FlxAnimationController: plays named animations on a sprite.</summary>
public sealed class FlxAnimationController
{
    private readonly Dictionary<string, FlxAnimation> _animations = new(StringComparer.OrdinalIgnoreCase);
    private float _frameTimer;

    public string? CurrentName { get; private set; }
    public FlxAnimation? CurrentAnimation { get; private set; }
    public int CurrentFrameIndex { get; private set; }
    public bool Finished { get; private set; }
    public bool Paused { get; set; }

    /// <summary>Fired when a non-looping animation reaches its last frame.</summary>
    public event Action<string>? OnFinish;

    /// <summary>Fired on every new frame of the current animation.</summary>
    public event Action<string, int>? OnFrameChange;

    public IReadOnlyDictionary<string, FlxAnimation> Animations => _animations;

    public int Count => _animations.Count;

    public bool Has(string name) => _animations.ContainsKey(name);

    public FlxAnimation Add(string name, IEnumerable<FrameRegion> frames, int frameRate = 24, bool looped = true)
    {
        var animation = new FlxAnimation { Name = name, FrameRate = frameRate, Looped = looped };
        animation.Frames.AddRange(frames);
        _animations[name] = animation;
        return animation;
    }

    public FlxAnimation? Get(string name) => _animations.TryGetValue(name, out var anim) ? anim : null;

    public FrameRegion? CurrentFrame
    {
        get
        {
            if (CurrentAnimation is null || CurrentAnimation.Frames.Count == 0) return null;
            return CurrentAnimation.Frames[Math.Clamp(CurrentFrameIndex, 0, CurrentAnimation.Frames.Count - 1)];
        }
    }

    public Vector2 CurrentFrameOffset
    {
        get
        {
            var animation = CurrentAnimation;
            if (animation?.FrameOffsets is null || animation.FrameOffsets.Count == 0) return Vector2.Zero;
            return animation.FrameOffsets[Math.Clamp(CurrentFrameIndex, 0, animation.FrameOffsets.Count - 1)];
        }
    }

    /// <summary>Plays an animation. Re-playing the current animation restarts it unless already playing.</summary>
    public void Play(string name, bool force = false, bool reverse = false)
    {
        if (!_animations.TryGetValue(name, out var animation)) return;
        if (!force && CurrentName == name && !Finished) return;

        CurrentName = name;
        CurrentAnimation = animation;
        CurrentFrameIndex = reverse ? Math.Max(0, animation.Frames.Count - 1) : 0;
        _frameTimer = 0f;
        Finished = false;
        OnFrameChange?.Invoke(name, CurrentFrameIndex);
    }

    public void Stop()
    {
        CurrentAnimation = null;
        CurrentName = null;
        CurrentFrameIndex = 0;
        Finished = false;
    }

    public void Update(float elapsed)
    {
        if (Paused || CurrentAnimation is null) return;

        var animation = CurrentAnimation;
        if (animation.Frames.Count <= 1 || animation.FrameRate <= 0) return;

        float frameDuration = animation.FrameDuration;
        _frameTimer += elapsed;

        while (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            int next = CurrentFrameIndex + 1;

            if (next >= animation.Frames.Count)
            {
                if (animation.Looped)
                {
                    next = 0;
                }
                else
                {
                    Finished = true;
                    OnFinish?.Invoke(animation.Name);
                    break;
                }
            }

            CurrentFrameIndex = next;
            OnFrameChange?.Invoke(animation.Name, CurrentFrameIndex);
        }
    }

    /// <summary>Jumps to a specific frame of the current animation.</summary>
    public void SetFrame(int index)
    {
        if (CurrentAnimation is null || CurrentAnimation.Frames.Count == 0) return;
        CurrentFrameIndex = Math.Clamp(index, 0, CurrentAnimation.Frames.Count - 1);
    }
}
