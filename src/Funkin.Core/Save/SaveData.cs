using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Funkin.Save;

/// <summary>Port of Save.SaveScoreTallyData: the judgement counts for a play.</summary>
public sealed class SaveScoreTallyData
{
    [JsonPropertyName("sick")] public int Sick { get; set; }
    [JsonPropertyName("good")] public int Good { get; set; }
    [JsonPropertyName("bad")] public int Bad { get; set; }
    [JsonPropertyName("shit")] public int Shit { get; set; }
    [JsonPropertyName("missed")] public int Missed { get; set; }
    [JsonPropertyName("combo")] public int Combo { get; set; }
    [JsonPropertyName("maxCombo")] public int MaxCombo { get; set; }
    [JsonPropertyName("totalNotesHit")] public int TotalNotesHit { get; set; }
    [JsonPropertyName("totalNotes")] public int TotalNotes { get; set; }

    public SaveScoreTallyData Clone() => new()
    {
        Sick = Sick,
        Good = Good,
        Bad = Bad,
        Shit = Shit,
        Missed = Missed,
        Combo = Combo,
        MaxCombo = MaxCombo,
        TotalNotesHit = TotalNotesHit,
        TotalNotes = TotalNotes,
    };

    /// <summary>Total number of judgements recorded (hits plus misses).</summary>
    [JsonIgnore]
    public int TotalJudgements => Sick + Good + Bad + Shit + Missed;

    public void RegisterHit(string judgement, int combo)
    {
        switch (judgement)
        {
            case "sick": Sick++; break;
            case "good": Good++; break;
            case "bad": Bad++; break;
            case "shit": Shit++; break;
            default: Missed++; break;
        }

        if (judgement != "miss")
        {
            TotalNotesHit++;
            Combo = combo;
            if (combo > MaxCombo) MaxCombo = combo;
        }
    }
}

/// <summary>Port of Save.SaveScoreData.</summary>
public sealed class SaveScoreData
{
    [JsonPropertyName("score")] public int Score { get; set; }
    [JsonPropertyName("tallies")] public SaveScoreTallyData Tallies { get; set; } = new();

    public SaveScoreData Clone() => new() { Score = Score, Tallies = Tallies.Clone() };
}

/// <summary>Port of Save.SaveDataOptions: the player's gameplay preferences.</summary>
public sealed class SaveDataOptions
{
    [JsonPropertyName("framerate")] public int Framerate { get; set; } = 60;
    [JsonPropertyName("naughtyness")] public bool Naughtyness { get; set; } = true;
    [JsonPropertyName("downscroll")] public bool Downscroll { get; set; }
    [JsonPropertyName("flashingLights")] public bool FlashingLights { get; set; } = true;
    [JsonPropertyName("zoomCamera")] public bool ZoomCamera { get; set; }
    [JsonPropertyName("debugDisplay")] public string DebugDisplay { get; set; } = "Off";
    [JsonPropertyName("debugDisplayBGOpacity")] public int DebugDisplayBgOpacity { get; set; } = 50;
    [JsonPropertyName("autoPause")] public bool AutoPause { get; set; } = true;
    [JsonPropertyName("ghostTapping")] public bool GhostTapping { get; set; } = true;
    [JsonPropertyName("guitarHeroSolo")] public bool GuitarHeroSolo { get; set; }
    [JsonPropertyName("noReset")] public bool NoReset { get; set; }
    [JsonPropertyName("botplay")] public bool Botplay { get; set; }
    [JsonPropertyName("accuracyMode")] public bool AccuracyMode { get; set; }
    [JsonPropertyName("noteOffset")] public int NoteOffset { get; set; }
    [JsonPropertyName("noteOffsetStep")] public int NoteOffsetStep { get; set; } = 5;
    [JsonPropertyName("audioVisualOffset")] public int AudioVisualOffset { get; set; }
    [JsonPropertyName("noteCameraMovement")] public bool NoteCameraMovement { get; set; } = true;
    [JsonPropertyName("menuCameraMovement")] public bool MenuCameraMovement { get; set; } = true;
    [JsonPropertyName("scoreDisplayMode")] public string ScoreDisplayMode { get; set; } = "Score";
    [JsonPropertyName("scrollSpeed")] public double ScrollSpeed { get; set; } = 1.0;
    [JsonPropertyName("scrollSpeedOverride")] public bool ScrollSpeedOverride { get; set; }
    [JsonPropertyName("noteStyle")] public string NoteStyle { get; set; } = "funkin";
    [JsonPropertyName("hitsoundVolume")] public double HitsoundVolume { get; set; } = 1.0;
    [JsonPropertyName("controllerMode")] public bool ControllerMode { get; set; }
    [JsonPropertyName("controllerScheme")] public string ControllerScheme { get; set; } = "Default";
    [JsonPropertyName("language")] public string Language { get; set; } = "en-US";
    [JsonPropertyName("ectoplasm")] public bool Ectoplasm { get; set; }
    [JsonPropertyName("disabledMobileControls")] public List<string> DisabledMobileControls { get; set; } = new();
}

