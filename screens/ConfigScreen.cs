// ============================================================
// FILE: screens/ConfigScreen.cs — In-game config editor
// ============================================================
using System;
using System.Collections.Generic;
using System.Reflection;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DungeonCrawler.utils;

namespace DungeonCrawler.screens;

public class ConfigScreen : IGameScreen
{
    private readonly Action<IGameScreen> _setScreen;
    private readonly IGameScreen _returnScreen;

    // ── Config entries built via reflection ──
    private readonly List<ConfigEntry> _entries = new();
    private int _selectedIndex;
    private int _scrollOffset;
    private const int MaxDrawY = 480;  // stop drawing entries below this Y value

    // ── Input ──
    private KeyboardState _prevKb;
    private float _holdTimer;
    private float _holdRepeatRate = 0.05f;
    private float _holdDelay = 0.4f;
    private Keys? _heldKey;

    // ── Categories: maps each property to its group ──
    private static readonly Dictionary<string, string> PropertyCategories = new()
    {
        // Display
        ["ScreenWidth"]              = "DISPLAY",
        ["ScreenHeight"]             = "DISPLAY",
        ["Fullscreen"]               = "DISPLAY",
        ["VSync"]                    = "DISPLAY",
        // Player
        ["DefaultPlayerName"]        = "PLAYER",
        ["StartingMaxHp"]            = "PLAYER",
        ["StartingAttack"]           = "PLAYER",
        ["StartingDefense"]          = "PLAYER",
        ["StartingSpeed"]            = "PLAYER",
        ["StartingMagic"]            = "PLAYER",
        ["StartWithMagicWeapon"]     = "PLAYER",
        // Combat
        ["MinDamage"]                = "COMBAT",
        ["DefendBoost"]              = "COMBAT",
        ["DefendBlockPercent"]       = "COMBAT",
        ["DefendCounterMultiplier"]  = "COMBAT",
        ["HealBase"]                 = "COMBAT",
        ["DamageVariance"]           = "COMBAT",
        ["CritChance"]               = "COMBAT",
        ["CritMultiplier"]           = "COMBAT",
        ["PreActionDelay"]           = "COMBAT",
        ["BetweenActionDelay"]       = "COMBAT",
        ["EnemyAttackChance"]        = "COMBAT",
        ["FlashDuration"]            = "COMBAT",
        // Enemy Scaling
        ["EnemyBaseHp"]              = "ENEMY SCALING",
        ["EnemyBaseAttack"]          = "ENEMY SCALING",
        ["EnemyBaseDefense"]         = "ENEMY SCALING",
        ["EnemyBaseSpeed"]           = "ENEMY SCALING",
        ["EnemyBaseMagic"]           = "ENEMY SCALING",
        ["EnemyScalePerDepth"]       = "ENEMY SCALING",
        // Progression
        ["ScorePerDepth"]            = "PROGRESSION",
        ["HealPercentBetweenFloors"] = "PROGRESSION",
        ["MaxHpBoostPerFloor"]       = "PROGRESSION",
        ["AttackBoostPerFloor"]      = "PROGRESSION",
        // Loot
        ["LootChoiceCount"]          = "LOOT",
        ["LootTierDivisor"]          = "LOOT",
        ["LootMaxTier"]              = "LOOT",
        ["LootBaseStatValue"]        = "LOOT",
        ["LootScaleExponent"]        = "LOOT",
        ["LootScaleMultiplier"]      = "LOOT",
        ["CursedLootChance"]         = "LOOT",
        ["MaxRerollAttempts"]        = "LOOT",
    };

    public ConfigScreen(Action<IGameScreen> setScreen, IGameScreen returnScreen)
    {
        _setScreen = setScreen;
        _returnScreen = returnScreen;
        _prevKb = Keyboard.GetState();
        BuildEntries();
    }

    private void BuildEntries()
    {
        var cfg = GameConfig.Instance;
        var props = typeof(GameConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props)
        {
            // Skip non-editable types
            var type = prop.PropertyType;
            if (type != typeof(int) && type != typeof(float) &&
                type != typeof(bool) && type != typeof(string))
                continue;

            // Skip the singleton property
            if (prop.Name == "Instance") continue;

            _entries.Add(new ConfigEntry
            {
                Name = prop.Name,
                Property = prop,
                Category = GetCategory(prop.Name)
            });
        }
    }

