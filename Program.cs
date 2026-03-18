using System;
using DungeonCrawler;
using DungeonCrawler.utils;

try
{
    GameLogger.Info("=== Game Starting ===");
    using var game = new Game1();
    game.Run();
    GameLogger.Info("=== Game Closed Normally ===");
}
catch (Exception ex)
{
    GameLogger.Error("FATAL CRASH", ex);

    // Also write a crash dump file for easy finding
    string crashFile = $"logs/CRASH_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
    try
    {
        System.IO.File.WriteAllText(crashFile,
            $"DungeonCrawler Crash Report\n" +
            $"Time: {DateTime.Now}\n" +
            $"Error: {ex.Message}\n\n" +
            $"Stack Trace:\n{ex.StackTrace}\n\n" +
            $"Full Exception:\n{ex}");
    }
    catch { }

    throw; // Re-throw so the OS still reports the crash
}
