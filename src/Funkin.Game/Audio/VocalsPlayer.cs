using Funkin.Data.Song;
using Funkin.Play;
using Funkin.Util;

namespace Funkin.Audio;

/// <summary>
/// Port of funkin.play.song.Song's voice handling (VoicesGroup): plays the player and opponent vocal
/// tracks alongside the instrumental and keeps them sample-synced with it.
/// </summary>
public sealed class VocalsPlayer
{
    private readonly List<FlxSound> _playerVocals = new();
    private readonly List<FlxSound> _opponentVocals = new();
    private FlxSound? _instrumental;

    public float PlayerVolume { get; private set; } = 1f;
    public float OpponentVolume { get; private set; } = 1f;

    public bool HasPlayerVocals => _playerVocals.Count > 0;
    public bool HasOpponentVocals => _opponentVocals.Count > 0;

    private double _muteTimer;

    /// <summary>Loads the vocal tracks for a song. Missing files are ignorable (instrumental-only).</summary>
    public void Load(SongDifficulty song)
    {
        Unload();

        var characters = song.Characters;

        // Opponent vocals: either an explicit list or `<song>/Voices-<opponent>`.
        var opponentTracks = characters.OpponentVocals ?? new List<string>();
        if (opponentTracks.Count == 0)
        {
            AddVocals(_opponentVocals, Paths.FunkinPaths.Instance.Voices(song.Parent.Id), song.Parent.Id);
        }
        else
        {
            foreach (var track in opponentTracks)
            {
                AddVocals(_opponentVocals, Paths.FunkinPaths.Instance.Voices(song.Parent.Id, track), track);
            }
        }

        var playerTracks = characters.PlayerVocals ?? new List<string>();
        if (playerTracks.Count > 0)
        {
            foreach (var track in playerTracks)
            {
                AddVocals(_playerVocals, Paths.FunkinPaths.Instance.Voices(song.Parent.Id, track), track);
            }
        }
        else
        {
            // A single 'Voices' file contains both singers; it is used as the player track too.
            var shared = Paths.FunkinPaths.Instance.Voices(song.Parent.Id);
            if (shared is not null && _opponentVocals.Count > 0)
            {
                AddVocals(_playerVocals, shared, "shared");
            }
        }

        Logger.Info($"Vocals: {_opponentVocals.Count} opponent track(s), {_playerVocals.Count} player track(s).");
    }

    private static void AddVocals(List<FlxSound> list, string? path, string name)
    {
        if (path is null) return;

        var sound = new FlxSound();
        if (!sound.Load(path)) return;

        sound.Volume = 0f; // starts silent until Play() syncs everything
        list.Add(sound);
    }

    public void BindInstrumental(FlxSound? instrumental)
    {
        _instrumental = instrumental;
    }

    public void Play()
    {
        foreach (var sound in _playerVocals.Concat(_opponentVocals)) sound.Play();
    }

    public void Pause()
    {
        foreach (var sound in _playerVocals.Concat(_opponentVocals)) sound.Pause();
    }

    public void Resume()
    {
        foreach (var sound in _playerVocals.Concat(_opponentVocals)) sound.Resume();
    }

    public void Stop()
    {
        foreach (var sound in _playerVocals.Concat(_opponentVocals)) sound.Stop();
    }

    public void Seek(double ms)
    {
        foreach (var sound in _playerVocals.Concat(_opponentVocals)) sound.Seek(ms);
    }

    public void Unload()
    {
        foreach (var sound in _playerVocals.Concat(_opponentVocals)) sound.Unload();
        _playerVocals.Clear();
        _opponentVocals.Clear();
    }

    /// <summary>Temporarily silences the player's vocals, e.g. after a miss.</summary>
    public void MutePlayerFor(double seconds) => _muteTimer = seconds;

    /// <summary>Keeps vocals aligned to the instrumental and applies volumes.</summary>
    public void Update(float elapsed, float playerVolume, float opponentVolume)
    {
        if (_muteTimer > 0) _muteTimer -= elapsed;

        PlayerVolume = _muteTimer > 0 ? 0f : playerVolume;
        OpponentVolume = opponentVolume;

        foreach (var sound in _playerVocals)
        {
            sound.Volume = PlayerVolume;
            sound.Update();
        }

        foreach (var sound in _opponentVocals)
        {
            sound.Volume = OpponentVolume;
            sound.Update();
        }

        // Correct drift against the instrumental clock (audio clocks diverge over a long song).
        if (_instrumental is null || !_instrumental.Playing) return;

        double expected = _instrumental.Time;
        foreach (var sound in _playerVocals.Concat(_opponentVocals))
        {
            if (!sound.Playing) continue;
            if (Math.Abs(sound.Time - expected) < 80) continue;
            sound.Seek(expected);
        }
    }
}
