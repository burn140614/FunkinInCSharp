namespace Funkin.Audio;

/// <summary>
/// Port of FlxG.sound: owns the music track (the song instrumental while playing) and any
/// looping/one-shot sounds. The Conductor reads its music position from <see cref="Music"/>.
/// </summary>
public sealed class SoundManager
{
    /// <summary>The currently playing music track (FlxG.sound.music).</summary>
    public FlxSound? Music { get; private set; }

    private readonly List<FlxSound> _sounds = new();

    public float MusicVolume { get; set; } = 1f;

    public float SfxVolume { get; set; } = 1f;

    public bool Muted { get; set; }

    public void Initialize()
    {
        Music = new FlxSound();
    }

    /// <summary>Loads and plays a music track, replacing the current one.</summary>
    public FlxSound? PlayMusic(string? absolutePath, float volume = 1f, bool loop = false)
    {
        StopMusic();
        Music ??= new FlxSound();

        if (absolutePath is null || !Music.Load(absolutePath))
        {
            Util.Logger.Warn($"Music not found: {absolutePath}");
            return null;
        }

        Music.Volume = Muted ? 0f : volume * MusicVolume;
        Music.Looped = loop;
        Music.Play();
        return Music;
    }

    public void StopMusic()
    {
        Music?.Stop();
        Music?.Unload();
    }

    public void PauseMusic() => Music?.Pause();

    public void ResumeMusic() => Music?.Resume();

    /// <summary>Registers a looping ambience track; returns it so callers can pause/stop it.</summary>
    public FlxSound PlayLoop(string? absolutePath, float volume = 1f)
    {
        var sound = new FlxSound();
        if (absolutePath is not null && sound.Load(absolutePath))
        {
            sound.Looped = true;
            sound.Volume = Muted ? 0f : volume * SfxVolume;
            sound.Play();
            _sounds.Add(sound);
        }

        return sound;
    }

    public void PlaySfx(string? absolutePath, float volume = 1f)
    {
        if (Muted) return;
        FlxSound.PlaySfx(absolutePath, volume * SfxVolume);
    }

    public void StopAll()
    {
        foreach (var sound in _sounds) sound.Unload();
        _sounds.Clear();
    }

    public void Update()
    {
        Music?.Update();

        for (int i = _sounds.Count - 1; i >= 0; i--)
        {
            _sounds[i].Update();
            if (!_sounds[i].Loaded)
            {
                _sounds[i].Unload();
                _sounds.RemoveAt(i);
            }
        }
    }
}
