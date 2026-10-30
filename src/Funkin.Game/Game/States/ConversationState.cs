using Funkin.Data.Dialogue;
using Funkin.Data.Song;
using Funkin.Flixel;
using Funkin.Paths;
using Funkin.Util;
using Microsoft.Xna.Framework;

namespace Funkin.Game.States;

/// <summary>
/// Port of funkin.play.cutscene.dialogue.Conversation: the pre-song dialogue cutscene.
/// Lines are read from `data/songs/&lt;id&gt;/dialogue.json` (or the legacy dialogue.txt), typed out
/// one character at a time and advanced with accept.
/// </summary>
public sealed class ConversationState : FlxState
{
    private readonly ConversationData _conversation;
    private readonly Action _onComplete;

    private FlxText _speakerText = null!;
    private FlxText _lineText = null!;
    private FlxText _promptText = null!;

    private int _lineIndex;
    private float _typeTimer;
    private int _visibleCharacters;
    private bool _lineComplete;

    private const float CharactersPerSecond = 45f;

    /// <summary>Automated playthroughs advance dialogue without input.</summary>
    private float _autoAdvanceTimer;

    public ConversationState(ConversationData conversation, Action onComplete)
    {
        _conversation = conversation;
        _onComplete = onComplete;
    }

    /// <summary>
    /// Returns a state that plays the song's dialogue first when one exists, otherwise the PlayState
    /// itself. Keeps the call sites (`new PlayState(...)`) simple.
    /// </summary>
    public static FlxState CreateForSong(string songId, string difficulty, string? weekId = null, int weekSongIndex = 0)
    {
        var playState = new PlayState(songId, difficulty)
        {
            WeekId = weekId,
            WeekSongIndex = weekSongIndex,
        };

        var conversation = LoadDialogue(songId);
        if (conversation is null || conversation.Lines.Count == 0) return playState;

        return new ConversationState(conversation, () => FlxG.SwitchState(playState));
    }

    private static ConversationData? LoadDialogue(string songId)
    {
        var json = FunkinPaths.Instance.ReadText($"data/songs/{songId}/dialogue.json");
        if (json is not null) return ConversationData.Parse(json);

        var legacy = FunkinPaths.Instance.ReadText($"data/songs/{songId}/dialogue.txt");
        if (legacy is null) return null;

        var conversation = new ConversationData { SongId = songId };
        foreach (var line in legacy.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            conversation.Lines.Add(ConversationData.ParseTextLine(line));
        }

        return conversation;
    }

    public override void Create()
    {
        FlxG.BackgroundColor = new Color(0, 0, 0);

        _speakerText = new FlxText(120, FunkinGame.GameHeight - 220, 400, string.Empty, 36)
        {
            ScrollFactor = Vector2.Zero,
            Color = new Color(255, 220, 60),
        };

        _lineText = new FlxText(120, FunkinGame.GameHeight - 170, FunkinGame.GameWidth - 240, string.Empty, 32)
        {
            ScrollFactor = Vector2.Zero,
        };

        _promptText = new FlxText(FunkinGame.GameWidth - 360, FunkinGame.GameHeight - 70, 300, "", 22)
        {
            ScrollFactor = Vector2.Zero,
            Color = new Color(180, 180, 180),
        };

        Add(_speakerText);
        Add(_lineText);
        Add(_promptText);

        ShowLine(0);
    }

    private void ShowLine(int index)
    {
        if (index >= _conversation.Lines.Count)
        {
            _onComplete();
            return;
        }

        _lineIndex = index;
        var line = _conversation.Lines[index];

        _speakerText.Text = line.Speaker.ToUpperInvariant();
        _lineText.Text = line.Text;
        _lineText.Alpha = 1f;
        _promptText.Text = string.Empty;

        _visibleCharacters = 0;
        _typeTimer = 0f;
        _lineComplete = false;

        // Reveal the text progressively by hiding the whole string and drawing a substring instead.
        _lineText.Text = string.Empty;
    }

    private void Advance()
    {
        if (!_lineComplete)
        {
            // Skip the typewriter effect.
            _visibleCharacters = _conversation.Lines[_lineIndex].Text.Length;
            _lineText.Text = _conversation.Lines[_lineIndex].Text;
            _lineComplete = true;
            _promptText.Text = "PRESS ENTER";
            return;
        }

        ShowLine(_lineIndex + 1);
    }

    public override void Update(float elapsed)
    {
        if (FlxG.Input.JustPressed(Funkin.Input.GameAction.Accept))
        {
            Advance();
        }

        if (CommandLine.IsSmokeTest)
        {
            // Give the typewriter effect a moment, then move on.
            _autoAdvanceTimer += elapsed;
            if (_autoAdvanceTimer > 0.6f)
            {
                _autoAdvanceTimer = 0f;
                Advance();
            }
        }

        if (!_lineComplete)
        {
            var line = _conversation.Lines[_lineIndex];
            _typeTimer += elapsed * CharactersPerSecond;

            int target = Math.Min(line.Text.Length, (int)_typeTimer);
            if (target != _visibleCharacters)
            {
                _visibleCharacters = target;
                _lineText.Text = line.Text[..target];

                if (target >= line.Text.Length)
                {
                    _lineComplete = true;
                    _promptText.Text = "PRESS ENTER";
                }
            }
        }

        base.Update(elapsed);
    }
}
