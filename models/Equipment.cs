// ============================================================
// FILE: models/Equipment.cs — Equipment item + slot management
// ============================================================
using System.Collections.Generic;

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

    // Cached on first read — Equipment is immutable after LootFactory builds it,
    // so the summary string can never change. If a future feature mutates an
    // already-built item in place (enchants, buffs), clear this field there.
    private string _summary;

    public string StatSummary() => _summary ??= BuildSummary();

    private string BuildSummary()
    {
        var parts = new List<string>(6);
        if (HealthBonus != 0)     parts.Add($"{HealthBonus:+#;-#;0} HP");
        if (AttackBonus != 0)     parts.Add($"{AttackBonus:+#;-#;0} ATK");
        if (DefenseBonus != 0)    parts.Add($"{DefenseBonus:+#;-#;0} DEF");
        if (SpeedBoost != 0)      parts.Add($"{SpeedBoost:+#;-#;0} SPD");
        if (MagicBonus != 0)      parts.Add($"{MagicBonus:+#;-#;0} MAG");
        if (ProtectionBonus != 0) parts.Add($"{ProtectionBonus:+#;-#;0} PROT");
        return parts.Count > 0 ? string.Join(", ", parts) : "No bonuses";
    }
}

public class EquipmentSet
{
    // ── Dictionary-backed storage replaces 7 individual properties ──
    private readonly Dictionary<EquipmentSlots, Equipment> _slots = new();

    // ── Cached totals ──
    // Recomputed whenever a slot changes (Equip / save-compat setters).
    // Reads are tight loops in Fighter.Effective* and the battle/map screens,
    // so caching avoids the LINQ Sum + delegate alloc that used to fire there.
    //
    // Note: this assumes Equipment instances aren't mutated in place after
    // they're equipped. LootFactory builds items and they're effectively
    // immutable thereafter — if that ever changes, call RecomputeTotals()
    // after the mutation.
    private int _totalHealth;
    private int _totalAttack;
    private int _totalMagic;
    private int _totalDefense;
    private int _totalProtection;
    private int _totalSpeed;

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
        RecomputeTotals();
    }

    public Equipment Get(EquipmentSlots slot) =>
        _slots.TryGetValue(slot, out var item) ? item : null;

    public Equipment Equip(Equipment newItem)
    {
        var old = Get(newItem.EquipmentType);
        _slots[newItem.EquipmentType] = newItem;
        RecomputeTotals();
        return old;
    }

    /// <summary>All currently equipped items (for iteration).</summary>
    public IEnumerable<Equipment> AllItems => _slots.Values;

    // ── Total bonuses: cached field reads (recomputed on slot change) ──
    public int TotalBonusHealth     => _totalHealth;
    public int TotalBonusAttack     => _totalAttack;
    public int TotalBonusMagic      => _totalMagic;
    public int TotalBonusDefense    => _totalDefense;
    public int TotalBonusProtection => _totalProtection;
    public int TotalBonusSpeed      => _totalSpeed;

    // ── Save/Load compatibility properties ──
    // These let SaveSystem still access individual slots by name. Setters
    // route through SetSlot so the cached totals stay in sync.
    public Equipment HeadPiece  { get => Get(EquipmentSlots.HeadPiece);  set => SetSlot(EquipmentSlots.HeadPiece,  value); }
    public Equipment ChestPiece { get => Get(EquipmentSlots.ChestPiece); set => SetSlot(EquipmentSlots.ChestPiece, value); }
    public Equipment Leggings   { get => Get(EquipmentSlots.Leggings);   set => SetSlot(EquipmentSlots.Leggings,   value); }
    public Equipment Booties    { get => Get(EquipmentSlots.Booties);    set => SetSlot(EquipmentSlots.Booties,    value); }
    public Equipment Ring       { get => Get(EquipmentSlots.Ring);       set => SetSlot(EquipmentSlots.Ring,       value); }
    public Equipment Necklace   { get => Get(EquipmentSlots.Necklace);   set => SetSlot(EquipmentSlots.Necklace,   value); }
    public Equipment Weapon     { get => Get(EquipmentSlots.Weapon);     set => SetSlot(EquipmentSlots.Weapon,     value); }

    private void SetSlot(EquipmentSlots slot, Equipment value)
    {
        _slots[slot] = value;
        RecomputeTotals();
    }

    /// <summary>
    /// Walk the 7 slots once and refresh every cached total. Called from
    /// the constructor, Equip(), and the save-compat setters.
    /// foreach over Dictionary.Values uses the struct enumerator — no
    /// boxing, no delegate allocation.
    /// </summary>
    private void RecomputeTotals()
    {
        int h = 0, a = 0, m = 0, d = 0, p = 0, s = 0;
        foreach (var e in _slots.Values)
        {
            if (e == null) continue;
            h += e.HealthBonus;
            a += e.AttackBonus;
            m += e.MagicBonus;
            d += e.DefenseBonus;
            p += e.ProtectionBonus;
            s += e.SpeedBoost;
        }
        _totalHealth = h;
        _totalAttack = a;
        _totalMagic = m;
        _totalDefense = d;
        _totalProtection = p;
        _totalSpeed = s;
    }
}