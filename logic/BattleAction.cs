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

    public string[] Execute(Random rng)
    {
        return Type switch
        {
            BattleActionType.Attack => ExecuteAttack(rng),
            BattleActionType.Defend => ExecuteDefend(),
            BattleActionType.Heal   => new[] { ExecuteHeal() },
            _ => new[] { "" }
        };
    }

    private string[] ExecuteAttack(Random rng)
    {
        var cfg = GameConfig.Instance;
        int atk, def;
        string verb;

        if (Source.UsesMagicAttack)
        {
            atk = Source.EffectiveMagic;
            def = Target.EffectiveProtection;
            verb = "casts a spell on";
        }
        else
        {
            atk = Source.EffectiveAttack;
            def = Target.EffectiveDefense;
            verb = "attacks";
        }

        var (dealt, isCrit) = CalculateDamage(atk, def, Source, Target, rng);

        // If target is defending, reduce damage and trigger counter-attack
        if (Target.IsDefending)
        {
            int blocked = (int)(dealt * cfg.DefendBlockPercent);
            dealt = Math.Max(cfg.MinDamage, dealt - blocked);

            Target.Stats.TakeDamage(dealt);
            Target.TriggerFlash();

            string critTag = isCrit ? " CRITICAL HIT!" : "";
            string attackMsg = $"{Source.Stats.Name} {verb} {Target.Stats.Name}!{critTag} (blocked)";

            // Counter-attack (uses defender's speed for crit calc).
            // The counter-attacker is `Target`; whether it's magic depends on the
            // counter-attacker's weapon, not the original attacker's.
            int counterAtk = Target.UsesMagicAttack ? Target.EffectiveMagic : Target.EffectiveAttack;
            int counterDef = Target.UsesMagicAttack ? Source.EffectiveProtection : Source.EffectiveDefense;
            int counterRaw = Math.Max(cfg.MinDamage, counterAtk - counterDef);
            int counterDmg = Math.Max(cfg.MinDamage, (int)(counterRaw * cfg.DefendCounterMultiplier));

            Source.Stats.TakeDamage(counterDmg);
            Source.TriggerFlash();

            string counterVerb = Target.UsesMagicAttack ? "retaliates with a spell" : "counter-attacks";
            string counterMsg = $"{Target.Stats.Name} {counterVerb}!";

            return new[] { attackMsg, counterMsg };
        }

        // Normal attack
        Target.Stats.TakeDamage(dealt);
        Target.TriggerFlash();

        string tag = isCrit ? " CRITICAL HIT!" : "";
        return new[] { $"{Source.Stats.Name} {verb} {Target.Stats.Name}!{tag}" };
    }

    private string[] ExecuteDefend()
    {
        var cfg = GameConfig.Instance;
        Source.DefendBuff += cfg.DefendBoost;
        Source.IsDefending = true;
        return new[] { $"{Source.Stats.Name} takes a defensive stance!" };
    }

    private string ExecuteHeal()
    {
        var cfg = GameConfig.Instance;

        if (!Source.CanHeal)
            return $"{Source.Stats.Name} tries to heal but it's not ready! ({Source.HealCooldown} turns)";

        // Heal a flat percentage of effective max HP (50% by default).
        // Future heal-power boosts can be layered on top of this base value.
        int heal = (int)(Source.EffectiveMaxHealth * cfg.HealPercent);
        Source.Heal(heal);

        // Start cooldown
        Source.HealCooldown = cfg.HealCooldownTurns;

        return $"{Source.Stats.Name} heals!";
    }

    /// <summary>
    /// Damage formula with variance and speed-based crit chance.
    /// Crit% = BaseCrit + SpeedCritBonus * max(0, attackerSpeed - defenderSpeed)
    /// </summary>
    private static (int damage, bool isCrit) CalculateDamage(
        int atk, int def, Fighter source, Fighter target, Random rng)
    {
        var cfg = GameConfig.Instance;
        int raw = Math.Max(cfg.MinDamage, atk - def);

        // Variance roll
        float roll = 1f + (float)(rng.NextDouble() * 2 - 1) * cfg.DamageVariance;
        int damage = Math.Max(cfg.MinDamage, (int)(raw * roll));

        // Crit chance scales with speed advantage
        int speedDiff = Math.Max(0, source.EffectiveSpeed - target.EffectiveSpeed);
        float critChance = cfg.CritChance + speedDiff * cfg.SpeedCritBonus;
        bool isCrit = rng.Next(100) < (int)critChance;

        if (isCrit)
            damage = (int)(damage * cfg.CritMultiplier);

        return (damage, isCrit);
    }
}