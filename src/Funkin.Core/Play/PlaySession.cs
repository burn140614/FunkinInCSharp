using Funkin.Data.Song;
using Funkin.Save;
using Funkin.Util;

namespace Funkin.Play;

/// <summary>The state of one note inside a <see cref="PlaySession"/>.</summary>
public sealed class PlayNote
{
    public required SongNoteData Data { get; init; }
    public required int Lane { get; init; }
    public required bool MustHit { get; init; }
    public int Index { get; init; }

    public float Time => Data.Time;
    public float Length => Data.Length;
    public bool IsHold => Data.Length > 0;
    public float EndTime => Data.Time + Data.Length;

    /// <summary>True once the note has been hit by the player (or auto-hit for opponent notes).</summary>
    public bool Hit { get; set; }

    /// <summary>True once the note counted as a miss.</summary>
    public bool Missed { get; set; }

    /// <summary>True while the player is holding the sustain.</summary>
    public bool Holding { get; set; }

    /// <summary>True when the sustain was released early (a "hold drop").</summary>
    public bool HoldDropped { get; set; }

    /// <summary>True once the sustain has reached its end without being dropped.</summary>
    public bool HoldCompleted { get; set; }

    /// <summary>Timing difference in ms between the hit and the chart time (negative = early).</summary>
    public float HitOffset { get; set; }

    public string? Judgement { get; set; }

    public int ScoreAwarded { get; set; }

    /// <summary>True when the note has been fully consumed (hit + sustain finished, or missed).</summary>
    public bool Resolved => Missed || (Hit && !IsHold) || (Hit && HoldDropped) || (Hit && HoldCompleted);

    public override string ToString() => $"PlayNote(lane {Lane}, {Time}ms{(IsHold ? $", len {Length}" : string.Empty)})";
}

/// <summary>Arguments for a note hit event.</summary>
public sealed class NoteHitEventArgs : EventArgs
{
    public required PlayNote Note { get; init; }
    public required string Judgement { get; init; }
    public required float TimingOffset { get; init; }
    public required int ScoreAwarded { get; init; }
    public required bool IsPlayer { get; init; }
}

/// <summary>Arguments for a note miss event.</summary>
public sealed class NoteMissEventArgs : EventArgs
{
    public required PlayNote Note { get; init; }
    public required bool IsPlayer { get; init; }
    public required bool IsGhostMiss { get; init; }
}

/// <summary>Options which affect gameplay behaviour, mirroring the relevant save data.</summary>
public sealed class PlaySessionOptions
{
    public ScoringSystem ScoringSystem { get; set; } = ScoringSystem.Pbot1;
    public bool Botplay { get; set; }
    public bool GhostTapping { get; set; } = true;
    public bool Downscroll { get; set; }
    public float NoteOffset { get; set; }
    public float ScrollSpeed { get; set; } = Constants.DEFAULT_SCROLLSPEED;
    public float AudioVisualOffset { get; set; }
    public bool Instakill { get; set; }
    public float PlaybackRate { get; set; } = 1f;
    public int StrumlineSize { get; set; } = 4;
    public bool NoFail { get; set; }
    public bool PracticeMode { get; set; }
}

/// <summary>
/// The engine-agnostic gameplay simulation for one song: it owns the notes, judges the player's
/// inputs, tracks health, combo, score and tallies, and fires events the renderer hooks into.
/// The graphical PlayState is a thin view on top of this class.
/// </summary>
public sealed class PlaySession
{
    public SongDifficulty Difficulty { get; }
    public Conductor Conductor { get; }
    public PlaySessionOptions Options { get; }

    public List<PlayNote> Notes { get; } = new();
    public List<SongEventData> Events { get; }

    /// <summary>Index of the next note which has not been consumed by the spawn check.</summary>
    public int SpawnIndex { get; private set; }

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public float Health { get; private set; } = Constants.HEALTH_STARTING;
    public SaveScoreTallyData Tallies { get; } = new();

    public int NotesHit => Tallies.TotalNotesHit;
    public int TotalPlayerNotes { get; }
    public int Misses => Tallies.Missed;
    public bool IsDead => Health <= Constants.HEALTH_MIN;
    public bool IsFinished { get; private set; }

