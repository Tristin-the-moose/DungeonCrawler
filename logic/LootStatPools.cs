// ============================================================
// FILE: logic/LootStatPools.cs — Per-slot / per-weapon stat pools
// ============================================================
using System.Collections.Generic;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

/// <summary>
/// Which stat a pool entry adds to. The first entry in any pool is the
/// slot's identity stat (rolled first, full base value); subsequent
/// entries decay so the primary stat stays the highlight.
/// </summary>
internal enum StatKey { Health, Attack, Magic, Defense, Protection, Speed }

/// <summary>
/// Stat-pool tables consumed by LootFactory.ApplyBonuses.
/// Yellow items (rarity 4) roll the first 4 entries.
/// </summary>
internal static class LootStatPools
{
    public static readonly Dictionary<EquipmentSlots, StatKey[]> Slots = new()
    {
        [EquipmentSlots.HeadPiece]  = new[] { StatKey.Health,  StatKey.Defense, StatKey.Magic,      StatKey.Protection },
        [EquipmentSlots.ChestPiece] = new[] { StatKey.Defense, StatKey.Health,  StatKey.Protection, StatKey.Magic      },
        [EquipmentSlots.Leggings]   = new[] { StatKey.Defense, StatKey.Speed,   StatKey.Health,     StatKey.Protection },
        [EquipmentSlots.Booties]    = new[] { StatKey.Speed,   StatKey.Defense, StatKey.Health,     StatKey.Protection },
        [EquipmentSlots.Ring]       = new[] { StatKey.Magic,   StatKey.Attack,  StatKey.Defense,    StatKey.Speed      },
        [EquipmentSlots.Necklace]   = new[] { StatKey.Health,  StatKey.Magic,   StatKey.Protection, StatKey.Defense    },
    };

    public static readonly Dictionary<WeaponType, StatKey[]> Weapons = new()
    {
        [WeaponType.Sword]  = new[] { StatKey.Attack, StatKey.Defense, StatKey.Speed,   StatKey.Health     },
        [WeaponType.Dagger] = new[] { StatKey.Attack, StatKey.Speed,   StatKey.Defense, StatKey.Health     },
        [WeaponType.Staff]  = new[] { StatKey.Magic,  StatKey.Health,  StatKey.Speed,   StatKey.Protection },
        [WeaponType.Wand]   = new[] { StatKey.Magic,  StatKey.Speed,   StatKey.Health,  StatKey.Protection },
        [WeaponType.Mace]   = new[] { StatKey.Attack, StatKey.Defense, StatKey.Health,  StatKey.Speed      },
    };
}
