using Funkin.Data.Song;

namespace Funkin.Util;

/// <summary>Port of funkin.util.Constants.</summary>
public static class Constants
{
    public const string TITLE = "Friday Night Funkin'";
    public const string VERSION = "0.10.2";
    public const string GENERATED_BY = "FunkinCSharp";
    public const int BUILD_NUMBER = 105;

    public const string URL_ITCH = "https://ninja-muffin24.itch.io/funkin";
    public const string URL_NEWGROUNDS = "https://www.newgrounds.com/portal/view/770371";
    public const string URL_KICKSTARTER = "https://www.kickstarter.com/projects/funkin/friday-night-funkin-the-full-ass-game/";
    public const string URL_MERCH_FALLBACK = "https://needlejuicerecords.com/en-ca/pages/friday-night-funkin";

    // Colors, stored as 0xAARRGGBB to match FlxColor literals in the Haxe source.
    public const uint COLOR_HEALTH_BAR_RED = 0xFFFF0000;
    public const uint COLOR_HEALTH_BAR_GREEN = 0xFF66FF33;
    public const uint COLOR_PRELOADER_BG = 0xFF000000;
    public const uint COLOR_PRELOADER_BAR = 0xFFA4FF11;
    public const uint COLOR_PRELOADER_LOCK_BG = 0xFF1B1717;
    public const uint COLOR_PRELOADER_LOCK_FG = 0xB96F10;
    public const uint COLOR_PRELOADER_LOCK_FONT = 0xFFCCCCCC;
    public const uint COLOR_PRELOADER_LOCK_LINK = 0xFFEEB211;

    /// <summary>Arrow colors, in the same order as the note data lanes: left, down, up, right.</summary>
    public static readonly uint[] COLOR_NOTES =
    {
        0xFFC24B99, // purple
        0xFF00FFFF, // cyan
        0xFF12FA05, // green
        0xFFF9393F, // red
    };

    public const string DEFAULT_DIFFICULTY = "normal";

    public static readonly string[] DEFAULT_DIFFICULTY_LIST = { "easy", "normal", "hard" };
    public static readonly string[] DEFAULT_DIFFICULTY_LIST_ERECT = { "erect", "nightmare" };

    public static readonly string[] DEFAULT_DIFFICULTY_LIST_FULL =
    {
        "easy", "normal", "hard", "erect", "nightmare",
    };

    public const string DEFAULT_CHARACTER = "bf";
    public const string DEFAULT_HEALTH_ICON = "face";
    public const string DEFAULT_STAGE = "mainStage";
    public const string DEFAULT_SONG = "tutorial";
    public const string DEFAULT_VARIATION = "default";
    public const string DEFAULT_STICKER_PACK = "default";
    public const string DEFAULT_NOTE_STYLE = "funkin";
    public const string DEFAULT_PIXEL_NOTE_STYLE = "pixel";
    public const string DEFAULT_ALBUM_ID = "volume1";
    public const string DEFAULT_OST_NAME = "OFFICIAL OST";

    public const float DEFAULT_BOP_INTENSITY = 1.015f;
    public const int DEFAULT_ZOOM_RATE = 4;
    public const int DEFAULT_ZOOM_OFFSET = 0;
    public const int DEFAULT_PROP_RATE = 1;
    public const float DEFAULT_BPM = 100.0f;
    public const float DEFAULT_SCROLLSPEED = 1.0f;
    public const int DEFAULT_TIME_SIGNATURE_NUM = 4;
    public const int DEFAULT_TIME_SIGNATURE_DEN = 4;

    public const string DEFAULT_SONGNAME = "Unknown";
    public const string DEFAULT_ARTIST = "Unknown";
    public const string DEFAULT_CHARTER = "Unknown";

    public const string ANIMATION_HOLD_SUFFIX = "-hold";
    public const string ANIMATION_END_SUFFIX = "-end";

    // Gameplay tuning.
    public const float PIXELS_PER_MS = 0.45f;
    public const float HIT_WINDOW_MS = 160.0f;
    public const int SECS_PER_MIN = 60;
    public const int MS_PER_SEC = 1000;
    public const int STEPS_PER_BEAT = 4;
    public const int BEAT_PER_MEASURE = 4;
    public const float DEFAULT_PREVIEW_START_TIME = 0f;
    public const float DEFAULT_PREVIEW_END_TIME = 0.2f;

    public static readonly SongTimeFormat DEFAULT_TIMEFORMAT = SongTimeFormat.Milliseconds;

    // Rank thresholds.
    public const float RANK_PERFECT_THRESHOLD = 1.0f;
    public const float RANK_EXCELLENT_THRESHOLD = 0.9f;
    public const float RANK_GREAT_THRESHOLD = 0.8f;
    public const float RANK_GOOD_THRESHOLD = 0.7f;

    // Health bar (values match Constants.hx).
    public const float HEALTH_MIN = 0.0f;
    public const float HEALTH_MAX = 2.0f;
    public const float HEALTH_STARTING = HEALTH_MAX / 2.0f;
    public const float HEALTH_BAR_SCALE = 1f;
    public const float HEALTH_KILLER_BONUS = 2.0f / 100.0f * HEALTH_MAX;
    public const float HEALTH_SICK_BONUS = 1.5f / 100.0f * HEALTH_MAX;
    public const float HEALTH_GOOD_BONUS = 0.75f / 100.0f * HEALTH_MAX;
    public const float HEALTH_BAD_BONUS = 0.0f / 100.0f * HEALTH_MAX;
    public const float HEALTH_SHIT_BONUS = -1.0f / 100.0f * HEALTH_MAX;
    public const float HEALTH_HOLD_BONUS_PER_SECOND = 6.0f / 100.0f * HEALTH_MAX;
    public const float HEALTH_MISS_PENALTY = -4.0f / 100.0f * HEALTH_MAX;
    public const float HEALTH_GHOST_MISS_PENALTY = -4.0f / 100.0f * HEALTH_MAX;
    public const float HEALTH_HOLD_DROP_PENALTY_PER_SECOND = 0f;
    public const float HEALTH_HOLD_DROP_PENALTY_MAX = 0f;
    public const float HEALTH_MINE_PENALTY = -15.0f / 100.0f * HEALTH_MAX;

    /// <summary>Fallback character id used when a character's data file is missing.</summary>
    public const string FallbackCharacter = "bf";

    /// <summary>Time the countdown takes before the song starts, in ms (4 steps).</summary>
    public const float COUNTDOWN_LENGTH_MS = 3000f;

    // Note sizes (pixels, at 1x scroll).
    public const float NOTE_WIDTH = 112f;
    public const float NOTE_HEIGHT = 112f;
    public const float NOTE_STRUM_X_OFFSET = 50f;
    public const float NOTE_STRUM_Y_OFFSET = 50f;
}
