// ============================================================
// FILE: logic/LootFactory.cs — Procedural loot generation
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;
using DungeonCrawler;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

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
    /// Generate loot choices with guaranteed variety and at least one upgrade.
    /// </summary>
    public static Equipment[] GenerateChoices(int depth, Random rng, EquipmentSet currentGear = null)
    {
        var cfg = GameConfig.Instance;
        int count = cfg.LootChoiceCount;

        // 1. Pick unique slots so you never see 3 helmets
        var allSlots = Enum.GetValues<EquipmentSlots>();
        var shuffled = allSlots.OrderBy(_ => rng.Next()).ToArray();
        var chosenSlots = shuffled.Take(count).ToArray();

        // 2. Generate one item per slot
        var choices = new Equipment[count];
        for (int i = 0; i < count; i++)
            choices[i] = GenerateDrop(depth, rng, chosenSlots[i]);

        // 3. Guarantee at least one upgrade if player gear is known
        if (currentGear != null)
        {
            bool hasUpgrade = choices.Any(item => IsUpgrade(item, currentGear));

            if (!hasUpgrade)
            {
                // Re-roll the first item until it's an upgrade (with a safety cap)
                for (int attempt = 0; attempt < cfg.MaxRerollAttempts; attempt++)
                {
                    choices[0] = GenerateDrop(depth, rng, chosenSlots[0]);
                    if (IsUpgrade(choices[0], currentGear))
                        break;
                }
            }
        }

        return choices;
    }

    /// <summary>
    /// Generate a single equipment drop for a specific slot.
    /// </summary>
    public static Equipment GenerateDrop(int depth, Random rng, EquipmentSlots? forceSlot = null)
    {
        var cfg = GameConfig.Instance;
        var slot = forceSlot ?? RandomSlot(rng);

        // Weighted tier roll — deeper floors have better odds at higher tiers
        int tier = RollTier(depth, rng, cfg);

        var item = new Equipment
        {
            Name = PickName(slot, tier, rng),
            EquipmentType = slot,
            Rarity = tier + 1
        };

        // Exponential base value: baseVal = base + multiplier * tier^exponent + random
        // Tier 0: ~2, Tier 1: ~7, Tier 2: ~11, Tier 3: ~19, Tier 4: ~30+
        float scaled = cfg.LootScaleMultiplier * MathF.Pow(tier, cfg.LootScaleExponent);
        int baseVal = cfg.LootBaseStatValue + (int)scaled + rng.Next(0, tier + 2);

        // Cursed item check — high primary stat but a penalty elsewhere
        bool isCursed = rng.Next(100) < cfg.CursedLootChance;
        if (isCursed)
            baseVal = (int)(baseVal * 1.6f);  // Boosted primary stat

        ApplySlotBonuses(item, slot, baseVal, tier, rng);

        if (isCursed)
        {
            ApplyCurse(item, rng);
            item.Name = "Cursed " + item.Name;
        }

        // Weapon type override for weapon-slot items
        if (slot == EquipmentSlots.Weapon && item.Weapon.HasValue)
            item.Name = isCursed ? "Cursed " + PickWeaponName(item.Weapon.Value, tier, rng)
                                 : PickWeaponName(item.Weapon.Value, tier, rng);

        return item;
    }

    /// <summary>
    /// Weighted tier roll. Higher depths shift the probability toward rarer tiers.
    /// Tier 0 = Common, 1 = Uncommon, 2 = Rare, 3 = Epic, 4 = Legendary
    /// </summary>
    private static int RollTier(int depth, Random rng, GameConfig cfg)
    {
        // Base tier from depth
        int baseTier = Math.Clamp((depth - 1) / cfg.LootTierDivisor, 0, cfg.LootMaxTier);

        // Roll: 50% base tier, 30% base-1, 15% base+1, 5% base+2
        int roll = rng.Next(100);
        int tier;
        if (roll < 50)
            tier = baseTier;
        else if (roll < 80)
            tier = baseTier - 1;
        else if (roll < 95)
            tier = baseTier + 1;
        else
            tier = baseTier + 2;

        return Math.Clamp(tier, 0, cfg.LootMaxTier);
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

    private static void ApplySlotBonuses(Equipment item, EquipmentSlots slot,
                                          int baseVal, int tier, Random rng)
    {
        switch (slot)
        {
            case EquipmentSlots.HeadPiece:
                item.HealthBonus = baseVal;
                item.MagicBonus = rng.Next(0, tier * 2 + 1);
                break;
            case EquipmentSlots.ChestPiece:
                item.DefenseBonus = baseVal;
                item.HealthBonus = rng.Next(1, tier * 2 + 2);
                break;
            case EquipmentSlots.Leggings:
                item.DefenseBonus = (int)(baseVal * 0.7f);
                item.SpeedBoost = rng.Next(0, tier * 2 + 1);
                break;
            case EquipmentSlots.Booties:
                item.SpeedBoost = baseVal;
                item.DefenseBonus = rng.Next(0, tier * 2 + 1);
                break;
            case EquipmentSlots.Weapon:
                var weaponType = (WeaponType)rng.Next(5);
                item.Weapon = weaponType;
                switch (weaponType)
                {
                    case WeaponType.Sword:
                        item.AttackBonus = baseVal;
                        break;
                    case WeaponType.Dagger:
                        item.AttackBonus = (int)(baseVal * 0.7f);
                        item.SpeedBoost = rng.Next(1, tier * 2 + 3);
                        break;
                    case WeaponType.Staff:
                        item.MagicBonus = baseVal;
                        break;
                    case WeaponType.Wand:
                        item.MagicBonus = (int)(baseVal * 0.7f);
                        item.SpeedBoost = rng.Next(1, tier * 2 + 3);
                        break;
                    case WeaponType.Mace:
                        item.AttackBonus = (int)(baseVal * 0.7f);
                        item.DefenseBonus = rng.Next(1, tier * 2 + 3);
                        break;
                }
                break;
            case EquipmentSlots.Ring:
                item.MagicBonus = baseVal;
                item.AttackBonus = rng.Next(0, tier * 2 + 1);
                break;
            case EquipmentSlots.Necklace:
                item.HealthBonus = baseVal;
                item.ProtectionBonus = rng.Next(0, tier * 2 + 1);
                break;
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