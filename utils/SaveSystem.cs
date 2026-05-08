// ============================================================
// FILE: utils/SaveSystem.cs — JSON-based save/load
// ============================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DungeonCrawler.models;
using DungeonCrawler.logic;

namespace DungeonCrawler.utils;

/// <summary>Serializable state of a single map room.</summary>
public class RoomSaveData
{
    public int       X     { get; set; }
    public int       Y     { get; set; }
    public RoomType  Type  { get; set; }
    public RoomState State { get; set; }
}

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

    // Map state (null when saved between floors — a fresh map will be generated)
    public int            MapWidth   { get; set; }
    public int            MapHeight  { get; set; }
    public int            MapPlayerX { get; set; }
    public int            MapPlayerY { get; set; }
    public RoomSaveData[] MapRooms   { get; set; }

    public DateTime SavedAt { get; set; }

    /// <summary>
    /// Build SaveData from live game objects.
    /// Pass <paramref name="map"/> to persist the current floor layout;
    /// omit it (or pass null) when saving between floors so a fresh map
    /// is generated on the next continue.
    /// </summary>
    public static SaveData FromGame(Fighter player, DepthManager depth, DungeonMap? map = null)
    {
        var gear = player.Equipment;
        var data = new SaveData
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

        if (map != null)
        {
            data.MapWidth   = map.Width;
            data.MapHeight  = map.Height;
            data.MapPlayerX = map.PlayerX;
            data.MapPlayerY = map.PlayerY;

            // Size is known up-front, so write straight into the result array
            // instead of going via a List + ToArray copy.
            var rooms = new RoomSaveData[map.Width * map.Height];
            int idx = 0;
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                {
                    var r = map.GetRoom(x, y);
                    rooms[idx++] = new RoomSaveData { X = x, Y = y, Type = r.Type, State = r.State };
                }
            data.MapRooms = rooms;
        }

        return data;
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

    /// <summary>
    /// Save current run to disk.
    /// Pass <paramref name="map"/> to include the current floor layout in the save.
    /// </summary>
    public static void Save(Fighter player, DepthManager depth, DungeonMap? map = null)
    {
        try
        {
            Directory.CreateDirectory(SaveDir);
            var data = SaveData.FromGame(player, depth, map);
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