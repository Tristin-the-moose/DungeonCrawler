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
        float mult = 1f + depth * cfg.EnemyScalePerDepth;

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