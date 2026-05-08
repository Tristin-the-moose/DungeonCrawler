// ============================================================
// FILE: models/Room.cs — Map room data model
// ============================================================
using Microsoft.Xna.Framework;

namespace DungeonCrawler.models;

public enum RoomType { Entrance, Exit, Battle, Elite, Boss, Treasure, Rest }

public enum RoomState
{
    Hidden,      // Not yet adjacent to player — not shown on map
    Accessible,  // Adjacent to player's current room, not yet entered
    Current,     // Player is here right now
    Visited      // Player has been here before
}

public class Room
{
    public int       X     { get; }
    public int       Y     { get; }
    public RoomType  Type  { get; set; }
    public RoomState State { get; set; } = RoomState.Hidden;

    public bool IsVisible   => State != RoomState.Hidden;
    public bool IsCompleted => State == RoomState.Visited;

    public Room(int x, int y, RoomType type)
    {
        X    = x;
        Y    = y;
        Type = type;
    }

    /// <summary>Short label drawn inside the map box.</summary>
    public string Label => Type switch
    {
        RoomType.Entrance => "START",
        RoomType.Exit     => "EXIT",
        RoomType.Battle   => "BATTLE",
        RoomType.Elite    => "ELITE",
        RoomType.Boss     => "BOSS",
        RoomType.Treasure => "CHEST",
        RoomType.Rest     => "REST",
        _                 => "????"
    };

    /// <summary>Colour used when the room is accessible or current.</summary>
    public Color TypeColor => Type switch
    {
        RoomType.Entrance => new Color(220, 220, 220),
        RoomType.Exit     => new Color(80,  220, 220),
        RoomType.Battle   => new Color(160, 160, 170),
        RoomType.Elite    => new Color(255, 160,  30),
        RoomType.Boss     => new Color(220,  50,  50),
        RoomType.Treasure => new Color(255, 210,  40),
        RoomType.Rest     => new Color(60,  200,  80),
        _                 => Color.Gray
    };
}
