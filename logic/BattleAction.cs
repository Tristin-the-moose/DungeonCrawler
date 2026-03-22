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
    /// Execute the action. Returns one or more log messages.
    /// </summary>
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
            verb = "casts a spell for";
        }
        else
        {
            atk = Source.EffectiveAttack;
            def = Target.EffectiveDefense;
            verb = "attacks for";
        }

        var (dealt, isCrit) = CalculateDamage(atk, def, rng);

        // If target is defending, reduce damage and trigger counter-attack
        if (Target.IsDefending)
        {
            int blocked = (int)(dealt * cfg.DefendBlockPercent);
            dealt = Math.Max(cfg.MinDamage, dealt - blocked);

            Target.Stats.TakeDamage(dealt);
            Target.TriggerFlash();

            string critTag = isCrit ? " CRITICAL HIT!" : "";
            string attackMsg = $"{Source.Stats.Name} {verb} {Target.Stats.Name}!{critTag} (blocked)";

            // Counter-attack
            int counterAtk = Target.UsesMagicAttack ? Target.EffectiveMagic : Target.EffectiveAttack;
            int counterDef = Source.UsesMagicAttack ? Source.EffectiveProtection : Source.EffectiveDefense;
            int counterRaw = Math.Max(cfg.MinDamage, counterAtk - counterDef);
            int counterDmg = Math.Max(cfg.MinDamage, (int)(counterRaw * cfg.DefendCounterMultiplier));

            Source.Stats.TakeDamage(counterDmg);
            Source.TriggerFlash();

            string counterVerb = Target.UsesMagicAttack ? "retaliates with a spell" : "counter-attacks";
            string counterMsg = $"{Target.Stats.Name} {counterVerb}!";

            return new[] { attackMsg, counterMsg };
        }

        // Normal attack (no defend active)
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
        int heal = cfg.HealBase + Source.Stats.Magic;
        Source.Stats.Heal(heal);
        return $"{Source.Stats.Name} heals!";
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