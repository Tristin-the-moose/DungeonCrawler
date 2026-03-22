// ============================================================
// FILE: models/Equipment.cs — Equipment item + slot management
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonCrawler.models;

public enum EquipmentSlots
{
    HeadPiece, ChestPiece, Leggings, Booties, Weapon, Ring, Necklace
}

public enum WeaponType
{
    Sword,   // ATK focused
    Dagger,  // ATK + SPD
    Staff,   // MAG focused
    Wand,    // MAG + SPD
    Mace     // ATK + DEF
}


public class Equipment
{
    public string Name { get; set; } = "Unknown";
    public EquipmentSlots EquipmentType { get; set; }
    public int HealthBonus { get; set; }
    public int AttackBonus { get; set; }
    public int MagicBonus { get; set; }
    public int DefenseBonus { get; set; }
    public int ProtectionBonus { get; set; }
    public int SpeedBoost { get; set; }
    public int Rarity { get; set; }
    public WeaponType? Weapon { get; set; }  // Only set for weapon-slot items

    /// <summary>Whether this weapon uses magic for its attack.</summary>
    public bool IsMagicWeapon => Weapon is WeaponType.Staff or WeaponType.Wand;

    /// <summary>Total of all stat bonuses on this item.</summary>
    public int TotalStats => HealthBonus + AttackBonus + MagicBonus
                           + DefenseBonus + ProtectionBonus + SpeedBoost;

    public string StatSummary()
    {
        var parts = new List<string>(6);
        if (HealthBonus > 0)     parts.Add($"+{HealthBonus} HP");
        if (AttackBonus > 0)     parts.Add($"+{AttackBonus} ATK");
        if (DefenseBonus > 0)    parts.Add($"+{DefenseBonus} DEF");
        if (SpeedBoost > 0)      parts.Add($"+{SpeedBoost} SPD");
        if (MagicBonus > 0)      parts.Add($"+{MagicBonus} MAG");
        if (ProtectionBonus > 0) parts.Add($"+{ProtectionBonus} PROT");
        return parts.Count > 0 ? string.Join(", ", parts) : "No bonuses";
    }
}

public class EquipmentSet
{
    // ── Dictionary-backed storage replaces 7 individual properties ──
    // This eliminates the duplicated Get/Equip switch blocks and
    // the 6 copy-pasted TotalBonus properties (42 null-checks → 1 loop).
    private readonly Dictionary<EquipmentSlots, Equipment> _slots = new();

    public EquipmentSet()
    {
        // Default gear
        _slots[EquipmentSlots.HeadPiece]  = new Equipment { EquipmentType = EquipmentSlots.HeadPiece,  Name = "Default Cap" };
        _slots[EquipmentSlots.ChestPiece] = new Equipment { EquipmentType = EquipmentSlots.ChestPiece, Name = "Default Chestpiece" };
        _slots[EquipmentSlots.Leggings]   = new Equipment { EquipmentType = EquipmentSlots.Leggings,   Name = "Default Leggings" };
        _slots[EquipmentSlots.Booties]    = new Equipment { EquipmentType = EquipmentSlots.Booties,     Name = "Default Boots" };
        _slots[EquipmentSlots.Necklace]   = new Equipment { EquipmentType = EquipmentSlots.Necklace,   Name = "Braided Necklace" };
        _slots[EquipmentSlots.Ring]       = new Equipment { EquipmentType = EquipmentSlots.Ring,        Name = "Old Wedding Ring" };
        _slots[EquipmentSlots.Weapon]     = new Equipment { EquipmentType = EquipmentSlots.Weapon,      Name = "Ye Old Dukes", Weapon = WeaponType.Sword };
    }

    public Equipment Get(EquipmentSlots slot) =>
        _slots.TryGetValue(slot, out var item) ? item : null;

    public Equipment Equip(Equipment newItem)
    {
        var old = Get(newItem.EquipmentType);
        _slots[newItem.EquipmentType] = newItem;
        return old;
    }

    /// <summary>All currently equipped items (for iteration).</summary>
    public IEnumerable<Equipment> AllItems => _slots.Values;

    // ── Total bonuses: one LINQ sum replaces 6 copy-pasted properties ──
    private int Sum(Func<Equipment, int> selector) =>
        _slots.Values.Sum(e => e != null ? selector(e) : 0);

    public int TotalBonusHealth     => Sum(e => e.HealthBonus);
    public int TotalBonusAttack     => Sum(e => e.AttackBonus);
    public int TotalBonusMagic      => Sum(e => e.MagicBonus);
    public int TotalBonusDefense    => Sum(e => e.DefenseBonus);
    public int TotalBonusProtection => Sum(e => e.ProtectionBonus);
    public int TotalBonusSpeed      => Sum(e => e.SpeedBoost);

    // ── Save/Load compatibility properties ──
    // These let SaveSystem still access individual slots by name.
    public Equipment HeadPiece  { get => Get(EquipmentSlots.HeadPiece);  set => _slots[EquipmentSlots.HeadPiece] = value; }
    public Equipment ChestPiece { get => Get(EquipmentSlots.ChestPiece); set => _slots[EquipmentSlots.ChestPiece] = value; }
    public Equipment Leggings   { get => Get(EquipmentSlots.Leggings);   set => _slots[EquipmentSlots.Leggings] = value; }
    public Equipment Booties    { get => Get(EquipmentSlots.Booties);    set => _slots[EquipmentSlots.Booties] = value; }
    public Equipment Ring       { get => Get(EquipmentSlots.Ring);       set => _slots[EquipmentSlots.Ring] = value; }
    public Equipment Necklace   { get => Get(EquipmentSlots.Necklace);   set => _slots[EquipmentSlots.Necklace] = value; }
    public Equipment Weapon     { get => Get(EquipmentSlots.Weapon);     set => _slots[EquipmentSlots.Weapon] = value; }
}