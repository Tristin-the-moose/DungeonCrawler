// ============================================================
// FILE: logic/LootFactory.cs — Procedural loot generation
// ============================================================
using System;
using System.Collections.Generic;
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

    public static Equipment GenerateDrop(int depth, Random rng)
    {
        var cfg = GameConfig.Instance;
        var slots = Enum.GetValues<EquipmentSlots>();
        var slot = slots[rng.Next(slots.Length)];
        int tier = Math.Clamp((depth - 1) / cfg.LootTierDivisor, 0, cfg.LootMaxTier);

        var item = new Equipment
        {
            Name = PickName(slot, tier, rng),
            EquipmentType = slot,
            Rarity = tier + 1
        };

        int baseVal = cfg.LootBaseStatValue + tier * cfg.LootStatPerTier + rng.Next(0, tier + 1);
        ApplySlotBonuses(item, slot, baseVal, tier, rng);

        // Weapon names depend on the generated WeaponType, so fix the name after bonuses
        if (slot == EquipmentSlots.Weapon && item.Weapon.HasValue)
            item.Name = PickWeaponName(item.Weapon.Value, tier, rng);

        return item;
    }

    public static Equipment[] GenerateChoices(int depth, Random rng)
    {
        int count = GameConfig.Instance.LootChoiceCount;
        var choices = new Equipment[count];
        for (int i = 0; i < count; i++)
            choices[i] = GenerateDrop(depth, rng);
        return choices;
    }

    private static void ApplySlotBonuses(Equipment item, EquipmentSlots slot,
                                          int baseVal, int tier, Random rng)
    {
        switch (slot)
        {
            case EquipmentSlots.HeadPiece:
                item.HealthBonus = baseVal;
                item.MagicBonus = rng.Next(0, tier + 1);
                break;
            case EquipmentSlots.ChestPiece:
                item.DefenseBonus = baseVal;
                item.HealthBonus = rng.Next(1, tier + 2);
                break;
            case EquipmentSlots.Leggings:
                item.DefenseBonus = (int)(baseVal * 0.7f);
                item.SpeedBoost = rng.Next(0, tier + 1);
                break;
            case EquipmentSlots.Booties:
                item.SpeedBoost = baseVal;
                item.DefenseBonus = rng.Next(0, tier + 1);
                break;
            case EquipmentSlots.Weapon:
                var weaponType = (WeaponType)rng.Next(5);
                item.Weapon = weaponType;
                switch (weaponType)
                {
                    case WeaponType.Sword:  // Pure ATK
                        item.AttackBonus = baseVal;
                        break;
                    case WeaponType.Dagger: // ATK + SPD
                        item.AttackBonus = (int)(baseVal * 0.7f);
                        item.SpeedBoost = rng.Next(1, tier + 3);
                        break;
                    case WeaponType.Staff:  // Pure MAG
                        item.MagicBonus = baseVal;
                        break;
                    case WeaponType.Wand:   // MAG + SPD
                        item.MagicBonus = (int)(baseVal * 0.7f);
                        item.SpeedBoost = rng.Next(1, tier + 3);
                        break;
                    case WeaponType.Mace:   // ATK + DEF
                        item.AttackBonus = (int)(baseVal * 0.7f);
                        item.DefenseBonus = rng.Next(1, tier + 3);
                        break;
                }
                break;
            case EquipmentSlots.Ring:
                item.MagicBonus = baseVal;
                item.AttackBonus = rng.Next(0, tier + 1);
                break;
            case EquipmentSlots.Necklace:
                item.HealthBonus = baseVal;
                item.ProtectionBonus = rng.Next(0, tier + 1);
                break;
        }
    }

    /// <summary>
    /// Pick a name for a weapon based on its WeaponType.
    /// Weapon names are ordered: Sword, Dagger, Staff, Wand, Mace per tier.
    /// </summary>
    public static string PickWeaponName(WeaponType type, int tier, Random rng)
    {
        if (!NameTables.TryGetValue(EquipmentSlots.Weapon, out var table))
            return "Unknown Weapon";

        var names = table[Math.Min(tier, table.Length - 1)];
        int index = (int)type; // enum order matches name order in each tier
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