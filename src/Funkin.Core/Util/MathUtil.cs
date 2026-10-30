namespace Funkin.Util;

/// <summary>Port of the flixel.math helpers the game relies on.</summary>
public static class MathUtil
{
    public static float RoundDecimal(double value, int precision)
    {
        double result = Math.Round(value, precision, MidpointRounding.AwayFromZero);
        return (float)result;
    }

    public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    public static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

    public static float Clamp01(float value) => Clamp(value, 0f, 1f);

    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>Converts a value from one range to another, optionally clamped.</summary>
    public static float Remap(float value, float inMin, float inMax, float outMin, float outMax, bool clamp = false)
    {
        float t = (value - inMin) / (inMax - inMin);
        if (clamp) t = Clamp01(t);
        return outMin + (outMax - outMin) * t;
    }

    /// <summary>flixel.util.FlxAngle.getCartesianCoords / degToRad helper.</summary>
    public static float DegToRad(float degrees) => degrees * (MathF.PI / 180f);

    public static float RadToDeg(float radians) => radians * (180f / MathF.PI);

    public static float Wrap(float value, float min, float max)
    {
        float range = max - min;
        if (range <= 0) return min;
        float result = value - min;
        result %= range;
        if (result < 0) result += range;
        return result + min;
    }

    public static bool FuzzyEqual(float a, float b, float epsilon = 0.0001f) => MathF.Abs(a - b) <= epsilon;

    public static float Sin01(float t) => (MathF.Sin(t * MathF.PI * 2f) + 1f) * 0.5f;
}