    private static string GetCategory(string propName)
    {
        return PropertyCategories.TryGetValue(propName, out var cat) ? cat : "OTHER";
    }

    public void Update(float dt)
    {
        var kb = Keyboard.GetState();

        // Navigation
        if (WasPressed(kb, Keys.Up))
            _selectedIndex = (_selectedIndex - 1 + _entries.Count) % _entries.Count;
        if (WasPressed(kb, Keys.Down))
            _selectedIndex = (_selectedIndex + 1) % _entries.Count;

        // Adjust scroll to keep selection visible — we'll calculate during Draw

        // Edit values
        var entry = _entries[_selectedIndex];
        var type = entry.Property.PropertyType;

        if (type == typeof(int))
        {
            int val = (int)entry.Property.GetValue(GameConfig.Instance);
            int step = kb.IsKeyDown(Keys.LeftShift) ? 10 : 1;

            if (WasPressed(kb, Keys.Right) || HeldRepeat(kb, Keys.Right, dt))
                entry.Property.SetValue(GameConfig.Instance, val + step);
            if (WasPressed(kb, Keys.Left) || HeldRepeat(kb, Keys.Left, dt))
                entry.Property.SetValue(GameConfig.Instance, Math.Max(0, val - step));
        }
        else if (type == typeof(float))
        {
            float val = (float)entry.Property.GetValue(GameConfig.Instance);
            float step = kb.IsKeyDown(Keys.LeftShift) ? 0.1f : 0.01f;

            if (WasPressed(kb, Keys.Right) || HeldRepeat(kb, Keys.Right, dt))
                entry.Property.SetValue(GameConfig.Instance, MathF.Round(val + step, 2));
            if (WasPressed(kb, Keys.Left) || HeldRepeat(kb, Keys.Left, dt))
                entry.Property.SetValue(GameConfig.Instance, MathF.Round(MathF.Max(0, val - step), 2));
        }
        else if (type == typeof(bool))
        {
            if (WasPressed(kb, Keys.Right) || WasPressed(kb, Keys.Left))
            {
                bool val = (bool)entry.Property.GetValue(GameConfig.Instance);
                entry.Property.SetValue(GameConfig.Instance, !val);
            }
        }

        // Save & Exit
        if (WasPressed(kb, Keys.Enter))
        {
            GameConfig.Instance.Save();
            GameLogger.Info("Config saved from in-game editor");
            _setScreen(_returnScreen);
        }

        // Discard & Exit
        if (WasPressed(kb, Keys.Escape))
        {
            GameConfig.Reload();
            _setScreen(_returnScreen);
        }

        // Reset to defaults
        if (WasPressed(kb, Keys.Delete))
        {
            GameConfig.ResetToDefaults();
            GameLogger.Info("Config reset to defaults");
        }

        // Handle hold key reset
        if (_heldKey.HasValue && kb.IsKeyUp(_heldKey.Value))
        {
            _heldKey = null;
            _holdTimer = 0;
        }

        _prevKb = kb;
    }

