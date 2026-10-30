using System.Globalization;

namespace Funkin.Util;

public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
}

/// <summary>
/// Minimal replacement for FlxG.log / trace(). Writes to the console and keeps a rolling buffer
/// that the in-game debug console can display.
/// </summary>
public static class Logger
{
    private static readonly object Sync = new();
    private static readonly List<string> Buffer = new();
    private const int MaxBufferLines = 500;

    public static LogLevel MinimumLevel { get; set; } = LogLevel.Info;

    public static event Action<string>? OnLog;

    public static IReadOnlyList<string> History
    {
        get
        {
            lock (Sync) return Buffer.ToArray();
        }
    }

    public static void Log(LogLevel level, string message)
    {
        if (level < MinimumLevel) return;

        var line = string.Create(CultureInfo.InvariantCulture, $"[{DateTime.Now:HH:mm:ss}] [{level.ToString().ToUpperInvariant()}] {message}");
        lock (Sync)
        {
            Buffer.Add(line);
            if (Buffer.Count > MaxBufferLines) Buffer.RemoveRange(0, Buffer.Count - MaxBufferLines);
        }

        Console.WriteLine(line);
        OnLog?.Invoke(line);
    }

    public static void Trace(string message) => Log(LogLevel.Trace, message);
    public static void Debug(string message) => Log(LogLevel.Debug, message);
    public static void Info(string message) => Log(LogLevel.Info, message);
    public static void Warn(string message) => Log(LogLevel.Warning, message);
    public static void Error(string message) => Log(LogLevel.Error, message);
}
