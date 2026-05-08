// ============================================================
// FILE: models/Stats.cs — Base stat block for any combatant
// ============================================================
using System;

namespace DungeonCrawler.models;

public class Stats
{
    public string Name { get; set; } = "Unknown";
    public int MaxHp { get; set; }
    public int Hp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Magic { get; set; }

    public bool IsAlive => Hp > 0;
    public float HpPercent => MaxHp > 0 ? (float)Hp / MaxHp : 0f;

    public void TakeDamage(int amount)
    {
        Hp = Math.Max(0, Hp - amount);
    }

    // Heal lives on Fighter — it needs effective max HP (base + gear),
    // which Stats can't see. See Fighter.Heal.
}