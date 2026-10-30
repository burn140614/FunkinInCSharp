using Funkin.Util;

namespace Funkin.Save;

/// <summary>
/// Port of funkin.save.Save: owns the singleton save data and persists it to `funkin-save.json`.
/// Writes are debounced so gameplay code can mark the save dirty every frame without hammering the disk.
/// </summary>
public sealed class SaveManager
{
    private static SaveManager? _instance;
    public static SaveManager Instance => _instance ??= new SaveManager();

    public const string FileName = "funkin-save.json";

    public string SaveDirectory { get; private set; } = Path.Combine(Directory.GetCurrentDirectory(), "saves");

    public string SaveFilePath => Path.Combine(SaveDirectory, FileName);

    public string BackupFilePath => Path.Combine(SaveDirectory, "funkin-save-backup.json");

    public SaveData Data { get; private set; } = new();

    public bool IsDirty { get; private set; }

    /// <summary>Fired after a successful load, so the UI can refresh bound values.</summary>
    public event Action? OnLoad;

    public event Action? OnSave;

    public void Initialize(string saveDirectory)
    {
        SaveDirectory = saveDirectory;
        Directory.CreateDirectory(SaveDirectory);
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                var json = File.ReadAllText(SaveFilePath);
                var loaded = Json.Deserialize<SaveData>(json);
                if (loaded is not null)
                {
                    Data = loaded;
                    Logger.Info($"Loaded save data from {SaveFilePath}");
                }
                else
                {
                    Logger.Warn("Save file was unreadable, starting from defaults.");
                    Data = new SaveData();
                }
            }
            else
            {
                Logger.Info("No save file found, creating a new one.");
                Data = new SaveData();
                IsDirty = true;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load save data: {ex.Message}");
            Data = new SaveData();
        }

        ApplyDefaults();
        OnLoad?.Invoke();
    }

    private void ApplyDefaults()
    {
        Data.Options ??= new SaveDataOptions();
        Data.Scores ??= new Dictionary<string, SaveScoreData>();
        Data.WeekCompletion ??= new Dictionary<string, double>();
        Data.UnlockedSongs ??= new List<string>();
        Data.ModOptions ??= new();
    }

    public void Flush(bool force = false)
    {
        if (!IsDirty && !force) return;

        try
        {
            Directory.CreateDirectory(SaveDirectory);
            if (File.Exists(SaveFilePath))
            {
                File.Copy(SaveFilePath, BackupFilePath, true);
            }

            var json = Json.Serialize(Data);
            string tempPath = SaveFilePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, SaveFilePath, true);

            IsDirty = false;
            Logger.Debug($"Saved game data to {SaveFilePath}");
            OnSave?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save game data: {ex.Message}");
        }
    }

    /// <summary>Marks the save as needing a write on the next flush.</summary>
    public void MarkDirty() => IsDirty = true;

    /// <summary>Records the result of a played chart, keeping the best score.</summary>
    public void CommitScore(string songId, string difficulty, string variation, int score, SaveScoreTallyData tallies)
    {
        var existing = Data.GetScore(songId, difficulty, variation);
        if (existing is null || score > existing.Score)
        {
            Data.SetScore(songId, difficulty, variation, new SaveScoreData
            {
                Score = score,
                Tallies = tallies.Clone(),
            });
        }

        MarkDirty();
        Flush();
    }

    public void SetWeekCompletion(string weekId, double completion)
    {
        Data.WeekCompletion[weekId] = Math.Clamp(completion, 0.0, 1.0);
        MarkDirty();
    }

    /// <summary>Sets the volume for the game, 0..1.</summary>
    public void SetVolume(double volume)
    {
        Data.Volume = Math.Clamp(volume, 0.0, 1.0);
        MarkDirty();
    }

    public void SetMuted(bool muted)
    {
        Data.Mute = muted;
        MarkDirty();
    }

    /// <summary>Resets everything to factory defaults (used by the Options menu).</summary>
    public void ResetToDefaults()
    {
        Data = new SaveData();
        MarkDirty();
        Flush(true);
        OnLoad?.Invoke();
    }
}
