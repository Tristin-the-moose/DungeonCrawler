// ============================================================
// FILE: utils/SaveSystem.cs — JSON-based save/load
// ============================================================
using System;
using System.IO;
using System.Text.Json;
using DungeonCrawler.models;
using DungeonCrawler.logic;

namespace DungeonCrawler.utils;

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

    // Equipment
    public Equipment HeadPiece { get; set; }
    public Equipment ChestPiece { get; set; }
    public Equipment Leggings { get; set; }
    public Equipment Booties { get; set; }
    public Equipment Ring { get; set; }
    public Equipment Necklace { get; set; }
    public Equipment Weapon { get; set; }

    public DateTime SavedAt { get; set; }

    /// <summary>
    /// Build SaveData from live game objects.
    /// Single source of truth for the mapping — adding a new stat
    /// only requires updating this one method + the properties above.
    /// </summary>
    public static SaveData FromGame(Fighter player, DepthManager depth)
    {
        var gear = player.Equipment;
        return new SaveData
        {
            CurrentDepth = depth.CurrentDepth,
            TotalKills   = depth.TotalKills,
            Score        = depth.Score,
            PlayerName   = player.Stats.Name,
            MaxHp        = player.Stats.MaxHp,
            Hp           = player.Stats.Hp,
            Attack       = player.Stats.Attack,
            Defense      = player.Stats.Defense,
            Speed        = player.Stats.Speed,
            Magic        = player.Stats.Magic,
            HeadPiece    = gear?.HeadPiece,
            ChestPiece   = gear?.ChestPiece,
            Leggings     = gear?.Leggings,
            Booties      = gear?.Booties,
            Ring         = gear?.Ring,
            Necklace     = gear?.Necklace,
            Weapon       = gear?.Weapon,
            SavedAt      = DateTime.Now
        };
    }
}

public static class SaveSystem
{
    private static readonly string SaveDir =
        Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
            "DungeonCrawler", "local", "playersaves");

    private static readonly string SavePath =
        Path.Combine(SaveDir, "save.json");

    // Reuse a single options instance — avoids allocating one per save
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    /// <summary>Save current run to disk.</summary>
    public static void Save(Fighter player, DepthManager depth)
    {
        try
        {
            Directory.CreateDirectory(SaveDir);
            var data = SaveData.FromGame(player, depth);
            string json = JsonSerializer.Serialize(data, JsonOpts);
            File.WriteAllText(SavePath, json);
            GameLogger.Info($"Game saved (Depth {depth.CurrentDepth})");
        }
        catch (Exception ex)
        {
            GameLogger.Error("Failed to save game", ex);
        }
    }

    /// <summary>Load a saved run. Returns null if no save exists.</summary>
    public static SaveData Load()
    {
        if (!HasSave()) return null;

        try
        {
            string json = File.ReadAllText(SavePath);
            var data = JsonSerializer.Deserialize<SaveData>(json);
            GameLogger.Info($"Save loaded (Depth {data?.CurrentDepth})");
            return data;
        }
        catch (Exception ex)
        {
            GameLogger.Error("Failed to load save", ex);
            return null;
        }
    }

    /// <summary>Delete the save file (on death or completed run).</summary>
    public static void Delete()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                GameLogger.Info("Save file deleted");
            }
        }
        catch (Exception ex)
        {
            GameLogger.Error("Failed to delete save", ex);
        }
    }

    /// <summary>Check if a save file exists.</summary>
    public static bool HasSave() => File.Exists(SavePath);
}