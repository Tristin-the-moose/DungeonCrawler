// ============================================================
// FILE: logic/LootNameTables.cs — Per-slot, per-tier loot name tables
// ============================================================
using System.Collections.Generic;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

/// <summary>
/// Names for generated loot, indexed [slot][tier][nameIndex].
/// Tier 0 = white (junk), 4 = yellow (legendary).
/// Pulled out of LootFactory so the data is easy to scan and edit.
/// </summary>
internal static class LootNameTables
{
    public static readonly Dictionary<EquipmentSlots, string[][]> Names = new()
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
}
