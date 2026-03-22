// ============================================================
// FILE: logic/BattleSystem.cs — Turn-based battle engine
// ============================================================
using System;
using System.Collections.Generic;
using DungeonCrawler;
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
        _rng = rng;
        State = BattleTurnState.PlayerChoosing;
        Log.Add($"A wild {enemy.Stats.Name} appears! (Depth {depth})");
    }

    public void SubmitPlayerAction(BattleActionType type)
    {
        if (State != BattleTurnState.PlayerChoosing) return;

        var cfg = GameConfig.Instance;

        // Reset temporary defend buffs at the start of each turn
        Player.ResetBuffs();
        Enemy.ResetBuffs();

        var playerTarget = type is BattleActionType.Defend or BattleActionType.Heal
            ? Player : Enemy;

        var playerAction = new BattleAction
        {
            Type = type, Source = Player, Target = playerTarget
        };

        // Enemy always attacks (weapon type determines physical vs magic)
        var enemyAction = new BattleAction
        {
            Type = BattleActionType.Attack, Source = Enemy, Target = Player
        };

        // Enqueue in speed order directly
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
        _animTimer = cfg.PreActionDelay;
    }

    public void Update(float dt)
    {
        Player.Update(dt);
        Enemy.Update(dt);

        if (State != BattleTurnState.Animating) return;

        _animTimer -= dt;
        if (_animTimer > 0f) return;

        if (_pendingActions.Count > 0)
        {
            var action = _pendingActions.Dequeue();

            if (action.Source.Stats.IsAlive)
            {
                string[] messages = action.Execute(_rng);
                foreach (var msg in messages)
                    Log.Add(msg);
            }

            _animTimer = GameConfig.Instance.BetweenActionDelay;
        }

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