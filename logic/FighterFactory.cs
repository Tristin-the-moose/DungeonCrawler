// ============================================================
// FILE: logic/FighterFactory.cs — Player creation & save loading
// ============================================================
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DungeonCrawler;
using DungeonCrawler.models;
using DungeonCrawler.utils;

namespace DungeonCrawler.logic;

public static class FighterFactory
{
    public static Fighter CreatePlayer(Texture2D sprite, string name = null)
    {
        var cfg = GameConfig.Instance;
        var stats = new Stats
        {
            Name       = name ?? cfg.DefaultPlayerName,
            MaxHp      = cfg.StartingMaxHp,
            Hp         = cfg.StartingMaxHp,
            Attack     = cfg.StartingAttack,
            Defense    = cfg.StartingDefense,
            Protection = cfg.StartingProtection,
            Speed      = cfg.StartingSpeed,
            Magic      = cfg.StartingMagic
        };

        var weaponType = cfg.StartWithMagicWeapon ? WeaponType.Staff : WeaponType.Sword;
        var startingWeapon = new Equipment
        {
            EquipmentType = EquipmentSlots.Weapon,
            Name = cfg.StartWithMagicWeapon ? "Old Staff" : "Ye Old Dukes",
            Weapon = weaponType
        };

        var player = new Fighter(stats, isPlayer: true)
        {
            Sprite = sprite,
            Scale = 2.0f,
            FlipEffect = SpriteEffects.None
        };

        player.Equipment.Equip(startingWeapon);
        return player;
    }

    public static Fighter FromSave(SaveData save, Texture2D sprite)
    {
        var stats = new Stats
        {
            Name       = save.PlayerName,
            MaxHp      = save.MaxHp,
            Hp         = save.Hp,
            Attack     = save.Attack,
            Defense    = save.Defense,
            Protection = save.Protection,
            Speed      = save.Speed,
            Magic      = save.Magic,
        };

        var gear = new EquipmentSet
        {
            HeadPiece  = save.HeadPiece,
            ChestPiece = save.ChestPiece,
            Leggings   = save.Leggings,
            Booties    = save.Booties,
            Ring       = save.Ring,
            Necklace   = save.Necklace,
            Weapon     = save.Weapon
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