    /// <summary>Set when the song's last note has passed or the instrumental ended.</summary>
    public bool AllNotesConsumed { get; private set; }

    public double SongPosition => Conductor.SongPosition;

    public float Accuracy
    {
        get
        {
            int total = Tallies.TotalJudgements;
            if (total == 0) return 1f;
            double weighted = (Tallies.Sick * 1.0) + (Tallies.Good * 0.75) + (Tallies.Bad * 0.4) + (Tallies.Shit * 0.1);
            return (float)(weighted / total);
        }
    }

    /// <summary>Score displayed in the HUD: either raw score or accuracy, depending on the option.</summary>
    public bool ShowScoreAsAccuracy { get; set; }

    // Input state per lane.
    private readonly bool[] _laneHeld;
    private readonly bool[] _lanePressedThisFrame;
    private readonly bool[] _laneReleasedThisFrame;

    public event EventHandler<NoteHitEventArgs>? OnNoteHit;
    public event EventHandler<NoteMissEventArgs>? OnNoteMiss;
    public event EventHandler<PlayNote>? OnNoteSpawn;
    public event EventHandler<PlayNote>? OnNoteHoldDrop;
    public event EventHandler<SongEventData>? OnSongEvent;
    public event EventHandler? OnSongEnd;
    public event EventHandler? OnPlayerDeath;

    private int _eventIndex;

    public PlaySession(SongDifficulty difficulty, PlaySessionOptions? options = null, Conductor? conductor = null)
    {
        Difficulty = difficulty;
        Options = options ?? new PlaySessionOptions();
        Conductor = conductor ?? Conductor.Instance;

        Conductor.MapTimeChanges(difficulty.TimeChanges);
        Conductor.InstrumentalOffset = difficulty.Offsets.Instrumental + Options.NoteOffset + Options.AudioVisualOffset;
        Conductor.ResetPosition();

        int strumlineSize = Options.StrumlineSize;
        int index = 0;
        foreach (var note in difficulty.Notes)
        {
            Notes.Add(new PlayNote
            {
                Data = note,
                Lane = note.GetDirection(strumlineSize),
                MustHit = note.GetMustHitNote(strumlineSize),
                Index = index++,
            });
        }

        Events = difficulty.Events.ToList();
        TotalPlayerNotes = Notes.Count(n => n.MustHit);
        _laneHeld = new bool[strumlineSize];
        _lanePressedThisFrame = new bool[strumlineSize];
        _laneReleasedThisFrame = new bool[strumlineSize];
        Tallies.TotalNotes = TotalPlayerNotes;
    }

    // ------------------------------------------------------------------
    // Input
    // ------------------------------------------------------------------

    /// <summary>Called by the view when a lane key goes down.</summary>
    public void PressLane(int lane)
    {
        if (lane < 0 || lane >= _laneHeld.Length) return;
        _laneHeld[lane] = true;
        _lanePressedThisFrame[lane] = true;
    }

    /// <summary>Called by the view when a lane key goes up.</summary>
    public void ReleaseLane(int lane)
    {
        if (lane < 0 || lane >= _laneHeld.Length) return;
        _laneHeld[lane] = false;
        _laneReleasedThisFrame[lane] = true;
    }

    public bool IsLaneHeld(int lane) => lane >= 0 && lane < _laneHeld.Length && _laneHeld[lane];

    // ------------------------------------------------------------------
    // Update
    // ------------------------------------------------------------------

    /// <summary>Advances the simulation. Call once per frame after updating the Conductor.</summary>
    public void Update(double songPosition)
    {
        SpawnDueNotes(songPosition);
        ProcessInputs(songPosition);
        ProcessHolds(songPosition);
        ProcessMisses(songPosition);
        ProcessEvents(songPosition);

        // The song is over once the position passes the last note (plus a small grace period).
        if (!IsFinished)
        {
            double endTime = Difficulty.Notes.Count > 0 ? Difficulty.Notes.Max(n => (double)n.Time + n.Length) : 0;
            double length = Conductor.Music?.Length ?? 0;
            double endThreshold = Math.Max(endTime, length > 0 ? length : endTime) + 250;

            AllNotesConsumed = Notes.All(n => n.Resolved || !n.MustHit);

            if (songPosition > endThreshold && AllNotesConsumed)
            {
                IsFinished = true;
                OnSongEnd?.Invoke(this, EventArgs.Empty);
            }
        }

        ClearFrameInputs();
    }