    public void Draw(SpriteBatch sb)
    {
        var font = Game1.Resources.Font;

        DrawHelpers.CenterTextLarge(sb, "SETTINGS", 10, Color.Gold);

        // Column headers (draw after entries so they stay on top)
        int nameX = 40, valX = 420, startY = 55;
        int headerHeight = 30;
        int contentTop = startY + headerHeight;

        // First pass: figure out the Y position of the selected item
        int tempY = contentTop;
        string tempLastCat = "";
        int selectedY = tempY;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Category != tempLastCat)
            {
                tempY += 22;
                tempLastCat = _entries[i].Category;
            }
            if (i == _selectedIndex)
                selectedY = tempY;
            tempY += 26;
        }

        // Adjust scroll so selected item is visible
        if (selectedY - _scrollOffset < contentTop)
            _scrollOffset = selectedY - contentTop;
        if (selectedY - _scrollOffset > MaxDrawY - 26)
            _scrollOffset = selectedY - (MaxDrawY - 26);
        if (_scrollOffset < 0)
            _scrollOffset = 0;

        // Second pass: draw with scroll offset
        int y = contentTop - _scrollOffset;
        string lastCategory = "";
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            bool selected = i == _selectedIndex;

            // Category header
            if (entry.Category != lastCategory)
            {
                if (y >= contentTop && y < MaxDrawY)
                    sb.DrawString(font, $"── {entry.Category} ──", new Vector2(nameX, y), Color.DarkGray);
                y += 22;
                lastCategory = entry.Category;
            }

            // Skip entries outside visible area
            if (y >= contentTop && y < MaxDrawY)
            {
                // Background highlight
                if (selected)
                    DrawHelpers.DrawRect(sb, nameX - 5, y - 2, Game1.ScreenW - 70, 24, Color.White * 0.1f);

                // Name
                Color nameColor = selected ? Color.Yellow : Color.White;
                string displayName = FormatName(entry.Name);
                sb.DrawString(font, displayName, new Vector2(nameX, y), nameColor);

                // Value
                object val = entry.Property.GetValue(GameConfig.Instance);
                string valStr = val is float f ? f.ToString("F2") : val.ToString();
                Color valColor = selected ? Color.LimeGreen : Color.CornflowerBlue;
                sb.DrawString(font, valStr, new Vector2(valX, y), valColor);

                // Edit hint for selected row
                if (selected)
                {
                    string hint = entry.Property.PropertyType == typeof(bool)
                        ? "<Left/Right> Toggle"
                        : "<Left/Right> Adjust  |  Hold Shift for 10x";
                    sb.DrawString(font, hint, new Vector2(valX + 150, y), Color.DarkGray);
                }
            }

            y += 26;
        }

        // Column header bar — drawn last so it covers scrolled content
        DrawHelpers.DrawRect(sb, 0, startY - 4, Game1.ScreenW, headerHeight, Color.Black);
        sb.DrawString(font, "Setting", new Vector2(nameX, startY), Color.Gray);
        sb.DrawString(font, "Value", new Vector2(valX, startY), Color.Gray);

        // Scroll indicator
        string scroll = $"({_selectedIndex + 1}/{_entries.Count})";
        sb.DrawString(font, scroll, new Vector2(Game1.ScreenW - 120, startY), Color.Gray);

        // Footer
        int footerY = Game1.ScreenH - 40;
        DrawHelpers.DrawRect(sb, 0, footerY - 8, Game1.ScreenW, 40, Color.Black * 0.8f);
        DrawHelpers.CenterText(sb, "ENTER = Save & Back  |  ESC = Discard & Back  |  DEL = Reset Defaults", footerY, Color.Gray);
    }

    /// <summary>Converts "EnemyBaseHp" to "Enemy Base Hp"</summary>
    private static string FormatName(string name)
    {
        var chars = new List<char>();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                chars.Add(' ');
            chars.Add(name[i]);
        }
        return new string(chars.ToArray());
    }

    private bool WasPressed(KeyboardState kb, Keys key)
        => kb.IsKeyDown(key) && _prevKb.IsKeyUp(key);

    /// <summary>
    /// Returns true repeatedly while a key is held, after an initial delay.
    /// Used for smooth value scrubbing with Left/Right arrows.
    /// </summary>
    private bool HeldRepeat(KeyboardState kb, Keys key, float dt)
    {
        if (!kb.IsKeyDown(key)) return false;
        if (_prevKb.IsKeyUp(key))
        {
            _heldKey = key;
            _holdTimer = 0;
            return false; // First press handled by WasPressed
        }

        if (_heldKey != key) return false;

        _holdTimer += dt;
        if (_holdTimer < _holdDelay) return false;

        // Repeat at holdRepeatRate
        float elapsed = _holdTimer - _holdDelay;
        float prev = elapsed - dt;
        return (int)(elapsed / _holdRepeatRate) > (int)(prev / _holdRepeatRate);
    }

    private class ConfigEntry
    {
        public string Name;
        public PropertyInfo Property;
        public string Category;
    }
}