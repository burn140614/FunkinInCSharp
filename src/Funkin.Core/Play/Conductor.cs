using Funkin.Data.Song;
using Funkin.Util;

namespace Funkin.Play;

/// <summary>Abstraction over the currently playing instrumental, so the Conductor stays engine-agnostic.</summary>
public interface IMusicSource
{
    double Time { get; set; }
    double Length { get; }
    bool Playing { get; }
    float Pitch { get; }
}

/// <summary>
/// Port of funkin.Conductor: the core class which handles musical timing throughout the game,
/// both in gameplay and in menus.
///
/// 4/4 = 4 beats per measure = 16 steps per measure
/// 120 BPM = 120 quarter notes per minute = 2 beat hits per second = 8 step hits per second
/// </summary>
public sealed class Conductor
{
    private static Conductor? _instance;

    /// <summary>The current static Conductor instance, created on demand.</summary>
    public static Conductor Instance => _instance ??= new Conductor();

    public static Conductor? ExistingInstance => _instance;

    public static void SetInstance(Conductor? conductor)
    {
        _instance = conductor;
    }

    public static void Reset() => _instance = new Conductor();

    // Signals shared by every Conductor.
    public static readonly Signal MeasureHitSignal = new();
    public static readonly Signal BeatHitSignal = new();
    public static readonly Signal StepHitSignal = new();

    // Per-instance signals.
    public readonly Signal OnMeasureHit = new();
    public readonly Signal OnBeatHit = new();
    public readonly Signal OnStepHit = new();

    /// <summary>The list of time changes in the song, sorted by timestamp.</summary>
    public List<SongTimeChange> TimeChanges { get; private set; } = new() { SongTimeChange.Default.Clone() };

    /// <summary>The most recent time change for the current song position.</summary>
    public SongTimeChange? CurrentTimeChange { get; private set; }

    /// <summary>Position in the song in milliseconds.</summary>
    public double SongPosition { get; private set; }

    /// <summary>Sub-frame delta used by <see cref="GetTimeWithDelta"/>.</summary>
    public double SongPositionDelta { get; private set; }

    /// <summary>Frame delta in seconds; the game loop keeps this up to date.</summary>
    public float Elapsed { get; set; }

    /// <summary>Optional instrumental source used when no explicit position is passed to Update.</summary>
    public IMusicSource? Music { get; set; }

    private double _prevTime;

    public float? BpmOverride { get; private set; }

    public int GlobalOffset { get; set; }

    public int AudioVisualOffset { get; set; }

    /// <summary>Offset tied to the chart to compensate for a delay in the instrumental.</summary>
    public float InstrumentalOffset { get; set; }

    /// <summary>Offset tied to the audio file format.</summary>
    public float FormatOffset { get; set; }

    public float CombinedOffset => InstrumentalOffset + FormatOffset + GlobalOffset;

    public float Bpm => BpmOverride ?? (CurrentTimeChange?.Bpm ?? Constants.DEFAULT_BPM);

    public float StartingBpm => BpmOverride ?? (TimeChanges.Count > 0 ? TimeChanges[0].Bpm : Constants.DEFAULT_BPM);

    public int TimeSignatureNumerator => CurrentTimeChange?.TimeSignatureNum ?? Constants.DEFAULT_TIME_SIGNATURE_NUM;

    public int TimeSignatureDenominator => CurrentTimeChange?.TimeSignatureDen ?? Constants.DEFAULT_TIME_SIGNATURE_DEN;

    public float BeatLengthMs => ((Constants.SECS_PER_MIN / Bpm) * Constants.MS_PER_SEC) * (4f / TimeSignatureDenominator);

    public float StepLengthMs => BeatLengthMs / Constants.STEPS_PER_BEAT;

    public float MeasureLengthMs => BeatLengthMs * TimeSignatureNumerator;

    public int StepsPerMeasure => (int)(TimeSignatureNumerator * Constants.STEPS_PER_BEAT);

    public float BeatsPerMeasure => TimeSignatureNumerator;

    /// <summary>The instrumental offset expressed in steps.</summary>
    public float InstrumentalOffsetSteps
    {
        get
        {
            float startingStepLengthMs = (((Constants.SECS_PER_MIN / StartingBpm) * Constants.MS_PER_SEC) * (4f / TimeSignatureDenominator)) / Constants.STEPS_PER_BEAT;
            return InstrumentalOffset / startingStepLengthMs;
        }
    }

    public float CurrentMeasureTime { get; private set; }
    public float CurrentBeatTime { get; private set; }
    public float CurrentStepTime { get; private set; }
    public int CurrentMeasure { get; private set; }
    public int CurrentBeat { get; private set; }
    public int CurrentStep { get; private set; }

    /// <summary>Steps into the current measure, as reported by the original game's step counter.</summary>
    public int CurrentStepInMeasure => StepsPerMeasure == 0 ? 0 : CurrentStep % StepsPerMeasure;

    private bool _dispatching;

    /// <summary>Forcibly define the BPM (pass null to reset to the chart's BPM).</summary>
    public void ForceBpm(float? bpm) => BpmOverride = bpm;