    private void ClearFrameInputs()
    {
        Array.Clear(_lanePressedThisFrame);
        Array.Clear(_laneReleasedThisFrame);
    }

    private void SpawnDueNotes(double songPosition)
    {
        // Notes are spawned (visually) a couple of seconds before they reach the strumline.
        double spawnWindow = GetSpawnWindowMs();

        while (SpawnIndex < Notes.Count && Notes[SpawnIndex].Time - songPosition <= spawnWindow)
        {
            var note = Notes[SpawnIndex];
            OnNoteSpawn?.Invoke(this, note);
            SpawnIndex++;
        }
    }

    /// <summary>How far ahead of the strumline notes become visible, in ms.</summary>
    public double GetSpawnWindowMs() => 2000.0 / Math.Max(0.1f, Options.ScrollSpeed * Options.PlaybackRate);

    /// <summary>Distance in pixels a note travels per millisecond.</summary>
    public float PixelsPerMs => Constants.PIXELS_PER_MS * Options.ScrollSpeed * Options.PlaybackRate;

    private void ProcessInputs(double songPosition)
    {
        for (int lane = 0; lane < _laneHeld.Length; lane++)
        {
            if (_lanePressedThisFrame[lane]) HandlePress(lane, songPosition);
        }

        if (Options.Botplay) HandleBotplay(songPosition);
    }

    private void HandlePress(int lane, double songPosition)
    {
        var note = FindHittableNote(lane, songPosition);

        if (note is null)
        {
            // There is no note to hit: ghost tapping either does nothing or costs health.
            if (!Options.GhostTapping && !Options.Botplay)
            {
                Health += Constants.HEALTH_GHOST_MISS_PENALTY;
                ClampHealth();
            }

            return;
        }

        HitNote(note, songPosition);
    }

    private PlayNote? FindHittableNote(int lane, double songPosition)
    {
        PlayNote? best = null;
        double bestDelta = double.MaxValue;

        foreach (var note in Notes)
        {
            if (!note.MustHit || note.Hit || note.Missed) continue;
            if (note.Lane != lane) continue;

            double delta = Math.Abs(note.Time - songPosition);
            if (delta > Constants.HIT_WINDOW_MS) continue;
            if (delta >= bestDelta) continue;

            best = note;
            bestDelta = delta;
        }

        return best;
    }

    /// <summary>Hits a note immediately, judging it against the current song position.</summary>
    public NoteHitEventArgs HitNote(PlayNote note, double songPosition)
    {
        float offset = (float)(songPosition - note.Time);
        string judgement = Scoring.JudgeNote(offset, Options.ScoringSystem);
        int score = Scoring.ScoreNote(offset, Options.ScoringSystem);

        if (judgement == "miss")
        {
            // Outside of the window: count it as a miss rather than a hit.
            note.Missed = true;
            MissNote(note, songPosition, ghostMiss: true);
            return new NoteHitEventArgs
            {
                Note = note,
                Judgement = "miss",
                TimingOffset = offset,
                ScoreAwarded = 0,
                IsPlayer = true,
            };
        }

        note.Hit = true;
        note.HitOffset = offset;
        note.Judgement = judgement;
        note.ScoreAwarded = score;
        note.Holding = note.IsHold && _laneHeld[note.Lane];

        Combo++;
        if (Combo > MaxCombo) MaxCombo = Combo;
        Score += score;
        Tallies.RegisterHit(judgement, Combo);
        Health += GetHealthBonus(judgement);
        ClampHealth();

        var args = new NoteHitEventArgs
        {
            Note = note,
            Judgement = judgement,
            TimingOffset = offset,
            ScoreAwarded = score,
            IsPlayer = true,
        };

        OnNoteHit?.Invoke(this, args);
        return args;
    }

    public static float GetHealthBonus(string judgement) => judgement switch
    {
        "killer" => Constants.HEALTH_KILLER_BONUS,
        "sick" => Constants.HEALTH_SICK_BONUS,
        "good" => Constants.HEALTH_GOOD_BONUS,
        "bad" => Constants.HEALTH_BAD_BONUS,
        "shit" => Constants.HEALTH_SHIT_BONUS,
        _ => Constants.HEALTH_MISS_PENALTY,
    };

