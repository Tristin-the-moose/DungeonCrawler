// ============================================================
// FILE: logic/LootFactory.cs — Procedural loot generation
// ============================================================
// Static data lives in sibling files:
//   • LootNameTables — per-slot/tier item names
//   • LootStatPools  — StatKey enum + slot/weapon stat pools
//   • LootRarity     — weighted rarity roll with context boosts
// This file keeps just the orchestration and bonus application.
using System;
using System.Linq;
using DungeonCrawler;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

/// <summary>
/// What kind of fight or pickup produced this loot pool.
/// Drives reroll behaviour and rarity boosts in <see cref="LootFactory.GenerateChoices"/>.
/// </summary>
public enum LootContext
{
    Battle,    // Standard fight: 3 random items, no reroll guarantee
    Elite,     // Elite fight: reroll until at least one item is an upgrade
    Boss,      // Boss fight: reroll for upgrade AND boosted chance at yellows
    Treasure   // Chest: depth-scaled chance at minimum purple, no reroll
}

public static class LootFactory
{
    // Cached once — Enum.GetValues allocates a fresh array on each call.
    private static readonly EquipmentSlots[] AllSlots = Enum.GetValues<EquipmentSlots>();

    /// <summary>
    /// Generate loot choices with unique slots. Reroll behaviour depends on
    /// <paramref name="context"/>:
    ///   • Battle   – pure random, no upgrade guarantee.
    ///   • Elite    – reroll the first item until at least one choice is an upgrade.
    ///   • Boss     – same as Elite + a depth-scaled chance for items to roll yellow.
    ///   • Treasure – reroll for upgrade + a depth-scaled chance to floor at purple.
    /// </summary>
    public static Equipment[] GenerateChoices(
        int depth, Random rng,
        EquipmentSet currentGear = null,
        LootContext context      = LootContext.Battle)
    {
        var cfg = GameConfig.Instance;
        int count = cfg.LootChoiceCount;

        // 1. Pick `count` unique slots via partial Fisher-Yates over a stack
        //    buffer. Properly uniform, and the only heap alloc is the result.
        var chosenSlots = new EquipmentSlots[count];
        Span<EquipmentSlots> pool = stackalloc EquipmentSlots[AllSlots.Length];
        AllSlots.AsSpan().CopyTo(pool);
        for (int i = 0; i < count; i++)
        {
            int j = i + rng.Next(pool.Length - i);
            (pool[i], pool[j]) = (pool[j], pool[i]);
            chosenSlots[i] = pool[i];
        }

        // 2. Generate one item per slot, honouring the context's rarity rules
        var choices = new Equipment[count];
        for (int i = 0; i < count; i++)
            choices[i] = GenerateDrop(depth, rng, chosenSlots[i], context);

        // 3. Reroll-for-upgrade for any "premium" context (Elite, Boss, Treasure).
        bool guaranteeUpgrade = context == LootContext.Elite
                             || context == LootContext.Boss
                             || context == LootContext.Treasure;
        if (guaranteeUpgrade && currentGear != null)
        {
            bool hasUpgrade = choices.Any(item => IsUpgrade(item, currentGear));
            if (!hasUpgrade)
            {
                // Reroll the slot in our hand whose currently-equipped item has
                // the lowest TotalStats — best odds of producing an upgrade.
                int rerollIdx   = 0;
                int lowestStats = int.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    int currentStats = currentGear.Get(chosenSlots[i])?.TotalStats ?? 0;
                    if (currentStats < lowestStats)
                    {
                        lowestStats = currentStats;
                        rerollIdx   = i;
                    }
                }

                for (int attempt = 0; attempt < cfg.MaxRerollAttempts; attempt++)
                {
                    choices[rerollIdx] = GenerateDrop(depth, rng, chosenSlots[rerollIdx], context);
                    if (IsUpgrade(choices[rerollIdx], currentGear))
                        break;
                }
            }
        }

