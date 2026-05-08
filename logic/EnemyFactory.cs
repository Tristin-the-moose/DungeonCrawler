// ============================================================
// FILE: logic/EnemyFactory.cs — Enemy generation by depth
// ============================================================
using System;
using Microsoft.Xna.Framework.Graphics;
using DungeonCrawler;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public static class EnemyFactory
{
    private static readonly string[] Names =
    {
        "Slime", "Goblin", "Skeleton", "Orc", "Dark Knight",
        "Wraith", "Dragon Whelp", "Demon", "Lich", "Elder Dragon"
    };

    public static Fighter Create(int depth, Texture2D[] enemySprites, Random rng)
        => CreateWithMultipliers(depth, enemySprites, rng, hpMult: 1f, atkMult: 1f, namePrefix: "");

    /// <summary>Elite enemy — significantly stronger than a standard foe.</summary>
    public static Fighter CreateElite(int depth, Texture2D[] enemySprites, Random rng)
    {
        var cfg = GameConfig.Instance;
        return CreateWithMultipliers(depth, enemySprites, rng,
            hpMult:     cfg.EliteHpMultiplier,
            atkMult:    cfg.EliteAttackMultiplier,
            namePrefix: "Elite ");
    }

    /// <summary>Boss enemy — very tough, appears every 5 floors.</summary>
    public static Fighter CreateBoss(int depth, Texture2D[] enemySprites, Random rng)
    {
        var cfg = GameConfig.Instance;
        return CreateWithMultipliers(depth, enemySprites, rng,
            hpMult:     cfg.BossHpMultiplier,
            atkMult:    cfg.BossAttackMultiplier,
            namePrefix: "Boss ");
    }

    private static Fighter CreateWithMultipliers(
        int depth, Texture2D[] enemySprites, Random rng,
        float hpMult, float atkMult, string namePrefix)
    {
        var cfg = GameConfig.Instance;
        // Depth is 1-based, but tier is 0-based — floor 1 = Slime (tier 0).
        int tier = Math.Min(Math.Max(0, depth - 1), Names.Length - 1);

        float mult = 1f + cfg.EnemyScaleMultiplier * MathF.Pow(depth, cfg.EnemyScaleExponent);
        float variance = 0.9f + (float)rng.NextDouble() * 0.2f;
        mult *= variance;

        var stats = new Stats
        {
            Name       = namePrefix + Names[tier],
            MaxHp      = (int)(cfg.EnemyBaseHp         * mult * hpMult),
            Hp         = (int)(cfg.EnemyBaseHp         * mult * hpMult),
            Attack     = (int)(cfg.EnemyBaseAttack     * mult * atkMult),
            Defense    = (int)(cfg.EnemyBaseDefense    * mult),
            Protection = (int)(cfg.EnemyBaseProtection * mult),
            Speed      = (int)(cfg.EnemyBaseSpeed      * mult),
            Magic      = (int)(cfg.EnemyBaseMagic      * mult * atkMult)
        };

        var sprite = enemySprites[tier % enemySprites.Length];

        return new Fighter(stats, isPlayer: false)
        {
            Sprite = sprite,
            Scale  = 0.8f
        };
    }
}