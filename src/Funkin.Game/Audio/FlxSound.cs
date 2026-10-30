using Funkin.Play;
using Funkin.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using NVorbis;

namespace Funkin.Audio;

/// <summary>
/// Port of flixel.sound.FlxSound for OGG music and sound effects.
/// OGG files are streamed through NVorbis into a DynamicSoundEffectInstance, which keeps memory use
/// low for the long instrumental tracks and gives a reliable playback position for the Conductor.
/// </summary>
public sealed class FlxSound : IMusicSource, IDisposable
{
    private const int BufferSeconds = 2; // total audio queued ahead of playback

    private DynamicSoundEffectInstance? _instance;
    private VorbisReader? _reader;
    private string? _path;
    private byte[] _buffer = Array.Empty<byte>();
    private float[] _sampleBuffer = Array.Empty<float>();
    private int _bufferSizeSamples;
    private bool _loop;
    private bool _wantPlaying;

    public float Volume
    {
        get => _instance?.Volume ?? _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            if (_instance is not null) _instance.Volume = _volume;
        }
    }

    private float _volume = 1f;

    /// <summary>Whether the track loops when it reaches the end.</summary>
    public bool Looped
    {
        get => _loop;
        set => _loop = value;
    }

    public float Pitch { get; set; } = 1f;

    /// <summary>Provider for the currently playing FlxG.sound.music equivalent.</summary>
    public string? Path => _path;

    public bool Playing => _wantPlaying && _instance is { State: SoundState.Playing };

    public bool Loaded => _reader is not null;

    public double Length { get; private set; }

    /// <summary>Playback position in milliseconds.</summary>
    public double Time
    {
        get
        {
            if (_reader is null) return _timeOffsetMs;
            if (_instance is null) return _timeOffsetMs;

            // Samples handed to the device, minus the ones still queued, gives the audible position.
            long submitted = _submittedSamples;
            long pending = (long)_instance.PendingBufferCount * _bufferSizeSamples;
            long playedSamples = Math.Max(0, submitted - pending);
            return _timeOffsetMs + (playedSamples * 1000.0 / Math.Max(1, _reader.SampleRate * _reader.Channels));
        }
        set
        {
            Seek(value);
        }
    }

    private long _submittedSamples;
    private double _timeOffsetMs;

    /// <summary>Raised when the track finishes (non-looping).</summary>
    public event Action? OnComplete;

    public static Func<byte[]>? FileReader { get; set; }

    /// <summary>Loads an OGG file from an absolute path, or null when the file is missing.</summary>
    public bool Load(string? absolutePath)
    {
        Unload();
        if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath)) return false;

        try
        {
            _reader = new VorbisReader(absolutePath);
            _path = absolutePath;
            Length = _reader.TotalTime.TotalMilliseconds;

            _instance = new DynamicSoundEffectInstance(_reader.SampleRate, _reader.Channels == 1 ? AudioChannels.Mono : AudioChannels.Stereo);
            _bufferSizeSamples = _reader.SampleRate / 10 * _reader.Channels; // 100ms chunks
            _buffer = new byte[_bufferSizeSamples * 2];
            _sampleBuffer = new float[_bufferSizeSamples];
            _submittedSamples = 0;
            _timeOffsetMs = 0;
            _instance.Volume = _volume;
            _instance.BufferNeeded += (_, _) => SubmitBuffers();

            // Fill the queue so playback starts immediately when asked to.
            SubmitBuffers(initial: true);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load audio '{absolutePath}': {ex.Message}");
            Unload();
            return false;
        }
    }

    private void SubmitBuffers(bool initial = false)
    {
        if (_instance is null || _reader is null) return;
        if (!initial && !_wantPlaying) return;

        int targetBuffers = Math.Max(1, BufferSeconds * 10);
        int submitted = 0;

        while (_instance.PendingBufferCount < targetBuffers && submitted < targetBuffers)
        {
            int samplesRead = _reader.ReadSamples(_sampleBuffer, 0, _sampleBuffer.Length);
            if (samplesRead <= 0)
            {
                if (_loop)
                {
                    _reader.TimePosition = TimeSpan.Zero;
                    _timeOffsetMs = 0;
                    _submittedSamples = 0;
                    continue;
                }

                if (_wantPlaying)
                {
                    _wantPlaying = false;
                    OnComplete?.Invoke();
                }

                return;
            }

            int byteCount = samplesRead * 2;
            ConvertToPcm16(_sampleBuffer, samplesRead, _buffer);

            try
            {
                _instance.SubmitBuffer(_buffer, 0, byteCount);
            }
            catch (InvalidOperationException)
            {
                // Queue full; try again on the next BufferNeeded callback.
                return;
            }

            _submittedSamples += samplesRead;
            submitted++;
        }
    }

    private static void ConvertToPcm16(float[] source, int sampleCount, byte[] destination)
    {
        for (int i = 0; i < sampleCount; i++)
        {
            float value = Math.Clamp(source[i], -1f, 1f);
            short pcm = (short)(value * short.MaxValue);
            destination[i * 2] = (byte)(pcm & 0xFF);
            destination[(i * 2) + 1] = (byte)((pcm >> 8) & 0xFF);
        }
    }

    public void Play(bool restart = false)
    {
        if (_instance is null) return;
        _wantPlaying = true;

        if (restart) Seek(0);

        if (_instance.State == SoundState.Paused) _instance.Resume();
        else if (_instance.State != SoundState.Playing)
        {
            SubmitBuffers(initial: true);
            _instance.Play();
        }
    }

    public void Pause()
    {
        if (_instance is null) return;
        _wantPlaying = false;
        if (_instance.State == SoundState.Playing) _instance.Pause();
    }

    public void Resume()
    {
        if (_instance is null) return;
        _wantPlaying = true;
        if (_instance.State == SoundState.Paused) _instance.Resume();
        else Play();
    }

    public void Stop()
    {
        if (_instance is null) return;
        _wantPlaying = false;
        _instance.Stop();
        Seek(0);
    }

    /// <summary>Seeks to a position in milliseconds.</summary>
    public void Seek(double ms)
    {
        if (_reader is null || _instance is null) return;

        bool wasPlaying = _instance.State == SoundState.Playing;
        if (wasPlaying) _instance.Stop();

        try
        {
            _reader.TimePosition = TimeSpan.FromMilliseconds(Math.Clamp(ms, 0, Length));
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to seek to {ms}ms: {ex.Message}");
        }

        _timeOffsetMs = ms;
        _submittedSamples = 0;

        if (wasPlaying && _wantPlaying)
        {
            SubmitBuffers(initial: true);
            _instance.Play();
        }
    }

    public void Unload()
    {
        _wantPlaying = false;
        try
        {
            _instance?.Stop();
            _instance?.Dispose();
        }
        catch
        {
            // Ignore disposal races on shutdown.
        }

        _instance = null;
        _reader?.Dispose();
        _reader = null;
        _path = null;
        Length = 0;
        _timeOffsetMs = 0;
        _submittedSamples = 0;
    }

    public void Dispose() => Unload();

    /// <summary>Keeps the streaming queue topped up; call once per frame.</summary>
    public void Update()
    {
        if (_wantPlaying) SubmitBuffers();
    }

    // ------------------------------------------------------------------
    // One-shot sound effects
    // ------------------------------------------------------------------

    private static readonly Dictionary<string, SoundEffect?> SoundCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Plays a short OGG/WAV sound effect; returns the live instance (or null when missing).</summary>
    public static SoundEffectInstance? PlaySfx(string? absolutePath, float volume = 1f, bool loop = false)
    {
        if (string.IsNullOrEmpty(absolutePath)) return null;

        if (!SoundCache.TryGetValue(absolutePath, out var effect))
        {
            effect = LoadSoundEffect(absolutePath);
            SoundCache[absolutePath] = effect;
        }

        if (effect is null) return null;

        var instance = effect.CreateInstance();
        instance.Volume = Math.Clamp(volume, 0f, 1f);
        instance.IsLooped = loop;
        instance.Play();
        return instance;
    }

    private static SoundEffect? LoadSoundEffect(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;

            if (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = File.OpenRead(path);
                return SoundEffect.FromStream(stream);
            }

            // Decode the whole OGG into memory; short SFX are small enough for this.
            using var reader = new VorbisReader(path);
            int totalSamples = (int)(reader.TotalTime.TotalSeconds * reader.SampleRate * reader.Channels);
            var samples = new float[totalSamples];
            int read = 0;
            while (read < totalSamples)
            {
                int chunk = reader.ReadSamples(samples, read, totalSamples - read);
                if (chunk <= 0) break;
                read += chunk;
            }

            var pcm = new byte[read * 2];
            var scratch = new byte[read * 2];
            ConvertToPcm16(samples, read, scratch);
            Array.Copy(scratch, pcm, pcm.Length);

            return new SoundEffect(pcm, reader.SampleRate, reader.Channels == 1 ? AudioChannels.Mono : AudioChannels.Stereo);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load sound effect '{path}': {ex.Message}");
            return null;
        }
    }
}
