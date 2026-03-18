using System;
using System.IO;

namespace DungeonCrawler.utils;

public static class GameLogger
{
    private static readonly string LogDir =
        Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
            "DungeonCrawler", "logs");

    private static readonly string LogFile =
        Path.Combine(LogDir, $"game_{DateTime.Now:yyyy-MM-dd}.log");

    static GameLogger()
    {
        Directory.CreateDirectory(LogDir);
    }

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Warn(string message)
    {
        Write("WARN", message);
    }

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
        try
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}";
            File.AppendAllText(LogFile, line + Environment.NewLine);
        }
        catch
        {
            // If logging itself fails, silently ignore
        }
    }
}