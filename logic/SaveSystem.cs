// ============================================================
// FILE: logic/SaveSystem.cs
// ============================================================
using System;
using System.IO;
using System.Text.Json;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

/// <summary>
/// Serializable snapshot of everything needed to resume a run.
/// </summary>
public class SaveData
{
    public int CurrentDepth { get; set; }
    public int TotalKills { get; set; }
    public int Score { get; set; }

    // Player stats
    public string PlayerName { get; set; } = "Hero";
    public int MaxHp { get; set; }
    public int Hp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Magic { get; set; }

    public DateTime SavedAt { get; set; }
}

public static class SaveSystem
{
    private static readonly string SaveDir =
        Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
            "DungeonCrawler/local/playersaves");

    private static readonly string SavePath =
        Path.Combine(SaveDir, "save.json");

    /// <summary>Save current run to disk.</summary>
    public static void Save(Fighter player, DepthManager depth)
    {
        var data = new SaveData
        {
            CurrentDepth = depth.CurrentDepth,
            TotalKills   = depth.TotalKills,
            Score        = depth.Score,

            PlayerName = player.Stats.Name,
            MaxHp      = player.Stats.MaxHp,
            Hp         = player.Stats.Hp,
            Attack     = player.Stats.Attack,
            Defense    = player.Stats.Defense,
            Speed      = player.Stats.Speed,
            Magic      = player.Stats.Magic,

            SavedAt = DateTime.Now
        };

        Directory.CreateDirectory(SaveDir);

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(SavePath, json);
    }

    /// <summary>Load a saved run. Returns null if no save exists.</summary>
    public static SaveData Load()
    {
        if (!File.Exists(SavePath))
            return null;

        try
        {
            string json = File.ReadAllText(SavePath);
            return JsonSerializer.Deserialize<SaveData>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Delete the save file (on death or completed run).</summary>
    public static void Delete()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    /// <summary>Check if a save file exists.</summary>
    public static bool HasSave() => File.Exists(SavePath);
}