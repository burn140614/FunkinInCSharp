namespace Funkin.Util;

/// <summary>Port of funkin.util.CoolUtil (the grab-bag helper class).</summary>
public static class CoolUtil
{
    /// <summary>Capitalizes the first letter of a string, leaving the rest untouched.</summary>
    public static string Capitalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpperInvariant(input[0]) + input[1..];
    }

    /// <summary>Formats a difficulty name for display: 'easy' -> 'Easy', 'nightmare' -> 'Nightmare'.</summary>
    public static string DifficultyToString(string difficulty) => Capitalize(difficulty.ToLowerInvariant());

    public static int DifficultyToListIndex(string difficulty)
    {
        int index = difficulty switch
        {
            "easy" => 0,
            "normal" => 1,
            "hard" => 2,
            _ => -1,
        };
        return index;
    }

    public static string ListIndexToDifficulty(int index) => index switch
    {
        0 => "easy",
        1 => "normal",
        2 => "hard",
        _ => Constants.DEFAULT_DIFFICULTY,
    };

    public static void BindAll<T>(IReadOnlyList<T> list, Action<T> action)
    {
        foreach (var item in list) action(item);
    }

    /// <summary>Formats a time in milliseconds as m:ss.</summary>
    public static string FormatTime(double ms, bool showHours = false)
    {
        var span = TimeSpan.FromMilliseconds(Math.Max(0, ms));
        return showHours ? $"{(int)span.TotalHours}:{span.Minutes:D2}:{span.Seconds:D2}" : $"{span.Minutes}:{span.Seconds:D2}";
    }

    public static T RandomFrom<T>(IReadOnlyList<T> list, Random? rng = null)
    {
        rng ??= Random.Shared;
        return list[rng.Next(list.Count)];
    }

    public static string NumberToString(int number, int minDigits)
    {
        var str = number.ToString();
        while (str.Length < minDigits) str = "0" + str;
        return str;
    }

    /// <summary>Truncates a float to the given number of decimal places, as FlxMath.roundDecimal does.</summary>
    public static float RoundDecimal(double value, int precision) => MathUtil.RoundDecimal(value, precision);

    public static float CoolLerp(float a, float b, float ratio) => MathUtil.Lerp(a, b, ratio);
}