        return choices;
    }

    /// <summary>
    /// Generate a single equipment drop for a specific slot.
    /// Rarity drives both colour and the number of stat bonuses the item carries
    /// (white 0 → yellow 4).
    /// </summary>
    public static Equipment GenerateDrop(
        int depth, Random rng,
        EquipmentSlots? forceSlot = null,
        LootContext context       = LootContext.Battle)
    {
        var cfg  = GameConfig.Instance;
        var slot = forceSlot ?? RandomSlot(rng);

        // Weighted rarity roll — see LootRarity.Roll for the formula and
        // context-specific minimum-rarity boosts (Boss / Treasure).
        int rarity = LootRarity.Roll(depth, rng, cfg, context);

        var item = new Equipment
        {
            Name          = PickName(slot, rarity, rng),
            EquipmentType = slot,
            Rarity        = rarity
        };

        // Pick the weapon type up-front so the weapon stat pool is selectable
        // and the name reflects the actual weapon.
        if (slot == EquipmentSlots.Weapon)
        {
            item.Weapon = (WeaponType)rng.Next(5);
            item.Name   = PickWeaponName(item.Weapon.Value, rarity, rng);
        }

        // Cursed: only meaningful on items that have at least one bonus to boost.
        bool isCursed = rarity > 0 && rng.Next(100) < cfg.CursedLootChance;

        ApplyBonuses(item, rarity, rng, isCursed);

        if (isCursed)
        {
            ApplyCurse(item, rng);
            item.Name = "Cursed " + item.Name;
            item.Rarity = 5;
        }

        return item;
    }

    /// <summary>
    /// Apply a random stat penalty to make a cursed item a tradeoff, not a free upgrade.
    /// </summary>
    private static void ApplyCurse(Equipment item, Random rng)
    {
        int curse   = rng.Next(3);
        int penalty = rng.Next(2, 5);
        switch (curse)
        {
            case 0: item.SpeedBoost   -= penalty; break;
            case 1: item.DefenseBonus -= penalty; break;
            case 2: item.HealthBonus  -= penalty; break;
        }
    }

    /// <summary>Check if an item is better than what's currently equipped in that slot.</summary>
    private static bool IsUpgrade(Equipment item, EquipmentSet gear)
    {
        var current = gear.Get(item.EquipmentType);
        if (current == null) return true;
        return item.TotalStats > current.TotalStats;
    }

    private static EquipmentSlots RandomSlot(Random rng) =>
        AllSlots[rng.Next(AllSlots.Length)];

    /// <summary>
    /// Roll <paramref name="rarity"/> stat bonuses onto the item, in pool order.
    /// The primary stat takes the full base value; secondaries decay so the
    /// item still has a clear identity.
    /// </summary>
    private static void ApplyBonuses(Equipment item, int rarity, Random rng, bool isCursed)
    {
        if (rarity <= 0) return;     // White items carry zero bonuses.

        StatKey[] pool;
        if (item.EquipmentType == EquipmentSlots.Weapon && item.Weapon.HasValue)
            pool = LootStatPools.Weapons[item.Weapon.Value];
        else if (!LootStatPools.Slots.TryGetValue(item.EquipmentType, out pool))
            return;

        int bonusCount = Math.Min(rarity, pool.Length);
        for (int i = 0; i < bonusCount; i++)
        {
            int value = ComputeStatValue(rarity, i, rng);
            if (i == 0 && isCursed) value = (int)(value * 1.6f);
            AddStat(item, pool[i], value);
        }
    }

    /// <summary>
    /// Exponential base value, identical scaling to the old tier formula:
    ///   baseVal = LootBaseStatValue + LootScaleMultiplier · rarity^LootScaleExponent + small random
    /// Secondary stats (statIndex > 0) decay so the primary remains the highlight.
    /// </summary>
    private static int ComputeStatValue(int rarity, int statIndex, Random rng)
    {
        var cfg = GameConfig.Instance;
        float scaled = cfg.LootScaleMultiplier * MathF.Pow(rarity, cfg.LootScaleExponent);
        int baseVal  = cfg.LootBaseStatValue + (int)scaled + rng.Next(0, rarity + 2);

        if (statIndex == 0) return baseVal;
        return Math.Max(1, (int)(baseVal * Math.Pow(0.6, statIndex)));
    }

    private static void AddStat(Equipment item, StatKey key, int value)
    {
        switch (key)
        {
            case StatKey.Health:     item.HealthBonus     += value; break;
            case StatKey.Attack:     item.AttackBonus     += value; break;
            case StatKey.Magic:      item.MagicBonus      += value; break;
            case StatKey.Defense:    item.DefenseBonus    += value; break;
            case StatKey.Protection: item.ProtectionBonus += value; break;
            case StatKey.Speed:      item.SpeedBoost      += value; break;
        }
    }

    public static string PickWeaponName(WeaponType type, int tier, Random rng)
    {
        if (!LootNameTables.Names.TryGetValue(EquipmentSlots.Weapon, out var table))
            return "Unknown Weapon";

        var names = table[Math.Min(tier, table.Length - 1)];
        int index = (int)type;
        return index < names.Length ? names[index] : names[rng.Next(names.Length)];
    }

    private static string PickName(EquipmentSlots slot, int tier, Random rng)
    {
        if (!LootNameTables.Names.TryGetValue(slot, out var table))
            return "Unknown Gear";

        var names = table[Math.Min(tier, table.Length - 1)];
        return names[rng.Next(names.Length)];
    }
}
