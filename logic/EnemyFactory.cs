using Microsoft.Xna.Framework.Graphics;
using System;

using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public static class EnemyFactory
{
    private static readonly string[] Names =
        { "Slime", "Goblin", "Skeleton", "Orc", "Dark Knight",
          "Wraith", "Dragon Whelp", "Demon", "Lich", "Elder Dragon" };

    public static Fighter Create(int depth, Texture2D[] enemySprites, Random rng)
    {
        // Stats scale with depth
        int tier = Math.Min(depth, Names.Length - 1);
        float mult = 1f + depth * 0.25f;

        var stats = new Stats
        {
            Name    = Names[tier],
            MaxHp   = (int)(30 * mult),
            Hp      = (int)(30 * mult),
            Attack  = (int)(8 * mult),
            Defense = (int)(3 * mult),
            Speed   = (int)(5 * mult),
            Magic   = (int)(4 * mult)
        };

        // Pick a sprite (cycle through available textures)
        var sprite = enemySprites[tier % enemySprites.Length];

        return new Fighter(stats, isPlayer: false)
        {
            Sprite = sprite,
            Scale = 0.8f   // background enemy is slightly smaller
        };
    }
}