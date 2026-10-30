using Funkin.Audio;
using Funkin.Data.Song;
using Funkin.Flixel;
using Funkin.Play;
using Funkin.Save;
using Funkin.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Funkin.Game.States;

/// <summary>
/// Port of funkin.play.PlayState: the gameplay state. The musical simulation lives in
/// <see cref="PlaySession"/> (shared with the unit tests); this class is the view plus the glue to
/// the Conductor, stage, characters, audio and HUD.
/// </summary>
public sealed class PlayState : FlxState
{
    public static PlayState? Instance { get; private set; }

    private readonly string _songId;
    private readonly string _difficulty;
    private readonly string _variation;

    private SongDifficulty _song = null!;
    private PlaySession _session = null!;
    private StageSprite _stage = null!;
    private NoteStyleAssets.NoteStyleAssetsSet _noteStyle = null!;

    private readonly List<StrumNote> _playerStrums = new();
    private readonly List<StrumNote> _opponentStrums = new();
    private readonly List<NoteSprite> _noteSprites = new();
    private readonly Dictionary<PlayNote, NoteSprite> _noteLookup = new();

    private HealthBar _healthBar = null!;
    private FlxText _scoreText = null!;
    private FlxText _comboText = null!;
    private FlxText _infoText = null!;
    private FlxText _botplayText = null!;
    private FlxText _countdownText = null!;

    private VocalsPlayer _vocals = null!;
    private Conductor _conductor = null!;

    private bool _songStarted;
    private bool _songEnded;
    private bool _paused;
    private int _countdownStep;
    private float _countdownTimer;
    private float _cameraZoom = 1f;
    private Vector2 _cameraTarget;
    private float _beatBop;
    private double _fallbackPositionMs;
    private bool _hasInstrumental;

    /// <summary>Where to return to when the song ends or the player quits (`null` = back to Freeplay).</summary>
    public string? NextStateAfterResults { get; set; }

    /// <summary>The story week this play belongs to, if any.</summary>
    public string? WeekId { get; set; }

    /// <summary>Index of this song inside the week's song list (story mode only).</summary>
    public int WeekSongIndex { get; set; }

    public PlaySession Session => _session;
    public SongDifficulty Song => _song;

    public PlayState(string songId, string difficulty = Constants.DEFAULT_DIFFICULTY, string variation = Constants.DEFAULT_VARIATION)
    {
        _songId = songId;
        _difficulty = difficulty;
        _variation = variation;
    }

