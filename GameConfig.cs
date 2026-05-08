// ============================================================
// FILE: GameConfig.cs — Centralized configuration with JSON support
// ============================================================
using System;
using System.IO;
using System.Text.Json;
using DungeonCrawler.utils;

namespace DungeonCrawler;

/// <summary>
/// All tweakable game values in one place.
/// Loads from config.json if it exists, otherwise uses defaults.
/// Missing fields in the JSON gracefully fall back to defaults.
/// </summary>
public class GameConfig
{
    // ── Singleton ──
    private static GameConfig _instance;
    public static GameConfig Instance => _instance ??= Load();

    private const string ConfigPath = "config.json";

    // ════════════════════════════════════════════
    //  DISPLAY
    // ════════════════════════════════════════════
    public int ScreenWidth { get; set; } = 960;
    public int ScreenHeight { get; set; } = 540;
    public bool Fullscreen { get; set; } = false;
    public bool VSync { get; set; } = true;

    // ════════════════════════════════════════════
    //  PLAYER STARTING STATS
    // ════════════════════════════════════════════
    public string DefaultPlayerName { get; set; } = "Hero";
    public int StartingMaxHp { get; set; } = 100;
    public int StartingAttack { get; set; } = 12;
    public int StartingDefense { get; set; } = 5;
    public int StartingSpeed { get; set; } = 7;
    public int StartingMagic { get; set; } = 8;
    public bool StartWithMagicWeapon { get; set; } = false; // false = Sword, true = Staff

    // ════════════════════════════════════════════
    //  COMBAT
    // ════════════════════════════════════════════
    public int MinDamage { get; set; } = 1;
    public int DefendBoost { get; set; } = 3;
    public float DefendBlockPercent { get; set; } = 0.40f;   // block 40% of incoming damage
    public float DefendCounterMultiplier { get; set; } = 0.5f; // counter for 50% of normal attack
    public int HealBase { get; set; } = 10;
    public float HealPercent { get; set; } = 0.50f;         // heals 50% of max HP
    public float MinHealPercent_Combat { get; set; } = 0.25f; // never heals below 25% of max HP
    public int HealCooldownTurns { get; set; } = 3;          // turns before heal is available again
    public float DamageVariance { get; set; } = 0.15f;
    public int CritChance { get; set; } = 5;              // base % chance to crit
    public float CritMultiplier { get; set; } = 1.75f;    // damage multiplier on crit
    public float SpeedCritBonus { get; set; } = 1.5f;     // extra crit % per point of speed advantage
    public float PreActionDelay { get; set; } = 0.6f;
    public float BetweenActionDelay { get; set; } = 0.8f;
    public int EnemyAttackChance { get; set; } = 70;   // % chance enemy picks Attack vs Magic
    public float FlashDuration { get; set; } = 0.3f;

    // ════════════════════════════════════════════
    //  ENEMY SCALING
    // ════════════════════════════════════════════
    public int EnemyBaseHp { get; set; } = 30;
    public int EnemyBaseAttack { get; set; } = 8;
    public int EnemyBaseDefense { get; set; } = 3;
    public int EnemyBaseSpeed { get; set; } = 5;
    public int EnemyBaseMagic { get; set; } = 4;
    public float EnemyScaleExponent { get; set; } = 1.6f;     // exponential curve to match loot
    public float EnemyScaleMultiplier { get; set; } = 0.3f;   // multiplied by depth^exponent

    // ════════════════════════════════════════════
    //  PROGRESSION
    // ════════════════════════════════════════════
    public int ScorePerDepth { get; set; } = 100;
    public float HealPercentBetweenFloors { get; set; } = 0.35f;
    public float HealDecayPerFloor { get; set; } = 0.015f;    // heal% drops by this each floor
    public float MinHealPercent { get; set; } = 0.10f;        // floor for heal%, never below this
    public int MaxHpBoostPerFloor { get; set; } = 1;
    public int AttackBoostPerFloor { get; set; } = 0;          // removed flat ATK — gear handles it now

    // ════════════════════════════════════════════
    //  MAP
    // ════════════════════════════════════════════
    public int   MapWidth  { get; set; } = 5;   // grid columns per floor
    public int   MapHeight { get; set; } = 4;   // grid rows    per floor

    // Rest room
    public float RestHealPercent { get; set; } = 0.75f;  // % of effective max HP restored

    // Elite enemy multipliers (applied on top of normal scaling)
    public float EliteHpMultiplier     { get; set; } = 1.6f;
    public float EliteAttackMultiplier { get; set; } = 1.4f;

    // Boss enemy multipliers
    public float BossHpMultiplier     { get; set; } = 2.5f;
    public float BossAttackMultiplier { get; set; } = 2.0f;

    // ════════════════════════════════════════════
    //  LOOT
    // ════════════════════════════════════════════
    public int LootChoiceCount { get; set; } = 3;
    public int LootTierDivisor { get; set; } = 2;    // tier = (depth-1) / this
    public int LootMaxTier { get; set; } = 4;
    public int LootBaseStatValue { get; set; } = 2;
    public int LootStatPerTier { get; set; } = 2;     // baseVal = LootBaseStatValue + tier * this
    public int CursedLootChance { get; set; } = 15;    // % chance an item is cursed (high stat + penalty)
    public int MaxRerollAttempts { get; set; } = 20;   // max re-rolls to guarantee an upgrade
    public float LootScaleExponent { get; set; } = 1.8f;
    public float LootScaleMultiplier { get; set; } = 2.5f;

    // ════════════════════════════════════════════
    //  LOAD / SAVE / GENERATE
    // ════════════════════════════════════════════

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Load config from disk. Returns defaults if file is missing or corrupt.
    /// </summary>
    public static GameConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                // Generate a default config file so users can see all options
                var defaults = new GameConfig();
                defaults.Save();
                GameLogger.Info("Generated default config.json");
                return defaults;
            }

            string json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<GameConfig>(json, JsonOpts);
            GameLogger.Info("Loaded config.json");
            return config ?? new GameConfig();
        }
        catch (Exception ex)
        {
            GameLogger.Error("Failed to load config.json, using defaults", ex);
            return new GameConfig();
        }
    }

    /// <summary>
    /// Write current config to disk (useful for generating a default file).
    /// </summary>
    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, JsonOpts);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            GameLogger.Error("Failed to save config.json", ex);
        }
    }

    /// <summary>
    /// Reload config from disk at runtime (e.g. for hot-reload during testing).
    /// </summary>
    public static void Reload()
    {
        _instance = Load();
        GameLogger.Info("Config reloaded");
    }

    /// <summary>
    /// Reset all values to compiled defaults, save to disk, and update the singleton.
    /// </summary>
    public static void ResetToDefaults()
    {
        _instance = new GameConfig();
        _instance.Save();
        GameLogger.Info("Config reset to defaults");
    }
}