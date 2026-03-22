// ============================================================
// FILE: logic/BattleSystem.cs — Turn-based battle engine
// ============================================================
using System;
using System.Collections.Generic;
using DungeonCrawler.models;

namespace DungeonCrawler.logic;

public enum BattleTurnState
{
    PlayerChoosing,
    Animating,
    EnemyTurn,
    BattleWon,
    BattleLost
}

public class BattleSystem
{
    // ── Timing constants (were magic numbers) ──
    private const float PreActionDelay = 0.6f;
    private const float BetweenActionDelay = 0.8f;

    public Fighter Player { get; }
    public Fighter Enemy { get; }
    public BattleTurnState State { get; set; }
    public List<string> Log { get; } = new();
    public int Depth { get; }

    private readonly Random _rng;
    private float _animTimer;
    private readonly Queue<BattleAction> _pendingActions = new();

    public BattleSystem(Fighter player, Fighter enemy, int depth, Random rng)
    {
        Player = player;
        Enemy = enemy;
        Depth = depth;
        _rng = rng;   // shared RNG from GameContext — no more new Random() per battle
        State = BattleTurnState.PlayerChoosing;
        Log.Add($"A wild {enemy.Stats.Name} appears! (Depth {depth})");
    }

    /// <summary>Player picks an action from the menu.</summary>
    public void SubmitPlayerAction(BattleActionType type)
    {
        if (State != BattleTurnState.PlayerChoosing) return;

        // Reset temporary defend buffs at the start of each turn
        Player.ResetBuffs();
        Enemy.ResetBuffs();

        var playerTarget = type is BattleActionType.Defend or BattleActionType.Heal
            ? Player : Enemy;

        var playerAction = new BattleAction
        {
            Type = type, Source = Player, Target = playerTarget
        };

        // Enemy auto-picks (70% attack, 30% magic)
        var enemyType = _rng.Next(100) < 70
            ? BattleActionType.Attack
            : BattleActionType.Magic;

        var enemyAction = new BattleAction
        {
            Type = enemyType, Source = Enemy, Target = Player
        };

        // Enqueue in speed order directly — no list/reverse needed
        // Both fighters use EffectiveSpeed so equipment bonuses apply to all
        if (Enemy.EffectiveSpeed > Player.EffectiveSpeed)
        {
            _pendingActions.Enqueue(enemyAction);
            _pendingActions.Enqueue(playerAction);
        }
        else
        {
            _pendingActions.Enqueue(playerAction);
            _pendingActions.Enqueue(enemyAction);
        }

        State = BattleTurnState.Animating;
        _animTimer = PreActionDelay;
    }

    /// <summary>Called every frame during battle.</summary>
    public void Update(float dt)
    {
        Player.Update(dt);
        Enemy.Update(dt);

        if (State != BattleTurnState.Animating) return;

        _animTimer -= dt;
        if (_animTimer > 0f) return;

        // Resolve next action
        if (_pendingActions.Count > 0)
        {
            var action = _pendingActions.Dequeue();

            if (action.Source.Stats.IsAlive)
                Log.Add(action.Execute(_rng));

            _animTimer = BetweenActionDelay;
        }

        // Check end conditions after all actions resolve
        if (_pendingActions.Count == 0 && _animTimer <= 0f)
        {
            if (!Enemy.Stats.IsAlive)
                State = BattleTurnState.BattleWon;
            else if (!Player.Stats.IsAlive)
                State = BattleTurnState.BattleLost;
            else
                State = BattleTurnState.PlayerChoosing;
        }
    }
}