    /// <summary>Apply the time change data from the song metadata to this Conductor.</summary>
    public void MapTimeChanges(IEnumerable<SongTimeChange> timeChanges)
    {
        var list = timeChanges.Select(tc => tc.Clone()).ToList();
        if (list.Count == 0) list.Add(SongTimeChange.Default.Clone());

        SortTimeChanges(list);

        // Recalculate beat times when they were left at the default.
        for (int i = 0; i < list.Count; i++)
        {
            var tc = list[i];
            if (tc.TimeStamp < 0) tc.TimeStamp = 0;
            if (i == 0)
            {
                if (tc.BeatTime < 0) tc.BeatTime = 0;
            }
            else if (tc.BeatTime < 0)
            {
                var prev = list[i - 1];
                float prevStepLengthMs = (((Constants.SECS_PER_MIN / prev.Bpm) * Constants.MS_PER_SEC) * (4f / prev.TimeSignatureDen)) / Constants.STEPS_PER_BEAT;
                float stepsDelta = (tc.TimeStamp - prev.TimeStamp) / prevStepLengthMs;
                tc.BeatTime = prev.BeatTime + (stepsDelta / Constants.STEPS_PER_BEAT);
            }
        }

        TimeChanges = list;
        CurrentTimeChange = list[0];

        // Reset the cached positions so the next update recomputes them.
        CurrentStep = -1;
        CurrentBeat = -1;
        CurrentMeasure = -1;
    }

    /// <summary>Sorts time changes in place by timestamp (stable).</summary>
    public static void SortTimeChanges(List<SongTimeChange> list)
    {
        list.Sort((a, b) => a.TimeStamp.CompareTo(b.TimeStamp));
    }

    /// <summary>
    /// Update the conductor with the current song position. BPM, current step, etc. are re-calculated from it.
    /// </summary>
    public void Update(double? songPos = null, bool applyOffsets = true, bool forceDispatch = false)
    {
        double currentTime = Music?.Time ?? 0.0;
        double currentLength = Music?.Length ?? 0.0;

        songPos ??= currentTime;
        if (applyOffsets) songPos += CombinedOffset;

        int oldMeasure = CurrentMeasure;
        int oldBeat = CurrentBeat;
        int oldStep = CurrentStep;

        if (Music is not null && Music.Playing)
        {
            double lower = Math.Min(CombinedOffset, 0f);
            SongPosition = Math.Clamp(songPos.Value, lower, Math.Max(currentLength, lower));
            SongPositionDelta += Elapsed * 1000.0 * Music.Pitch;
        }
        else
        {
            SongPosition = songPos.Value;
        }

        CurrentTimeChange = TimeChanges.Count > 0 ? TimeChanges[0] : null;
        if (SongPosition > 0.0 && TimeChanges.Count > 0)
        {
            foreach (var tc in TimeChanges)
            {
                if (SongPosition >= tc.TimeStamp) CurrentTimeChange = tc;
                if (SongPosition < tc.TimeStamp) break;
            }
        }

        if (CurrentTimeChange is null && BpmOverride is null && Music is not null)
        {
            Logger.Warn("Conductor is broken, timeChanges is empty.");
        }

        if (CurrentTimeChange is not null && SongPosition > 0.0)
        {
            CurrentStepTime = MathUtil.RoundDecimal(
                (CurrentTimeChange.BeatTime * Constants.STEPS_PER_BEAT) + ((SongPosition - CurrentTimeChange.TimeStamp) / StepLengthMs), 6);
            CurrentBeatTime = CurrentStepTime / Constants.STEPS_PER_BEAT;
            CurrentMeasureTime = GetTimeInMeasures(SongPosition);
            CurrentStep = (int)Math.Floor(CurrentStepTime);
            CurrentBeat = (int)Math.Floor(CurrentBeatTime);
            CurrentMeasure = (int)Math.Floor(CurrentMeasureTime);
        }
        else
        {
            CurrentStepTime = MathUtil.RoundDecimal(SongPosition / StepLengthMs, 4);
            CurrentBeatTime = CurrentStepTime / Constants.STEPS_PER_BEAT;
            CurrentMeasureTime = StepsPerMeasure == 0 ? 0 : CurrentStepTime / StepsPerMeasure;
            CurrentStep = (int)Math.Floor(CurrentStepTime);
            CurrentBeat = (int)Math.Floor(CurrentBeatTime);
            CurrentMeasure = (int)Math.Floor(CurrentMeasureTime);
        }

        bool changed = false;

        if (CurrentStep != oldStep)
        {
            changed = true;
            DispatchStepHit();
        }

        if (CurrentBeat != oldBeat)
        {
            changed = true;
            DispatchBeatHit();
        }

        if (CurrentMeasure != oldMeasure)
        {
            changed = true;
            DispatchMeasureHit();
        }

        if (!changed && forceDispatch)
        {
            DispatchStepHit();
            DispatchBeatHit();
            DispatchMeasureHit();
        }

        // Only reset the sub-frame delta when the position actually moved.
        if (Math.Abs(_prevTime - SongPosition) > double.Epsilon)
        {
            SongPositionDelta = 0;
            _prevTime = SongPosition;
        }
    }

