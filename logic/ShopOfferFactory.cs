// ============================================================
// FILE: logic/ShopOfferFactory.cs — Random shop offer generation
// ============================================================
using System;
using DungeonCrawler;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

/// <summary>Which base stat a shop offer permanently boosts.</summary>
public enum ShopStat
{
    MaxHp,
    Attack,
    Defense,
    Protection,
    Speed,
    Magic,
}

/// <summary>
/// A single thing the shopkeeper has on the counter: a stat upgrade with
/// a flavor name, magnitude, and depth-scaled price.
/// </summary>
public class ShopOffer
{
    public ShopStat Stat       { get; init; }
    public int      Magnitude  { get; init; }
    public int      Price      { get; init; }
    public string   Name       { get; init; } = "Mystery Tonic";

    public string StatLabel => Stat switch
    {
        ShopStat.MaxHp      => "Max HP",
        ShopStat.Attack     => "Attack",
        ShopStat.Defense    => "Defense",
        ShopStat.Protection => "Protection",
        ShopStat.Speed      => "Speed",
        ShopStat.Magic      => "Magic",
        _                   => "???",
    };

    public string EffectLabel => $"+{Magnitude} {StatLabel}";

    /// <summary>
    /// Apply this offer's effect to <paramref name="fighter"/>'s base stats.
    /// MaxHp also bumps current HP by the same amount so the purchase feels
    /// rewarding immediately rather than only after the next heal.
    /// </summary>
    public void ApplyTo(Fighter fighter)
    {
        switch (Stat)
        {
            case ShopStat.MaxHp:
                fighter.Stats.MaxHp += Magnitude;
                fighter.Stats.Hp    += Magnitude;
                break;
            case ShopStat.Attack:     fighter.Stats.Attack     += Magnitude; break;
            case ShopStat.Defense:    fighter.Stats.Defense    += Magnitude; break;
            case ShopStat.Protection: fighter.Stats.Protection += Magnitude; break;
            case ShopStat.Speed:      fighter.Stats.Speed      += Magnitude; break;
            case ShopStat.Magic:      fighter.Stats.Magic      += Magnitude; break;
        }
    }
}

public static class ShopOfferFactory
{
    // Cached so we don't re-allocate the enum array on every reroll
    private static readonly ShopStat[] AllStats = (ShopStat[])Enum.GetValues(typeof(ShopStat));

    // Flavor names per stat — picked at random when an offer of that stat rolls
    private static readonly string[] HpNames         = { "Tonic of Vigor", "Heartroot Draught", "Bloodwine" };
    private static readonly string[] AttackNames     = { "Whetstone", "Fang Oil", "Brawler's Salt" };
    private static readonly string[] DefenseNames    = { "Iron Plates", "Aegis Wax", "Stoneskin Balm" };
    private static readonly string[] ProtectionNames = { "Warding Sigil", "Spirit Charm", "Hexweave Cord" };
    private static readonly string[] SpeedNames      = { "Nimblefoot Tonic", "Windstep Charm", "Quickdraw Salts" };
    private static readonly string[] MagicNames      = { "Arcane Ink", "Wyrm's Breath", "Dust of Pages" };

    /// <summary>
    /// Roll <see cref="GameConfig.ShopUpgradeOfferCount"/> distinct offers for
    /// the current depth. Distinct = no two offers boost the same stat in the
    /// same shop view (until the player runs out of stats and we have to dupe).
    /// </summary>
    public static ShopOffer[] Generate(int depth, Random rng)
    {
        var cfg = GameConfig.Instance;
        int count = Math.Max(1, cfg.ShopUpgradeOfferCount);

        // Shuffled stat pool so each offer covers a different stat where possible.
        // If count > AllStats.Length we fall back to repeats — fine for now.
        Span<ShopStat> pool = stackalloc ShopStat[AllStats.Length];
        for (int i = 0; i < AllStats.Length; i++) pool[i] = AllStats[i];
        for (int i = pool.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int basePrice = (int)MathF.Round(cfg.ShopUpgradeBasePrice
            * MathF.Pow(cfg.ShopUpgradePriceScale, Math.Max(0, depth - 1)));

        var offers = new ShopOffer[count];
        for (int i = 0; i < count; i++)
        {
            ShopStat stat = pool[i % pool.Length];
            int magnitude = MagnitudeFor(stat, cfg);
            // ±10% per-offer price jitter so the three offers don't all read identically
            int price = (int)MathF.Round(basePrice * (0.9f + (float)rng.NextDouble() * 0.2f));
            string[] names = NamePoolFor(stat);

            offers[i] = new ShopOffer
            {
                Stat      = stat,
                Magnitude = magnitude,
                Price     = Math.Max(1, price),
                Name      = names[rng.Next(names.Length)],
            };
        }

        return offers;
    }

    private static int MagnitudeFor(ShopStat stat, GameConfig cfg) => stat switch
    {
        ShopStat.MaxHp      => cfg.ShopUpgradeMaxHp,
        ShopStat.Attack     => cfg.ShopUpgradeAttack,
        ShopStat.Defense    => cfg.ShopUpgradeDefense,
        ShopStat.Protection => cfg.ShopUpgradeProtection,
        ShopStat.Speed      => cfg.ShopUpgradeSpeed,
        ShopStat.Magic      => cfg.ShopUpgradeMagic,
        _                   => 1,
    };

    private static string[] NamePoolFor(ShopStat stat) => stat switch
    {
        ShopStat.MaxHp      => HpNames,
        ShopStat.Attack     => AttackNames,
        ShopStat.Defense    => DefenseNames,
        ShopStat.Protection => ProtectionNames,
        ShopStat.Speed      => SpeedNames,
        ShopStat.Magic      => MagicNames,
        _                   => HpNames,
    };
}
