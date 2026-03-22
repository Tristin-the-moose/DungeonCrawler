// ============================================================
// FILE: logic/LootFactory.cs — Procedural loot generation
// ============================================================
using System;
using System.Collections.Generic;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public static class LootFactory
{
    // ── Name tables: Dictionary replaces 6 separate jagged arrays ──
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
            new[] { "Wooden Sword", "Rusty Dagger", "Oak Staff" },
            new[] { "Iron Sword", "Steel Dagger", "Arcane Staff" },
            new[] { "Steel Blade", "Assassin Blade", "Mystic Staff" },
            new[] { "Mythril Sword", "Shadow Blade", "Dragon Staff" },
            new[] { "Legendary Blade", "Void Edge", "God Staff" }
        },
        // BUG FIX: Ring and Necklace now have proper name tables
        // (original fell through to generic "Worn Down Gear" defaults)
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
        var slots = Enum.GetValues<EquipmentSlots>();
        var slot = slots[rng.Next(slots.Length)];
        int tier = Math.Clamp((depth - 1) / 2, 0, 4);

        var item = new Equipment
        {
            Name = PickName(slot, tier, rng),
            EquipmentType = slot,
            Rarity = tier + 1
        };

        int baseVal = 2 + tier * 2 + rng.Next(0, tier + 1);
        ApplySlotBonuses(item, slot, baseVal, tier, rng);

        return item;
    }

    public static Equipment[] GenerateChoices(int depth, Random rng)
    {
        var choices = new Equipment[3];
        for (int i = 0; i < 3; i++)
            choices[i] = GenerateDrop(depth, rng);
        return choices;
    }

    private static void ApplySlotBonuses(Equipment item, EquipmentSlots slot,
                                          int baseVal, int tier, Random rng)
    {
        // Each slot favors certain stats
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
                item.AttackBonus = baseVal;
                item.MagicBonus = rng.Next(0, tier + 1);
                break;
            // BUG FIX: Ring and Necklace now generate actual stats
            // (original had no case for these — they always got zero bonuses)
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

    private static string PickName(EquipmentSlots slot, int tier, Random rng)
    {
        if (!NameTables.TryGetValue(slot, out var table))
            return "Unknown Gear";

        var names = table[Math.Min(tier, table.Length - 1)];
        return names[rng.Next(names.Length)];
    }
}