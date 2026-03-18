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
        int actual = Math.Max(0, amount - Defense / 2);
        Hp = Math.Max(0, Hp - actual);
    }
}