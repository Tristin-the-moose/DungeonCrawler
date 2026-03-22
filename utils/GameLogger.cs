// ============================================================
// FILE: utils/GameLogger.cs — Simple file logger
// ============================================================
using System;
using System.IO;

namespace DungeonCrawler.utils;

public static class GameLogger
{
    private static readonly string LogDir =
        Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
            "DungeonCrawler", "logs");

    private static readonly object Lock = new();
    private static StreamWriter _writer;
    private static string _currentDate;

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception ex = null)
    {
        Write("ERROR", message);
        if (ex != null)
        {
            Write("ERROR", $"  Exception: {ex.GetType().Name}: {ex.Message}");
            Write("ERROR", $"  Stack: {ex.StackTrace}");

            if (ex.InnerException != null)
                Write("ERROR", $"  Inner: {ex.InnerException.Message}");
        }
    }

    private static void Write(string level, string message)
    {
        lock (Lock)
        {
            try
            {
                EnsureWriter();
                _writer.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}");
                _writer.Flush();
            }
            catch
            {
                // If logging itself fails, silently ignore
            }
        }
    }

    /// <summary>
    /// Opens a writer if needed, or rolls to a new file at midnight.
    /// Replaces File.AppendAllText which opened/closed the file on every call.
    /// </summary>
    private static void EnsureWriter()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        if (_writer != null && _currentDate == today) return;

        _writer?.Dispose();
        Directory.CreateDirectory(LogDir);
        string path = Path.Combine(LogDir, $"game_{today}.log");
        _writer = new StreamWriter(path, append: true);
        _currentDate = today;
    }

    /// <summary>
    /// Call from Game1.UnloadContent() or Program.cs to cleanly close the log.
    /// </summary>
    public static void Shutdown()
    {
        lock (Lock)
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
        }
    }
}