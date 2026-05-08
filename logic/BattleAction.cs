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

    /// <summary>
    /// Run the action, emitting each log line through <paramref name="log"/>.
    /// Callers should pass a cached delegate so this stays allocation-free.
    /// </summary>
    public void Execute(Random rng, Action<string> log)
    {
        switch (Type)
        {
            case BattleActionType.Attack: ExecuteAttack(rng, log); break;
            case BattleActionType.Defend: ExecuteDefend(log);      break;
            case BattleActionType.Heal:   ExecuteHeal(log);        break;
        }
    }

    private void ExecuteAttack(Random rng, Action<string> log)
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
            log($"{Source.Stats.Name} {verb} {Target.Stats.Name}!{critTag} (blocked)");

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
            log($"{Target.Stats.Name} {counterVerb}!");
            return;
        }

        // Normal attack
        Target.Stats.TakeDamage(dealt);
        Target.TriggerFlash();

        string tag = isCrit ? " CRITICAL HIT!" : "";
        log($"{Source.Stats.Name} {verb} {Target.Stats.Name}!{tag}");
    }

    private void ExecuteDefend(Action<string> log)
    {
        var cfg = GameConfig.Instance;
        Source.DefendBuff += cfg.DefendBoost;
        Source.IsDefending = true;
        log($"{Source.Stats.Name} takes a defensive stance!");
    }

    private void ExecuteHeal(Action<string> log)
    {
        var cfg = GameConfig.Instance;

        if (!Source.CanHeal)
        {
            log($"{Source.Stats.Name} tries to heal but it's not ready! ({Source.HealCooldown} turns)");
            return;
        }

        // Heal a flat percentage of effective max HP (50% by default).
        // Future heal-power boosts can be layered on top of this base value.
        int heal = (int)(Source.EffectiveMaxHealth * cfg.HealPercent);
        Source.Heal(heal);

        // Start cooldown
        Source.HealCooldown = cfg.HealCooldownTurns;

        log($"{Source.Stats.Name} heals!");
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