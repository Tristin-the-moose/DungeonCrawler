// ============================================================
// FILE: logic/BattleAction.cs — Individual battle action logic
// ============================================================
using System;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public enum BattleActionType
{
    Attack,
    Magic,
    Defend,
    Heal
}

public class BattleAction
{
    // ── Tuning constants (were magic numbers) ──
    private const int MinDamage = 1;
    private const int DefendBoost = 3;
    private const int HealBase = 10;
    private const float DamageVariance = 0.15f;  // ±15% damage roll

    public BattleActionType Type { get; set; }
    public Fighter Source { get; set; }
    public Fighter Target { get; set; }

    public string Execute(Random rng)
    {
        return Type switch
        {
            BattleActionType.Attack => ExecuteAttack(rng),
            BattleActionType.Magic  => ExecuteMagic(rng),
            BattleActionType.Defend => ExecuteDefend(),
            BattleActionType.Heal   => ExecuteHeal(),
            _ => ""
        };
    }

    private string ExecuteAttack(Random rng)
    {
        // Always use Effective stats — returns base stats when Equipment is null,
        // so this works for both player and enemies without branching on IsPlayer.
        int atk = Source.EffectiveAttack;
        int def = Target.EffectiveDefense;

        int dealt = CalculateDamage(atk, def, rng);
        Target.Stats.TakeDamage(dealt);
        Target.TriggerFlash();

        return $"{Source.Stats.Name} attacks for {dealt} damage!";
    }

    private string ExecuteMagic(Random rng)
    {
        int atk = Source.EffectiveMagic;
        int def = Target.EffectiveProtection;

        int dealt = CalculateDamage(atk, def, rng);
        Target.Stats.TakeDamage(dealt);
        Target.TriggerFlash();

        return $"{Source.Stats.Name} casts a spell for {dealt} damage!";
    }

    private string ExecuteDefend()
    {
        // BUG FIX: Original permanently increased Stats.Defense by 3 every use.
        // Now applies a temporary buff that BattleSystem resets at turn start.
        Source.DefendBuff += DefendBoost;
        return $"{Source.Stats.Name} braces for impact! (+{DefendBoost} DEF)";
    }

    private string ExecuteHeal()
    {
        int heal = HealBase + Source.Stats.Magic;
        Source.Stats.Heal(heal);  // Uses Stats.Heal() instead of inline Math.Min
        return $"{Source.Stats.Name} heals for {heal} HP!";
    }

    /// <summary>
    /// Shared damage formula with ±15% variance.
    /// Guarantees at least MinDamage so attacks always do something.
    /// </summary>
    private static int CalculateDamage(int atk, int def, Random rng)
    {
        int raw = Math.Max(MinDamage, atk - def);

        // Add some variance so identical matchups don't feel robotic
        float roll = 1f + (float)(rng.NextDouble() * 2 - 1) * DamageVariance;
        return Math.Max(MinDamage, (int)(raw * roll));
    }
}