    public override void Create()
    {
        Instance = this;

        var song = SongRegistry.Instance.LoadSong(_songId);
        if (song is null)
        {
            Logger.Error($"Could not load song '{_songId}'.");
            FlxG.SwitchState(new FreeplayState());
            return;
        }

        _song = song.GetDifficulty(_difficulty, _variation)
                ?? song.GetDifficulty(Constants.DEFAULT_DIFFICULTY, _variation)
                ?? song.GetDifficulty(song.ListDifficulties(_variation).FirstOrDefault() ?? Constants.DEFAULT_DIFFICULTY, _variation)
                ?? throw new InvalidOperationException($"Song '{_songId}' has no playable difficulties.");

        var options = SaveManager.Instance.Data.Options;
        var playOptions = new PlaySessionOptions
        {
            Botplay = options.Botplay,
            GhostTapping = options.GhostTapping,
            Downscroll = options.Downscroll,
            NoteOffset = options.NoteOffset,
            AudioVisualOffset = options.AudioVisualOffset,
            ScrollSpeed = options.ScrollSpeedOverride ? (float)options.ScrollSpeed : _song.ScrollSpeed,
        };

        _conductor = Conductor.Instance;
        _conductor.ForceBpm(null);
        _session = new PlaySession(_song, playOptions, _conductor);
        _session.ShowScoreAsAccuracy = options.ScoreDisplayMode == "Accuracy";

        _noteStyle = NoteStyleAssets.Instance.Get(_song.NoteStyle);

        // Stage and characters.
        _stage = new StageSprite(_song.Stage);
        _stage.Create();
        _stage.AddCharacters(
            string.IsNullOrEmpty(_song.Characters.Player) ? Constants.DEFAULT_CHARACTER : _song.Characters.Player,
            string.IsNullOrEmpty(_song.Characters.Girlfriend) ? "gf" : _song.Characters.Girlfriend,
            string.IsNullOrEmpty(_song.Characters.Opponent) ? "dad" : _song.Characters.Opponent);

        BuildStrumlines();
        BuildHud();
        StartAudio();

        // Camera starts focused on the opponent.
        FocusCamera("dad", instant: true);
        Camera.Zoom = _stage.DefaultCameraZoom;

        _session.OnNoteSpawn += (_, note) => SpawnNote(note);
        _session.OnNoteHit += (_, args) => HandleNoteHit(args);
        _session.OnNoteMiss += (_, args) => HandleNoteMiss(args);
        _session.OnNoteHoldDrop += (_, note) => HandleHoldDrop(note);
        _session.OnSongEvent += (_, songEvent) => HandleSongEvent(songEvent);
        _session.OnPlayerDeath += (_, _) => HandleDeath();
        _session.OnSongEnd += (_, _) => HandleSongEnd();

        _conductor.OnBeatHit.Add(OnBeatHit);

        _countdownText.Text = "";
        _countdownStep = 0;
        _countdownTimer = 0f;
        _songStarted = false;

        Logger.Info($"Starting {_songId} ({_difficulty}/{_variation}) at {_song.GetStartingBpm()} BPM.");
    }

    // ------------------------------------------------------------------
    // Setup
    // ------------------------------------------------------------------

    private void BuildStrumlines()
    {
        int lanes = _session.Options.StrumlineSize;
        float spacing = StrumNote.NoteSize * 0.9f;
        float totalWidth = lanes * spacing;

        float baseY = _session.Options.Downscroll
            ? FunkinGame.GameHeight - 150
            : 50f;

        // Opponent strums on the left, player strums on the right (as the default skins do).
        float opponentX = (FunkinGame.GameWidth - totalWidth * 2 - 80) / 2f;
        float playerX = opponentX + totalWidth + 80;

        for (int lane = 0; lane < lanes; lane++)
        {
            var opponent = new StrumNote(lane, false, _noteStyle)
            {
                Position = new Vector2(opponentX + lane * spacing, baseY),
                ScrollFactor = Vector2.Zero,
            };
            _opponentStrums.Add(opponent);

            var player = new StrumNote(lane, true, _noteStyle)
            {
                Position = new Vector2(playerX + lane * spacing, baseY),
                ScrollFactor = Vector2.Zero,
            };
            _playerStrums.Add(player);
        }
    }

    private void BuildHud()
    {
        _healthBar = new HealthBar(SaveManager.Instance.Data.Options.Ectoplasm ? "face-ecto" : _song.Characters.Player, _song.Characters.Opponent);
        _healthBar.Layout(FunkinGame.GameWidth, FunkinGame.GameHeight);

        _scoreText = new FlxText(20, 20, 400, "", 32) { Alignment = TextAlign.Left, ScrollFactor = Vector2.Zero };
        _comboText = new FlxText(0, 220, FunkinGame.GameWidth, "", 72)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
        };