    /// <summary>Resets the conductor to a clean state for a new song.</summary>
    public void ResetPosition()
    {
        SongPosition = 0;
        SongPositionDelta = 0;
        CurrentStepTime = 0;
        CurrentBeatTime = 0;
        CurrentMeasureTime = 0;
        CurrentStep = -1;
        CurrentBeat = -1;
        CurrentMeasure = -1;
        _prevTime = 0;
    }

    private void DispatchStepHit()
    {
        if (_dispatching) return;
        _dispatching = true;
        try
        {
            OnStepHit.Dispatch();
            if (ReferenceEquals(this, _instance)) StepHitSignal.Dispatch();
        }
        finally
        {
            _dispatching = false;
        }
    }

    private void DispatchBeatHit()
    {
        if (_dispatching) return;
        _dispatching = true;
        try
        {
            OnBeatHit.Dispatch();
            if (ReferenceEquals(this, _instance)) BeatHitSignal.Dispatch();
        }
        finally
        {
            _dispatching = false;
        }
    }

    private void DispatchMeasureHit()
    {
        if (_dispatching) return;
        _dispatching = true;
        try
        {
            OnMeasureHit.Dispatch();
            if (ReferenceEquals(this, _instance)) MeasureHitSignal.Dispatch();
        }
        finally
        {
            _dispatching = false;
        }
    }

    /// <summary>Returns a more accurate music time for higher framerates.</summary>
    public double GetTimeWithDelta() => SongPosition + SongPositionDelta;

    // -------------------------------------------------------------------------
    // Time conversions (ported from Conductor.hx)
    // -------------------------------------------------------------------------

    /// <summary>Get the time change a given millisecond timestamp falls inside of.</summary>
    public SongTimeChange GetTimeChange(double ms)
    {
        if (TimeChanges.Count == 0) return new SongTimeChange(0, Constants.DEFAULT_BPM);
        ms = ms < 0 ? 0 : ms;

        for (int i = 0; i < TimeChanges.Count; i++)
        {
            var tc = TimeChanges[i];
            if ((i == TimeChanges.Count - 1 && ms >= tc.TimeStamp) || (ms >= tc.TimeStamp && ms < TimeChanges[i + 1].TimeStamp))
            {
                return tc;
            }
        }

        return new SongTimeChange(0, Constants.DEFAULT_BPM);
    }

    /// <summary>The length of a step/beat/measure in milliseconds at a given time.</summary>
    public float GetTypeLengthAtMs(double ms, string type = "beat")
    {
        if (TimeChanges.Count == 0) return 0;

        var wanted = TimeChanges[0];
        foreach (var tc in TimeChanges)
        {
            if (ms >= tc.TimeStamp) wanted = tc;
            else break;
        }

        float beatLengthMs = ((Constants.SECS_PER_MIN / wanted.Bpm) * Constants.MS_PER_SEC) * (4f / wanted.TimeSignatureDen);
        return type.ToLowerInvariant() switch
        {
            "measure" or "m" => beatLengthMs * wanted.TimeSignatureNum,
            "step" or "s" => beatLengthMs / Constants.STEPS_PER_BEAT,
            _ => beatLengthMs,
        };
    }

    /// <summary>Convert a millisecond timestamp to a step count.</summary>
    public float GetTimeInSteps(double ms)
    {
        if (TimeChanges.Count == 0) return 0f;
        if (ms <= 0) return 0f;

        var tc = GetTimeChange(ms);
        double stepLengthMs = (((Constants.SECS_PER_MIN / tc.Bpm) * Constants.MS_PER_SEC) * (4f / tc.TimeSignatureDen)) / Constants.STEPS_PER_BEAT;
        return (float)((tc.BeatTime * Constants.STEPS_PER_BEAT) + ((ms - tc.TimeStamp) / stepLengthMs));
    }

    /// <summary>Convert a step count to a millisecond timestamp.</summary>
    public float GetStepTimeInMs(double stepTime)
    {
        if (TimeChanges.Count == 0) return 0f;

        double beatTime = stepTime / Constants.STEPS_PER_BEAT;
        double resultMs = 0;
        var lastTimeChange = TimeChanges[0];

        foreach (var tc in TimeChanges)
        {
            if (beatTime >= tc.BeatTime)
            {
                lastTimeChange = tc;
                resultMs = tc.TimeStamp;
            }
            else
            {
                break;
            }
        }

        double lastStepLengthMs = (((Constants.SECS_PER_MIN / lastTimeChange.Bpm) * Constants.MS_PER_SEC) * (4f / lastTimeChange.TimeSignatureDen)) / Constants.STEPS_PER_BEAT;
        resultMs += (beatTime - lastTimeChange.BeatTime) * lastStepLengthMs * Constants.STEPS_PER_BEAT;
        return (float)resultMs;
    }

    /// <summary>Convert a millisecond timestamp to a measure count.</summary>
    public float GetTimeInMeasures(double ms)
    {
        if (TimeChanges.Count == 0) return 0f;
        return GetTimeInSteps(ms) / StepsPerMeasure;
    }
}
