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
    {
        var cfg = GameConfig.Instance;
        int tier = Math.Min(depth, Names.Length - 1);

        // Exponential scaling: mult = 1 + multiplier * depth^exponent
        // This curves upward to match the exponential loot progression
        float mult = 1f + cfg.EnemyScaleMultiplier * MathF.Pow(depth, cfg.EnemyScaleExponent);

        // Add slight randomness (±10%) so same-depth enemies feel different
        float variance = 0.9f + (float)rng.NextDouble() * 0.2f;
        mult *= variance;

        var stats = new Stats
        {
            Name    = Names[tier],
            MaxHp   = (int)(cfg.EnemyBaseHp      * mult),
            Hp      = (int)(cfg.EnemyBaseHp      * mult),
            Attack  = (int)(cfg.EnemyBaseAttack  * mult),
            Defense = (int)(cfg.EnemyBaseDefense * mult),
            Speed   = (int)(cfg.EnemyBaseSpeed   * mult),
            Magic   = (int)(cfg.EnemyBaseMagic   * mult)
        };

        var sprite = enemySprites[tier % enemySprites.Length];

        return new Fighter(stats, isPlayer: false)
        {
            Sprite = sprite,
            Scale = 0.8f
        };
    }
}