        _infoText = new FlxText(0, 20, FunkinGame.GameWidth, $"{_song.SongName} - {CoolUtil.DifficultyToString(_difficulty)}", 24)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
        };

        _botplayText = new FlxText(20, FunkinGame.GameHeight - 48, 400, "", 24) { ScrollFactor = Vector2.Zero };
        _countdownText = new FlxText(0, FunkinGame.GameHeight / 2f - 80, FunkinGame.GameWidth, "", 128)
        {
            Alignment = TextAlign.Center,
            ScrollFactor = Vector2.Zero,
        };

        Add(_scoreText);
        Add(_comboText);
        Add(_infoText);
        Add(_botplayText);
        Add(_countdownText);
    }

    private void StartAudio()
    {
        var instrumental = Paths.FunkinPaths.Instance.Instrumental(_song.Parent.Id);
        if (instrumental is null)
        {
            Logger.Warn($"No instrumental found for '{_song.Parent.Id}'; running without audio.");
            _session.Conductor.Music = null;
        }
        else
        {
            var music = FlxG.Sound.PlayMusic(instrumental, (float)SaveManager.Instance.Data.Volume, loop: false);
            _session.Conductor.Music = music;
            _hasInstrumental = music is not null;

            // The song should not start until the countdown finishes.
            music?.Pause();
            music?.Seek(0);
        }

        _vocals = new VocalsPlayer();
        _vocals.Load(_song);
        _vocals.BindInstrumental(FlxG.Sound.Music);
    }

    // ------------------------------------------------------------------
    // Update
    // ------------------------------------------------------------------

    public override void Update(float elapsed)
    {
        // Pause handling.
        if (!_paused && FlxG.Input.JustPressed(Funkin.Input.GameAction.Pause) && _songStarted && !_songEnded)
        {
            OpenPause();
            return;
        }

        if (FlxG.Input.JustPressed(Funkin.Input.GameAction.Reset) && FlxG.Input.Pressed(Microsoft.Xna.Framework.Input.Keys.LeftControl))
        {
            RestartSong();
            return;
        }

        if (FlxG.Input.JustPressed(Funkin.Input.GameAction.ToggleBotplay))
        {
            _session.Options.Botplay = !_session.Options.Botplay;
            SaveManager.Instance.Data.Options.Botplay = _session.Options.Botplay;
            SaveManager.Instance.MarkDirty();
        }

        if (FlxG.Input.JustPressed(Funkin.Input.GameAction.Screenshot))
        {
            Logger.Info("Screenshot key pressed (screenshots are handled by the platform layer).");
        }

        HandleInputs();

        if (!_songStarted)
        {
            UpdateCountdown(elapsed);
        }
        else
        {
            _conductor.Elapsed = elapsed;

            if (_hasInstrumental)
            {
                _conductor.Update();
            }
            else
            {
                // No audio available: drive the song from the frame clock so the chart still runs.
                _fallbackPositionMs += elapsed * 1000.0;
                _conductor.Update(_fallbackPositionMs, applyOffsets: false);
            }
            _session.Update(_conductor.SongPosition);
        }

        UpdateNoteVisuals(elapsed);
        UpdateHud(elapsed);
        UpdateCamera(elapsed);

        _stage.Update(elapsed);
        _healthBar.Update(elapsed);
        _vocals.Update(elapsed, 1f, 1f);

        base.Update(elapsed);
    }

    private void HandleInputs()
    {
        if (!_songStarted || _session.IsFinished) return;

        int lanes = _session.Options.StrumlineSize;

        foreach (var key in FlxG.Input.KeysJustPressed())
        {
            int lane = Funkin.Input.Controls.GetNoteLaneFor(key);
            if (lane < 0 || lane >= lanes) continue;
            _session.PressLane(lane);
            _playerStrums[lane].ShowPressed();
        }

        // Release detection: a lane is released when none of its keys are held.
        for (int lane = 0; lane < lanes; lane++)
        {
            bool held = false;
            var actions = lane < Funkin.Input.Controls.NoteActions.Length
                ? new[] { Funkin.Input.Controls.NoteActions[lane] }
                : Array.Empty<Funkin.Input.GameAction>();

            foreach (var action in actions)
            {
                if (FlxG.Input.Pressed(action))
                {
                    held = true;
                    break;
                }
            }

            if (!held && _session.IsLaneHeld(lane))
            {
                _session.ReleaseLane(lane);
                _playerStrums[lane].ShowStatic();
            }
        }

        int padLane = FlxG.Input.GamepadNoteJustPressed();
        if (padLane >= 0 && padLane < lanes) _session.PressLane(padLane);

        int padReleased = FlxG.Input.GamepadNoteReleased();
        if (padReleased >= 0 && padReleased < lanes) _session.ReleaseLane(padReleased);
    }

    private void UpdateCountdown(float elapsed)
    {
        _countdownTimer += elapsed;

        // Four beats of countdown at the song's starting BPM.
        float beatLength = 60f / Math.Max(1f, _song.GetStartingBpm());
        int step = (int)(_countdownTimer / beatLength);

        if (step != _countdownStep)
        {
            _countdownStep = step;
            _countdownText.Text = step switch
            {
                0 => "3",
                1 => "2",
                2 => "1",
                _ => "GO!",
            };
        }

        if (_countdownTimer >= beatLength * 4)
        {
            _countdownText.Text = "";
            StartSong();
        }
    }

    private void StartSong()
    {
        _songStarted = true;
        _session.Conductor.ResetPosition();

        if (FlxG.Sound.Music is not null)
        {
            FlxG.Sound.Music.Seek(0);
            FlxG.Sound.Music.Play();
        }

        _vocals.Play();
    }

    private void UpdateNoteVisuals(float elapsed)
    {
        for (int i = _noteSprites.Count - 1; i >= 0; i--)
        {
            var sprite = _noteSprites[i];
            var receptor = sprite.IsPlayer ? _playerStrums[sprite.Lane] : _opponentStrums[sprite.Lane];

            sprite.Update(elapsed);
            sprite.UpdatePlacement(_session, receptor);

            bool gone = sprite.Note.Missed && _session.SongPosition > sprite.Note.Time + 200;
            bool past = !sprite.Note.Missed && _session.SongPosition > sprite.Note.EndTime + 200;

            if (gone || past)
            {
                _noteSprites.RemoveAt(i);
                _noteLookup.Remove(sprite.Note);
            }
        }

        for (int lane = 0; lane < _playerStrums.Count; lane++)
        {
            _playerStrums[lane].Update(elapsed);
            _opponentStrums[lane].Update(elapsed);
        }
    }

    private void UpdateHud(float elapsed)
    {
        _healthBar.SetHealth(_session.Health);

        double displayScore = _session.ShowScoreAsAccuracy ? _session.Accuracy * 100 : _session.Score;
        _scoreText.Text = _session.ShowScoreAsAccuracy ? $"{displayScore:0.00}%" : _session.Score.ToString("N0");

        _comboText.Text = _session.Combo > 1 ? _session.Combo + "x" : string.Empty;
        _comboText.Color = _session.Combo >= 100 ? new Color(255, 215, 0) : Color.White;
        _botplayText.Text = _session.Options.Botplay ? "BOTPLAY" : string.Empty;

        _beatBop = Math.Max(0f, _beatBop - elapsed * 6f);
    }

    private void UpdateCamera(float elapsed)
    {
        float zoomBop = SaveManager.Instance.Data.Options.ZoomCamera ? _beatBop * 0.015f : 0f;
        Camera.Zoom = MathUtil.Lerp(Camera.Zoom, _cameraZoom + zoomBop, Math.Min(1f, elapsed * 3f));
    }

    // ------------------------------------------------------------------
    // Gameplay events
    // ------------------------------------------------------------------

    private void SpawnNote(PlayNote note)
    {
        var sprite = new NoteSprite(note, note.MustHit, _noteStyle);
        var receptor = note.MustHit ? _playerStrums[note.Lane] : _opponentStrums[note.Lane];
        sprite.UpdatePlacement(_session, receptor);

        _noteSprites.Add(sprite);
        _noteLookup[note] = sprite;

        // Opponent notes are auto-hit by the simulation when their time arrives; the view just
        // scrolls them in like the player's.
    }

    private void HandleNoteHit(NoteHitEventArgs args)
    {
        int lane = args.Note.Lane;
        bool isPlayer = args.IsPlayer;

        // Visual feedback: a splash on the receptor and a floating judgement label.
        var receptor = isPlayer ? _playerStrums[lane] : _opponentStrums[lane];
        Add(new NoteSplash(receptor.Position.X, receptor.Position.Y, lane));

        if (isPlayer)
        {
            string label = args.Judgement switch
            {
                "killer" => "KILLER!",
                "sick" => "SICK!",
                "good" => "GOOD",
                "bad" => "BAD",
                "shit" => "SHIT",
                _ => "MISS",
            };

            Add(new JudgementPopup(
                receptor.Position.X - 40,
                receptor.Position.Y + (session_downscroll() ? -90 : 90),
                label,
                JudgementPopup.ColorForJudgement(args.Judgement)));
        }

        if (isPlayer)
        {
            if (lane < _playerStrums.Count) _playerStrums[lane].ShowConfirm();
            _stage.Boyfriend?.Sing(lane, args.Note.Data.Kind == "alt-animation");
        }
        else
        {
            if (lane < _opponentStrums.Count) _opponentStrums[lane].ShowConfirm();
            _stage.Dad?.Sing(lane, args.Note.Data.Kind == "alt-animation");
        }

        if (_noteLookup.TryGetValue(args.Note, out var sprite))
        {
            _noteSprites.Remove(sprite);
            _noteLookup.Remove(args.Note);
            sprite.Destroy();
        }

        // Camera focus follows whoever is singing.
        FocusCamera(isPlayer ? "bf" : "dad");
    }

    private void HandleNoteMiss(NoteMissEventArgs args)
    {
        _stage.Boyfriend?.Sing(args.Note.Lane, miss: true);
        _vocals.MutePlayerFor(0.25);

        if (SaveManager.Instance.Data.Options.ZoomCamera) Camera.ShakeFor(6f, 0.15f);
    }

    private void HandleHoldDrop(PlayNote note)
    {
        _stage.Boyfriend?.Sing(note.Lane, miss: true);
    }

    private void HandleSongEvent(SongEventData songEvent)
    {
        switch (songEvent.EventKind)
        {
            case "Zoom Camera":
            {
                double zoom = songEvent.GetValueAsFloat("zoom", _cameraZoom);
                double duration = songEvent.GetValueAsFloat("duration", 1.0);
                _cameraZoom = (float)Math.Clamp(zoom, 0.5, 2.0);
                if (duration > 0) Camera.Lerp = (float)duration;
                break;
            }

            case "Add Camera Zoom":
            {
                _cameraZoom = (float)Math.Clamp(_cameraZoom + songEvent.GetValueAsFloat("zoom", 0.05), 0.5, 2.0);
                break;
            }

            case "Hey!":
                _stage.Boyfriend?.Sing(1, altAnimation: true);
                _stage.Dad?.Sing(1, altAnimation: true);
                break;

            case "Play Animation":
            {
                string target = songEvent.GetValueAsString("target", "dad");
                string animation = songEvent.GetValueAsString("anim", "idle");
                var character = target.ToLowerInvariant() switch
                {
                    "bf" or "boyfriend" => _stage.Boyfriend,
                    "gf" or "girlfriend" => _stage.Girlfriend,
                    _ => _stage.Dad,
                };
                character?.PlayAnimation(animation, force: true);
                break;
            }

            case "Change Character":
            {
                Logger.Info($"Change Character event: {songEvent.GetValueAsString("char")}");
                break;
            }
        }
    }

    private void OnBeatHit()
    {
        _beatBop = 1f;
        _stage.OnBeat(_conductor.CurrentBeat);
        _healthBar.OnBeat();

        foreach (var strum in _playerStrums) if (!strum.IsPressed) strum.ShowStatic();
        foreach (var strum in _opponentStrums) strum.ShowStatic();
    }

    private void HandleDeath()
    {
        Logger.Info("Player died.");
        FlxG.Sound.StopMusic();
        _vocals.Stop();
        FlxG.OpenSubState(new GameOverState(this));
        _paused = true;
    }

    private void HandleSongEnd()
    {
        if (_songEnded) return;
        _songEnded = true;

        _vocals.Stop();

        var score = _session.BuildScoreData();
        var save = SaveManager.Instance;
        save.CommitScore(_song.Parent.Id, _difficulty, _variation, score.Score, score.Tallies);

        if (WeekId is not null)
        {
            var week = Data.Story.LevelRegistry.Instance.Load(WeekId);
            if (week is not null && week.Songs.Count > 0)
            {
                double completion = Math.Clamp((double)(_session.Tallies.Sick + _session.Tallies.Good) / Math.Max(1, _session.Tallies.TotalNotes), 0, 1);
                save.SetWeekCompletion(WeekId, completion);
            }
        }

        save.Flush();
        FlxG.SwitchState(new ResultState(_song.Parent.Id, _difficulty, _variation, score, WeekId, WeekSongIndex));
    }

    // ------------------------------------------------------------------
    // Pause / restart helpers
    // ------------------------------------------------------------------

    public void OpenPause()
    {
        _paused = true;
        FlxG.Sound.Music?.Pause();
        _vocals.Pause();
        FlxG.OpenSubState(new PauseState(this));
    }

    public void ClosePause()
    {
        _paused = false;
        FlxG.Sound.Music?.Resume();
        _vocals.Resume();
    }

    public void RestartSong()
    {
        FlxG.Sound.StopMusic();
        _vocals.Unload();
        _conductor.OnBeatHit.RemoveAll();
        FlxG.SwitchState(new PlayState(_songId, _difficulty, _variation)
        {
            WeekId = WeekId,
            WeekSongIndex = WeekSongIndex,
        });
    }

    public void ExitToFreeplay()
    {
        FlxG.Sound.StopMusic();
        _vocals.Unload();
        _conductor.OnBeatHit.RemoveAll();
        FlxG.SwitchState(WeekId is null
            ? new FreeplayState()
            : new StoryMenuState());
    }

    private bool session_downscroll() => _session.Options.Downscroll;

    private void FocusCamera(string role, bool instant = false)
    {
        var characterPosition = _stage.GetCharacterPosition(role);
        if (characterPosition == Vector2.Zero) return;

        _cameraTarget = characterPosition + _stage.GetCameraOffsets(role);

        if (instant)
        {
            Camera.Scroll = _cameraTarget - new Vector2(FunkinGame.GameWidth / 2f, FunkinGame.GameHeight / 2f);
            Camera.Target = null;
            return;
        }

        Camera.Scroll = Vector2.Lerp(Camera.Scroll, _cameraTarget - new Vector2(FunkinGame.GameWidth / 2f, FunkinGame.GameHeight / 2f), 0.35f);
    }

    // ------------------------------------------------------------------
    // Draw
    // ------------------------------------------------------------------

    public override void Draw(SpriteBatch batch)
    {
        _stage.Draw(batch);

        foreach (var note in _noteSprites)
        {
            note.Sustain?.Draw(batch, Camera);
            note.Draw(batch, Camera);
        }

        foreach (var strum in _opponentStrums) strum.Draw(batch, Camera);
        foreach (var strum in _playerStrums) strum.Draw(batch, Camera);

        _healthBar.Draw(batch);
        base.Draw(batch);
    }

    public override void Destroy()
    {
        // Create() can bail out early when the song data is missing; guard against partial setup.
        _conductor?.OnBeatHit.RemoveAll();
        _vocals?.Unload();
        Instance = null;
        base.Destroy();
    }
}
