// ============================================================
// FILE: logic/BattleAction.cs — Individual battle action logic
// ============================================================
using System;
using DungeonCrawler;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public enum BattleActionType
{
    Attack,
    Defend,
    Heal
}

public class BattleAction
{
    public BattleActionType Type { get; set; }
    public Fighter Source { get; set; }
    public Fighter Target { get; set; }

    public string Execute(Random rng)
    {
        return Type switch
        {
            BattleActionType.Attack => ExecuteAttack(rng),
            BattleActionType.Defend => ExecuteDefend(),
            BattleActionType.Heal   => ExecuteHeal(),
            _ => ""
        };
    }

    private string ExecuteAttack(Random rng)
    {
        int atk, def;
        string verb;

        // Weapon type determines whether attack is physical or magical
        if (Source.UsesMagicAttack)
        {
            atk = Source.EffectiveMagic;
            def = Target.EffectiveProtection;
            verb = "casts a spell for";
        }
        else
        {
            atk = Source.EffectiveAttack;
            def = Target.EffectiveDefense;
            verb = "attacks for";
        }

        var (dealt, isCrit) = CalculateDamage(atk, def, rng);
        Target.Stats.TakeDamage(dealt);
        Target.TriggerFlash();

        string critTag = isCrit ? " CRITICAL HIT!" : "";
        return $"{Source.Stats.Name} {verb} {dealt} damage!{critTag}";
    }

    private string ExecuteDefend()
    {
        var cfg = GameConfig.Instance;
        Source.DefendBuff += cfg.DefendBoost;
        return $"{Source.Stats.Name} braces for impact! (+{cfg.DefendBoost} DEF)";
    }

    private string ExecuteHeal()
    {
        var cfg = GameConfig.Instance;
        int heal = cfg.HealBase + Source.Stats.Magic;
        Source.Stats.Heal(heal);
        return $"{Source.Stats.Name} heals for {heal} HP!";
    }

    private static (int damage, bool isCrit) CalculateDamage(int atk, int def, Random rng)
    {
        var cfg = GameConfig.Instance;
        int raw = Math.Max(cfg.MinDamage, atk - def);

        float roll = 1f + (float)(rng.NextDouble() * 2 - 1) * cfg.DamageVariance;
        int damage = Math.Max(cfg.MinDamage, (int)(raw * roll));

        bool isCrit = rng.Next(100) < cfg.CritChance;
        if (isCrit)
            damage = (int)(damage * cfg.CritMultiplier);

        return (damage, isCrit);
    }
}