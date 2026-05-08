// ============================================================
// FILE: logic/LootRarity.cs — Weighted rarity rolls + context boosts
// ============================================================
using System;

namespace DungeonCrawler.logic;

/// <summary>
/// Weighted rarity roll. Higher depths shift the probability toward rarer tiers.
/// Rarity 0 = White (junk), 1 = Green, 2 = Blue, 3 = Purple, 4 = Yellow.
/// (5 is reserved for Cursed and is set by LootFactory after this returns.)
///
/// After the normal weighted roll, the context can enforce a minimum rarity:
///   • Boss     – depth-scaled chance to lock in yellow.
///   • Treasure – depth-scaled chance to floor at purple (still allowed to
///                roll up to yellow on the normal path).
/// </summary>
internal static class LootRarity
{
    public static int Roll(int depth, Random rng, GameConfig cfg, LootContext context)
    {
        // 1. Base depth-driven weighted roll
        int baseRarity = Math.Clamp((depth - 1) / cfg.LootTierDivisor, 0, cfg.LootMaxTier);

        int roll = rng.Next(100);
        int rarity = roll < 50 ? baseRarity
                   : roll < 80 ? baseRarity - 1
                   : roll < 95 ? baseRarity + 1
                   :              baseRarity + 2;

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
}
