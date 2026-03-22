// ============================================================
// FILE: logic/EnemyFactory.cs — Enemy generation by depth
// ============================================================
using System;
using Microsoft.Xna.Framework.Graphics;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public static class EnemyFactory
{
    private static readonly string[] Names =
    {
        "Slime", "Goblin", "Skeleton", "Orc", "Dark Knight",
        "Wraith", "Dragon Whelp", "Demon", "Lich", "Elder Dragon"
    };

    // Base stats for enemy generation — avoids magic numbers scattered in code
    private const int BaseHp = 30;
    private const int BaseAtk = 8;
    private const int BaseDef = 3;
    private const int BaseSpd = 5;
    private const int BaseMag = 4;
    private const float ScalePerDepth = 0.25f;

    public static Fighter Create(int depth, Texture2D[] enemySprites, Random rng)
    {
        int tier = Math.Min(depth, Names.Length - 1);
        float mult = 1f + depth * ScalePerDepth;

        var stats = new Stats
        {
            Name    = Names[tier],
            MaxHp   = (int)(BaseHp  * mult),
            Hp      = (int)(BaseHp  * mult),
            Attack  = (int)(BaseAtk * mult),
            Defense = (int)(BaseDef * mult),
            Speed   = (int)(BaseSpd * mult),
            Magic   = (int)(BaseMag * mult)
        };

        var sprite = enemySprites[tier % enemySprites.Length];

        return new Fighter(stats, isPlayer: false)
        {
            Sprite = sprite,
            Scale = 0.8f
        };
    }
}