// ============================================================
// FILE: logic/LootFactory.cs — Procedural loot generation
// ============================================================
using System;
using System.Collections.Generic;
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
    private static readonly Dictionary<EquipmentSlots, string[][]> NameTables = new()
    {
        [EquipmentSlots.HeadPiece] = new[] {
            new[] { "Cloth Hood", "Leather Cap", "Worn Helmet" },
            new[] { "Iron Helm", "Chain Coif", "Scout Visor" },
            new[] { "Steel Helm", "Mage Crown", "War Helmet" },
            new[] { "Mythril Helm", "Shadow Hood", "Dragon Visor" },
            new[] { "Legendary Crown", "Void Helm", "God Helm" }
        },
        [EquipmentSlots.ChestPiece] = new[] {
            new[] { "Cloth Tunic", "Leather Vest", "Padded Shirt" },
            new[] { "Iron Cuirass", "Chain Mail", "Ranger Coat" },
            new[] { "Steel Plate", "Mage Robe", "War Armor" },
            new[] { "Mythril Plate", "Shadow Cloak", "Dragon Mail" },
            new[] { "Legendary Plate", "Void Armor", "God Armor" }
        },
        [EquipmentSlots.Leggings] = new[] {
            new[] { "Cloth Pants", "Leather Leggings", "Padded Pants" },
            new[] { "Iron Greaves", "Chain Legs", "Ranger Chaps" },
            new[] { "Steel Greaves", "Mage Skirt", "War Tassets" },
            new[] { "Mythril Greaves", "Shadow Legs", "Dragon Greaves" },
            new[] { "Legendary Greaves", "Void Legs", "God Greaves" }
        },
        [EquipmentSlots.Booties] = new[] {
            new[] { "Leather Sandals", "Worn Boots", "Cloth Shoes" },
            new[] { "Iron Boots", "Chain Boots", "Scout Boots" },
            new[] { "Steel Sabatons", "Mage Slippers", "War Boots" },
            new[] { "Mythril Boots", "Shadow Steps", "Dragon Boots" },
            new[] { "Legendary Boots", "Void Treads", "God Boots" }
        },
        [EquipmentSlots.Weapon] = new[] {
            new[] { "Wooden Sword", "Rusty Dagger", "Oak Staff", "Twig Wand", "Stone Mace" },
            new[] { "Iron Sword", "Steel Dagger", "Arcane Staff", "Iron Wand", "Iron Mace" },
            new[] { "Steel Blade", "Assassin Blade", "Mystic Staff", "Crystal Wand", "War Mace" },
            new[] { "Mythril Sword", "Shadow Blade", "Dragon Staff", "Void Wand", "Mythril Mace" },
            new[] { "Legendary Blade", "Void Edge", "God Staff", "God Wand", "God Mace" }
        },
        [EquipmentSlots.Ring] = new[] {
            new[] { "Copper Band", "Wooden Ring", "Bone Ring" },
            new[] { "Iron Ring", "Silver Band", "Scout Ring" },
            new[] { "Steel Ring", "Mage Ring", "War Signet" },
            new[] { "Mythril Ring", "Shadow Band", "Dragon Ring" },
            new[] { "Legendary Ring", "Void Band", "God Ring" }
        },
        [EquipmentSlots.Necklace] = new[] {
            new[] { "Cord Necklace", "Bone Pendant", "Wooden Charm" },
            new[] { "Iron Chain", "Silver Amulet", "Scout Pendant" },
            new[] { "Steel Locket", "Mage Amulet", "War Medallion" },
            new[] { "Mythril Pendant", "Shadow Amulet", "Dragon Charm" },
            new[] { "Legendary Amulet", "Void Pendant", "God Amulet" }
        }
    };

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

        // 1. Pick unique slots so you never see 3 helmets
        var allSlots    = Enum.GetValues<EquipmentSlots>();
        var shuffled    = allSlots.OrderBy(_ => rng.Next()).ToArray();
        var chosenSlots = shuffled.Take(count).ToArray();

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
                for (int attempt = 0; attempt < cfg.MaxRerollAttempts; attempt++)
                {
                    choices[0] = GenerateDrop(depth, rng, chosenSlots[0], context);
                    if (IsUpgrade(choices[0], currentGear))
                        break;
                }
            }
        }

        return choices;
    }

    /// <summary>
    /// Generate a single equipment drop for a specific slot.
    /// Rarity drives both colour and the number of stat bonuses the item carries
    /// (white 0 → yellow 4). See <see cref="SlotStatPools"/>.
    /// </summary>
    public static Equipment GenerateDrop(
        int depth, Random rng,
        EquipmentSlots? forceSlot = null,
        LootContext context       = LootContext.Battle)
    {
        var cfg  = GameConfig.Instance;
        var slot = forceSlot ?? RandomSlot(rng);

        // Weighted rarity roll — deeper floors weight toward higher rarities (= more bonuses).
        // Boss context layers an extra depth-scaled chance to roll a guaranteed yellow.
        int rarity = RollRarity(depth, rng, cfg, context);

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
        }

        return item;
    }

    /// <summary>
    /// Weighted rarity roll. Higher depths shift the probability toward rarer tiers.
    /// Rarity 0 = White (junk), 1 = Green, 2 = Blue, 3 = Purple, 4 = Yellow.
    ///
    /// After the normal weighted roll, the context can enforce a minimum rarity:
    ///   • Boss     – depth-scaled chance to lock in yellow.
    ///   • Treasure – depth-scaled chance to floor at purple (still allowed to
    ///                roll up to yellow on the normal path).
    /// </summary>
    private static int RollRarity(int depth, Random rng, GameConfig cfg, LootContext context)
    {
        // 1. Base depth-driven weighted roll
        int baseRarity = Math.Clamp((depth - 1) / cfg.LootTierDivisor, 0, cfg.LootMaxTier);

        int roll = rng.Next(100);
        int rarity;
        if (roll < 50)
            rarity = baseRarity;
        else if (roll < 80)
            rarity = baseRarity - 1;
        else if (roll < 95)
            rarity = baseRarity + 1;
        else
            rarity = baseRarity + 2;

        rarity = Math.Clamp(rarity, 0, cfg.LootMaxTier);

        // 2. Context-specific minimum-rarity boosts
        switch (context)
        {
            case LootContext.Boss:
            {
                int yellowChance = cfg.BossYellowBaseChance + depth * cfg.BossYellowDepthBonus;
                if (rng.Next(100) < yellowChance)
                    rarity = cfg.LootMaxTier; // Yellow
                break;
            }
            case LootContext.Treasure:
            {
                int purpleChance = cfg.TreasurePurpleBaseChance + depth * cfg.TreasurePurpleDepthBonus;
                if (rng.Next(100) < purpleChance)
                    rarity = Math.Max(rarity, 3); // At least Purple — natural Yellow rolls survive.
                break;
            }
        }

        return rarity;
    }

    /// <summary>
    /// Apply a random stat penalty to make a cursed item a tradeoff, not a free upgrade.
    /// </summary>
    private static void ApplyCurse(Equipment item, Random rng)
    {
        // Pick a stat to penalize that isn't the item's primary stat
        int curse = rng.Next(3);
        int penalty = rng.Next(2, 5);

        switch (curse)
        {
            case 0: item.SpeedBoost -= penalty;    break;
            case 1: item.DefenseBonus -= penalty;  break;
            case 2: item.HealthBonus -= penalty;   break;
        }
    }

    /// <summary>Check if an item is better than what's currently equipped in that slot.</summary>
    private static bool IsUpgrade(Equipment item, EquipmentSet gear)
    {
        var current = gear.Get(item.EquipmentType);
        if (current == null) return true;
        return item.TotalStats > current.TotalStats;
    }

    private static EquipmentSlots RandomSlot(Random rng)
    {
        var slots = Enum.GetValues<EquipmentSlots>();
        return slots[rng.Next(slots.Length)];
    }

    // ── Stat pools ──────────────────────────────────────────
    // The first entry in each list is the slot's identity stat (rolled first
    // and gets the largest value). Subsequent entries are added in order as
    // rarity climbs — yellow items (rarity 4) roll the first 4 entries.
    private enum StatKey { Health, Attack, Magic, Defense, Protection, Speed }

    private static readonly Dictionary<EquipmentSlots, StatKey[]> SlotStatPools = new()
    {
        [EquipmentSlots.HeadPiece]  = new[] { StatKey.Health,  StatKey.Defense, StatKey.Magic,      StatKey.Protection },
        [EquipmentSlots.ChestPiece] = new[] { StatKey.Defense, StatKey.Health,  StatKey.Protection, StatKey.Magic      },
        [EquipmentSlots.Leggings]   = new[] { StatKey.Defense, StatKey.Speed,   StatKey.Health,     StatKey.Protection },
        [EquipmentSlots.Booties]    = new[] { StatKey.Speed,   StatKey.Defense, StatKey.Health,     StatKey.Protection },
        [EquipmentSlots.Ring]       = new[] { StatKey.Magic,   StatKey.Attack,  StatKey.Defense,    StatKey.Speed      },
        [EquipmentSlots.Necklace]   = new[] { StatKey.Health,  StatKey.Magic,   StatKey.Protection, StatKey.Defense    },
    };

    private static readonly Dictionary<WeaponType, StatKey[]> WeaponStatPools = new()
    {
        [WeaponType.Sword]  = new[] { StatKey.Attack, StatKey.Defense, StatKey.Speed,   StatKey.Health     },
        [WeaponType.Dagger] = new[] { StatKey.Attack, StatKey.Speed,   StatKey.Defense, StatKey.Health     },
        [WeaponType.Staff]  = new[] { StatKey.Magic,  StatKey.Health,  StatKey.Speed,   StatKey.Protection },
        [WeaponType.Wand]   = new[] { StatKey.Magic,  StatKey.Speed,   StatKey.Health,  StatKey.Protection },
        [WeaponType.Mace]   = new[] { StatKey.Attack, StatKey.Defense, StatKey.Health,  StatKey.Speed      },
    };

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
            pool = WeaponStatPools[item.Weapon.Value];
        else if (!SlotStatPools.TryGetValue(item.EquipmentType, out pool))
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
        if (!NameTables.TryGetValue(EquipmentSlots.Weapon, out var table))
            return "Unknown Weapon";

        var names = table[Math.Min(tier, table.Length - 1)];
        int index = (int)type;
        return index < names.Length ? names[index] : names[rng.Next(names.Length)];
    }

    private static string PickName(EquipmentSlots slot, int tier, Random rng)
    {
        if (!NameTables.TryGetValue(slot, out var table))
            return "Unknown Gear";

        var names = table[Math.Min(tier, table.Length - 1)];
        return names[rng.Next(names.Length)];
    }
}