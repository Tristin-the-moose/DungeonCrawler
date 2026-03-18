using System;
using System.Linq;
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
    public Fighter Player { get; }
    public Fighter Enemy { get; }
    public BattleTurnState State { get; set; }
    public List<string> Log { get; } = new();
    public int Depth { get; }

    private Random _rng = new();
    private float _animTimer;
    private Queue<BattleAction> _pendingActions = new();

    public BattleSystem(Fighter player, Fighter enemy, int depth)
    {
        Player = player;
        Enemy = enemy;
        Depth = depth;
        State = BattleTurnState.PlayerChoosing;
        Log.Add($"A wild {enemy.Stats.Name} appears! (Depth {depth})");
    }

    /// <summary>Player picks an action from the menu.</summary>
    public void SubmitPlayerAction(BattleActionType type)
    {
        if (State != BattleTurnState.PlayerChoosing) return;

        var target = type is BattleActionType.Defend or BattleActionType.Heal
            ? Player : Enemy;

        _pendingActions.Enqueue(new BattleAction
        {
            Type = type, Source = Player, Target = target
        });

        // Enemy auto-picks
        var enemyAction = _rng.Next(100) < 70
            ? BattleActionType.Attack
            : BattleActionType.Magic;

        _pendingActions.Enqueue(new BattleAction
        {
            Type = enemyAction, Source = Enemy, Target = Player
        });

        // Determine turn order by speed
        if (Enemy.Stats.Speed > Player.Stats.Speed)
        {
            var list = _pendingActions.ToList();
            list.Reverse();
            _pendingActions = new Queue<BattleAction>(list);
        }

        State = BattleTurnState.Animating;
        _animTimer = 0.6f; // pause before first action resolves
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

            // Skip if source is dead
            if (action.Source.Stats.IsAlive)
            {
                string msg = action.Execute(_rng);
                Log.Add(msg);
            }

            _animTimer = 0.8f; // pause between actions
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