    private void HandleBotplay(double songPosition)
    {
        foreach (var note in Notes)
        {
            if (!note.MustHit || note.Hit || note.Missed) continue;
            if (songPosition < note.Time) continue;

            note.Holding = note.IsHold;
            HitNote(note, note.Time);
        }
    }

    private void ProcessHolds(double songPosition)
    {
        foreach (var note in Notes)
        {
            if (!note.Hit || !note.IsHold || note.HoldDropped) continue;

            if (note.Holding)
            {
                Health += Constants.HEALTH_HOLD_BONUS_PER_SECOND * (float)(1.0 / 60.0);
                ClampHealth();
            }

            if (songPosition >= note.EndTime)
            {
                note.Holding = false;
                note.HoldCompleted = true;
                continue;
            }

            bool held = Options.Botplay || IsLaneHeld(note.Lane);
            if (!held && note.Holding)
            {
                note.Holding = false;
                note.HoldDropped = true;
                Health += Constants.HEALTH_HOLD_DROP_PENALTY_MAX;
                ClampHealth();
                OnNoteHoldDrop?.Invoke(this, note);
            }
        }
    }

    private void ProcessMisses(double songPosition)
    {
        foreach (var note in Notes)
        {
            if (!note.MustHit || note.Hit || note.Missed) continue;
            if (songPosition <= note.Time + Constants.HIT_WINDOW_MS) continue;

            note.Missed = true;
            MissNote(note, songPosition, ghostMiss: false);
        }
    }

    private void MissNote(PlayNote note, double songPosition, bool ghostMiss)
    {
        Combo = 0;
        Tallies.Missed++;
        Health += Constants.HEALTH_MISS_PENALTY;
        ClampHealth();

        OnNoteMiss?.Invoke(this, new NoteMissEventArgs
        {
            Note = note,
            IsPlayer = true,
            IsGhostMiss = ghostMiss,
        });

        if (IsDead)
        {
            OnPlayerDeath?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ProcessEvents(double songPosition)
    {
        while (_eventIndex < Events.Count && Events[_eventIndex].Time <= songPosition)
        {
            var songEvent = Events[_eventIndex];
            songEvent.Activated = true;
            OnSongEvent?.Invoke(this, songEvent);
            _eventIndex++;
        }
    }

    // ------------------------------------------------------------------
    // Helpers used by the view
    // ------------------------------------------------------------------

    /// <summary>The note's Y position offset from the strumline, in pixels.</summary>
    public float GetNoteYOffset(PlayNote note, double songPosition)
    {
        float distance = (float)(note.Time - songPosition) * PixelsPerMs;
        if (note.Data.Kind is not null)
        {
            var kind = NoteKindLookup.Get(note.Data.Kind);
            if (kind?.InvertScroll == true) distance = -distance;
        }

        return Options.Downscroll ? -distance : distance;
    }

    /// <summary>Collects the results of the play into save data.</summary>
    public SaveScoreData BuildScoreData()
    {
        var tallies = Tallies.Clone();
        tallies.Combo = Combo;
        tallies.MaxCombo = MaxCombo;
        tallies.TotalNotes = TotalPlayerNotes;
        return new SaveScoreData { Score = Score, Tallies = tallies };
    }

    public ScoringRank? GetRank() => Scoring.CalculateRank(BuildScoreData());

    private void ClampHealth() => Health = MathUtil.Clamp(Health, Constants.HEALTH_MIN, Constants.HEALTH_MAX);

    /// <summary>Adds health directly (used by note kinds such as mines).</summary>
    public void ModifyHealth(float delta)
    {
        Health += delta;
        ClampHealth();
        if (IsDead) OnPlayerDeath?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Adds score directly (used by note kinds and events).</summary>
    public void ModifyScore(int delta) => Score += delta;

    /// <summary>Number of notes still to be resolved.</summary>
    public int RemainingNotes => Notes.Count(n => !n.Resolved);
}

/// <summary>Small helper which looks up note kinds without the view having to import the manager.</summary>
internal static class NoteKindLookup
{
    public static Notes.NoteKind? Get(string id) => Notes.NoteKindManager.Instance.GetNoteKind(id);
}
