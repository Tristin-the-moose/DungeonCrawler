using System;

using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public static class LootFactory
{
    // ── Name tables per slot ──
    private static readonly string[][] HeadNames = {
        new[] { "Cloth Hood", "Leather Cap", "Worn Helmet" },
        new[] { "Iron Helm", "Chain Coif", "Scout Visor" },
        new[] { "Steel Helm", "Mage Crown", "War Helmet" },
        new[] { "Mythril Helm", "Shadow Hood", "Dragon Visor" },
        new[] { "Legendary Crown", "Void Helm", "God Helm" }
    };

    private static readonly string[][] ChestNames = {
        new[] { "Cloth Tunic", "Leather Vest", "Padded Shirt" },
        new[] { "Iron Cuirass", "Chain Mail", "Ranger Coat" },
        new[] { "Steel Plate", "Mage Robe", "War Armor" },
        new[] { "Mythril Plate", "Shadow Cloak", "Dragon Mail" },
        new[] { "Legendary Plate", "Void Armor", "God Armor" }
    };

    private static readonly string[][] LegNames = {
        new[] { "Cloth Pants", "Leather Leggings", "Padded Pants" },
        new[] { "Iron Greaves", "Chain Legs", "Ranger Chaps" },
        new[] { "Steel Greaves", "Mage Skirt", "War Tassets" },
        new[] { "Mythril Greaves", "Shadow Legs", "Dragon Greaves" },
        new[] { "Legendary Greaves", "Void Legs", "God Greaves" }
    };

    private static readonly string[][] BootNames = {
        new[] { "Leather Sandals", "Worn Boots", "Cloth Shoes" },
        new[] { "Iron Boots", "Chain Boots", "Scout Boots" },
        new[] { "Steel Sabatons", "Mage Slippers", "War Boots" },
        new[] { "Mythril Boots", "Shadow Steps", "Dragon Boots" },
        new[] { "Legendary Boots", "Void Treads", "God Boots" }
    };

    private static readonly string[][] WeaponNames = {
        new[] { "Wooden Sword", "Rusty Dagger", "Oak Staff" },
        new[] { "Iron Sword", "Steel Dagger", "Arcane Staff" },
        new[] { "Steel Blade", "Assassin Blade", "Mystic Staff" },
        new[] { "Mythril Sword", "Shadow Blade", "Dragon Staff" },
        new[] { "Legendary Blade", "Void Edge", "God Staff" }
    };

    private static readonly string[][] DefaultNames = {
        new [] {"Worn Down Gear", "Random Scraps", "Wild Plants"},
        new [] {"Worn Down Gear", "Random Scraps", "Wild Plants"},
        new [] {"Worn Down Gear", "Random Scraps", "Wild Plants"},
        new [] {"Worn Down Gear", "Random Scraps", "Wild Plants"},
        new [] {"Worn Down Gear", "Random Scraps", "Wild Plants"}
    };

    /// <summary>
    /// Generate a random equipment drop based on dungeon depth.
    /// </summary>
    public static Equipment GenerateDrop(int depth, Random rng)
    {
        // Pick a random slot
        var slots = Enum.GetValues<EquipmentSlots>();
        var slot = slots[rng.Next(slots.Length)];

        // Determine tier from depth (0-4)
        int tier = Math.Clamp((depth - 1) / 2, 0, 4);

        // Pick a name
        string name = PickName(slot, tier, rng);

        // Generate stat bonuses based on tier and slot
        var item = new Equipment
        {
            Name = name,
            EquipmentType = slot,
            Rarity = tier + 1
        };

        // Base value scales with tier
        int baseVal = 2 + tier * 2 + rng.Next(0, tier + 1);

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
        }

        return item;
    }

    /// <summary>
    /// Generate 3 equipment choices for the player to pick from.
    /// </summary>
    public static Equipment[] GenerateChoices(int depth, Random rng)
    {
        var choices = new Equipment[3];
        for (int i = 0; i < 3; i++)
            choices[i] = GenerateDrop(depth, rng);
        return choices;
    }

    private static string PickName(EquipmentSlots slot, int tier, Random rng)
    {
        string[][] table = slot switch
        {
            EquipmentSlots.HeadPiece   => HeadNames,
            EquipmentSlots.ChestPiece  => ChestNames,
            EquipmentSlots.Leggings   => LegNames,
            EquipmentSlots.Booties  => BootNames,
            EquipmentSlots.Weapon => WeaponNames,
            _ => DefaultNames
        };

        var names = table[Math.Min(tier, table.Length - 1)];
        return names[rng.Next(names.Length)];
    }
}