// ============================================================
// FILE: logic/FighterFactory.cs — Player creation & save loading
// ============================================================
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DungeonCrawler.models;
using DungeonCrawler.utils;

namespace DungeonCrawler.logic;

/// <summary>
/// Centralizes player creation logic.
/// Replaces the duplicated InitPlayer() / LoadSavedGame() methods from original Game1.
/// </summary>
public static class FighterFactory
{
    public static Fighter CreatePlayer(Texture2D sprite, string name = "Hero")
    {
        var stats = new Stats
        {
            Name = name,
            MaxHp = 100, Hp = 100,
            Attack = 12, Defense = 5,
            Speed = 7, Magic = 8
        };

        return new Fighter(stats, isPlayer: true)
        {
            Sprite = sprite,
            Scale = 2.0f,
            FlipEffect = SpriteEffects.None
        };
    }

    public static Fighter FromSave(SaveData save, Texture2D sprite)
    {
        var stats = new Stats
        {
            Name = save.PlayerName,
            MaxHp = save.MaxHp, Hp = save.Hp,
            Attack = save.Attack, Defense = save.Defense,
            Speed = save.Speed, Magic = save.Magic,
        };

        var gear = new EquipmentSet
        {
            HeadPiece = save.HeadPiece,
            ChestPiece = save.ChestPiece,
            Leggings = save.Leggings,
            Booties = save.Booties,
            Ring = save.Ring,
            Necklace = save.Necklace,
            Weapon = save.Weapon
        };

        return new Fighter(stats, isPlayer: true)
        {
            Sprite = sprite,
            Scale = 2.0f,
            FlipEffect = SpriteEffects.None,
            Equipment = gear
        };
    }
}