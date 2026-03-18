using System;
using System.Collections.Generic;

namespace DungeonCrawler.models;

public enum EquipmentSlots {
    HeadPiece, ChestPiece, Leggings, Booties, Weapon, Ring, Necklace
}

public class Equipment {
    public string Name { get; set; } = "Unknown";

    public EquipmentSlots EquipmentType { get; set; }

    public int HealthBonus { get; set; } = 0;

    public int AttackBonus { get; set; } = 0;
    public int MagicBonus { get; set; } = 0;

    public int DefenseBonus { get; set; } = 0;
    public int ProtectionBonus { get; set; } = 0;

    public int SpeedBoost { get; set; } = 0;

    public int Rarity { get; set; } = 0;

    public string StatSummary()
    {
        var parts = new List<string>();
        if (HealthBonus > 0)  parts.Add($"+{HealthBonus} HP");
        if (AttackBonus > 0)  parts.Add($"+{AttackBonus} ATK");
        if (DefenseBonus > 0) parts.Add($"+{DefenseBonus} DEF");
        if (SpeedBoost > 0)   parts.Add($"+{SpeedBoost} SPD");
        if (MagicBonus > 0)   parts.Add($"+{MagicBonus} MAG");
        return parts.Count > 0 ? string.Join(", ", parts) : "No bonuses";
    }
}

public class EquipmentSet {
    public Equipment HeadPiece { get; set; }
    public Equipment ChestPiece { get; set; }
    public Equipment Leggings { get; set; }
    public Equipment Booties { get; set; }

    public Equipment Ring { get; set; }

    public Equipment Necklace { get; set; }

    public Equipment Weapon { get; set; }

    public EquipmentSet() {
        HeadPiece = new Equipment { EquipmentType = EquipmentSlots.HeadPiece, Name = "Default Cap" };
        ChestPiece = new Equipment { EquipmentType = EquipmentSlots.ChestPiece, Name = "Default Chestpiece" };
        Leggings = new Equipment { EquipmentType = EquipmentSlots.Leggings, Name = "Default Leggings" };
        Booties = new Equipment { EquipmentType = EquipmentSlots.Booties, Name = "Default Boots" };
        Necklace = new Equipment { EquipmentType = EquipmentSlots.Necklace, Name = "Braided Necklace"};
        Ring = new Equipment { EquipmentType = EquipmentSlots.Ring, Name = "Old Wedding Ring" };
        Weapon = new Equipment { EquipmentType = EquipmentSlots.Weapon, Name = "Ye Old Dukes"};
    }

    public Equipment Get (EquipmentSlots slot) => slot switch {
        EquipmentSlots.HeadPiece => HeadPiece,  
        EquipmentSlots.ChestPiece => ChestPiece,
        EquipmentSlots.Leggings => Leggings,
        EquipmentSlots.Booties => Booties,

        EquipmentSlots.Ring => Ring,

        EquipmentSlots.Necklace => Necklace,
        EquipmentSlots.Weapon => Weapon,
        _ => null
    };

    public Equipment Equip(Equipment newItem) {
        Equipment oldItem = Get(newItem.EquipmentType);

        switch (oldItem.EquipmentType) {
            case EquipmentSlots.HeadPiece: HeadPiece = newItem; break; 
            case EquipmentSlots.ChestPiece: ChestPiece = newItem; break; 
            case EquipmentSlots.Leggings: Leggings = newItem; break; 
            case EquipmentSlots.Booties: Booties = newItem; break;

            case EquipmentSlots.Ring: Ring = newItem; break;  

            case EquipmentSlots.Necklace: Necklace = newItem; break;
            case EquipmentSlots.Weapon: Weapon = newItem; break;
        }

        return oldItem;
    }

    public int TotalBonusHealth => 
        (HeadPiece?.HealthBonus ?? 0) + (ChestPiece?.HealthBonus ?? 0) + (Leggings?.HealthBonus ?? 0) + (Booties?.HealthBonus ?? 0) +
        (Ring?.HealthBonus ?? 0) + (Necklace?.HealthBonus ?? 0) + (Weapon?.HealthBonus ?? 0);

    public int TotalBonusAttack => 
        (HeadPiece?.AttackBonus ?? 0) + (ChestPiece?.AttackBonus ?? 0) + (Leggings?.AttackBonus ?? 0) + (Booties?.AttackBonus ?? 0) +
        (Ring?.AttackBonus ?? 0) + (Necklace?.AttackBonus ?? 0) + (Weapon?.AttackBonus ?? 0);

    public int TotalBonusMagic => 
        (HeadPiece?.MagicBonus ?? 0) + (ChestPiece?.MagicBonus ?? 0) + (Leggings?.MagicBonus ?? 0) + (Booties?.MagicBonus ?? 0) +
        (Ring?.MagicBonus ?? 0) + (Necklace?.MagicBonus ?? 0) + (Weapon?.MagicBonus ?? 0);

    public int TotalBonusDefense => 
        (HeadPiece?.DefenseBonus ?? 0) + (ChestPiece?.DefenseBonus ?? 0) + (Leggings?.DefenseBonus ?? 0) + (Booties?.DefenseBonus ?? 0) +
        (Ring?.DefenseBonus ?? 0) + (Necklace?.DefenseBonus ?? 0) + (Weapon?.DefenseBonus ?? 0);

    public int TotalBonusProtection => 
        (HeadPiece?.ProtectionBonus ?? 0) + (ChestPiece?.ProtectionBonus ?? 0) + (Leggings?.ProtectionBonus ?? 0) + (Booties?.ProtectionBonus ?? 0) +
        (Ring?.ProtectionBonus ?? 0) + (Necklace?.ProtectionBonus ?? 0) + (Weapon?.ProtectionBonus ?? 0);

    public int TotalBonusSpeed => 
        (HeadPiece?.SpeedBoost ?? 0) + (ChestPiece?.SpeedBoost ?? 0) + (Leggings?.SpeedBoost ?? 0) + (Booties?.SpeedBoost ?? 0) +
        (Ring?.SpeedBoost ?? 0) + (Necklace?.SpeedBoost ?? 0) + (Weapon?.SpeedBoost ?? 0);
}