/// <summary>Port of Save.SaveDataMobileOptions.</summary>
public sealed class SaveDataMobileOptions
{
    [JsonPropertyName("deviceModel")] public string DeviceModel { get; set; } = string.Empty;
    [JsonPropertyName("hideMobileControls")] public bool HideMobileControls { get; set; }
    [JsonPropertyName("mobileControlsAlpha")] public double MobileControlsAlpha { get; set; } = 0.5;
    [JsonPropertyName("mobileControlsOffset")] public double MobileControlsOffset { get; set; } = 15;
    [JsonPropertyName("mobileButtonSize")] public string MobileButtonSize { get; set; } = "Default";
}

/// <summary>The full contents of the save file (`funkin-save.json`).</summary>
public sealed class SaveData
{
    [JsonPropertyName("version")] public string VersionString { get; set; } = "1.0.0";
    [JsonPropertyName("options")] public SaveDataOptions Options { get; set; } = new();
    [JsonPropertyName("mobileOptions")] public SaveDataMobileOptions MobileOptions { get; set; } = new();
    [JsonPropertyName("volume")] public double Volume { get; set; } = 1.0;
    [JsonPropertyName("mute")] public bool Mute { get; set; }
    [JsonPropertyName("ngSessionId")] public string? NewgroundsSessionId { get; set; }
    [JsonPropertyName("enabledModDirs")] public List<string> EnabledModDirs { get; set; } = new();
    [JsonPropertyName("charactersSeen")] public List<string> CharactersSeen { get; set; } = new();
    [JsonPropertyName("oldChar")] public bool OldChar { get; set; }

    /// <summary>Scores keyed by $songId:$difficulty:$variation.</summary>
    [JsonPropertyName("scores")]
    public Dictionary<string, SaveScoreData> Scores { get; set; } = new();

    /// <summary>Completion percentage per week id, 0..1.</summary>
    [JsonPropertyName("weekCompletion")]
    public Dictionary<string, double> WeekCompletion { get; set; } = new();

    /// <summary>Unlocked songs / weeks, keyed by id.</summary>
    [JsonPropertyName("unlockedSongs")]
    public List<string> UnlockedSongs { get; set; } = new();

    [JsonPropertyName("modOptions")]
    public Dictionary<string, JsonNode?> ModOptions { get; set; } = new();

    [JsonIgnore] public SaveDataOptions OptionsRef => Options;

    public static string GetScoreKey(string songId, string difficulty, string variation)
        => $"{songId}:{difficulty}:{variation}";

    public SaveScoreData? GetScore(string songId, string difficulty, string variation)
        => Scores.TryGetValue(GetScoreKey(songId, difficulty, variation), out var score) ? score : null;

    public void SetScore(string songId, string difficulty, string variation, SaveScoreData score)
        => Scores[GetScoreKey(songId, difficulty, variation)] = score;
}
