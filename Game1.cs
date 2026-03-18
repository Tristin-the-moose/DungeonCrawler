// ============================================================
// FILE: Game1.cs — Main MonoGame entry point
// ============================================================
using System;
using System.IO;
using FontStashSharp;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using DungeonCrawler.models;
using DungeonCrawler.logic;

namespace DungeonCrawler;

public class Game1 : Game
{
    // ── Core ──
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _sb;
    private Random _rng = new();

    // ── Textures ──
    private Texture2D _pixel;          // 1x1 white pixel for drawing rects
    private Texture2D _playerSprite;
    private Texture2D[] _enemySprites;
    private Texture2D _bgBattle;
    private SpriteFontBase _font;
    private SpriteFontBase _fontLarge;

    // ── Game State ──
    private GamePhase _phase = GamePhase.Title;
    private BattleSystem _battle;
    private Fighter _player;
    private DepthManager _depth = new();

    // ── Input ──
    private KeyboardState _prevKb;
    private int _menuIndex = 0;
    private readonly BattleActionType[] _menuOptions =
        { BattleActionType.Attack, BattleActionType.Magic,
          BattleActionType.Defend, BattleActionType.Heal };

    private int _titleIndex = 0;
    private int _victoryIndex = 0;

    private Equipment[] _lootChoices;
    private int _lootIndex = 0;

    // ── Screen ──
    private const int ScreenW = 960;
    private const int ScreenH = 540;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = ScreenW,
            PreferredBackBufferHeight = ScreenH
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    // ────────────────────────────────────────────────────────
    //  LOAD CONTENT
    // ────────────────────────────────────────────────────────
    protected override void LoadContent()
    {
        _sb = new SpriteBatch(GraphicsDevice);

        // 1x1 white pixel (for drawing rectangles procedurally)
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // Load your sprites via the Content Pipeline:
        //   _playerSprite = Content.Load<Texture2D>("player");
        //   _bgBattle     = Content.Load<Texture2D>("battle_bg");
        //   _enemySprites = new[] {
        //       Content.Load<Texture2D>("enemy_slime"),
        //       Content.Load<Texture2D>("enemy_goblin"),
        //       ... etc
        //   };

        var fontSystem = new FontSystem();
        fontSystem.AddFont(File.ReadAllBytes("styles/fonts/arial.ttf"));
        _font = fontSystem.GetFont(18);
        _fontLarge = fontSystem.GetFont(32);

        // ── PLACEHOLDER SPRITES (colored rectangles) ──
        // Replace these with real textures once you have art
        _playerSprite = MakePlaceholder(64, 96, Color.CornflowerBlue);
        _bgBattle     = MakePlaceholder(ScreenW, ScreenH, new Color(30, 20, 40));
        _enemySprites = new[]
        {
            MakePlaceholder(56, 72, Color.LimeGreen),
            MakePlaceholder(60, 80, Color.OrangeRed),
            MakePlaceholder(72, 88, Color.MediumPurple),
            MakePlaceholder(80, 96, Color.DarkRed),
            MakePlaceholder(96, 112, Color.Gold),
        };

        // ── Initialize player ──
        InitPlayer();
    }

    private Texture2D MakePlaceholder(int w, int h, Color color)
    {
        var tex = new Texture2D(GraphicsDevice, w, h);
        var data = new Color[w * h];
        Array.Fill(data, color);
        // Add a 1px border for visibility
        for (int x = 0; x < w; x++)
        {
            data[x] = Color.White;              // top
            data[(h - 1) * w + x] = Color.White; // bottom
        }
        for (int y = 0; y < h; y++)
        {
            data[y * w] = Color.White;          // left
            data[y * w + w - 1] = Color.White;  // right
        }
        tex.SetData(data);
        return tex;
    }

    private void InitPlayer(string characterName = "Hero")
    {
        var stats = new Stats
        {
            Name = characterName, MaxHp = 100, Hp = 100,
            Attack = 12, Defense = 5, Speed = 7, Magic = 8
        };
        _player = new Fighter(stats, isPlayer: true)
        {
            Sprite = _playerSprite,
            Scale = 2.0f,  // foreground = larger
            FlipEffect = SpriteEffects.None
        };
    }

    private void LoadSavedGame()
    {
        var save = SaveSystem.Load();
        if (save == null) return;

        _depth = new DepthManager
        {
            CurrentDepth = save.CurrentDepth,
            TotalKills   = save.TotalKills,
            Score        = save.Score
        };

        var stats = new Stats
        {
            Name    = save.PlayerName,
            MaxHp   = save.MaxHp,
            Hp      = save.Hp,
            Attack  = save.Attack,
            Defense = save.Defense,
            Speed   = save.Speed,
            Magic   = save.Magic,
        };

        var gear = new EquipmentSet {
            HeadPiece = save.HeadPiece,
            ChestPiece = save.ChestPiece,
            Leggings = save.Leggings,
            Booties = save.Booties,

            Ring = save.Ring,
            Necklace = save.Necklace,
            Weapon = save.Weapon
        };

        _player = new Fighter(stats, isPlayer: true)
        {
            Sprite = _playerSprite,
            Scale = 2.0f,
            FlipEffect = SpriteEffects.None,
            Equipment = gear
        };
    }

    // ────────────────────────────────────────────────────────
    //  UPDATE (Input + Logic)
    // ────────────────────────────────────────────────────────
    protected override void Update(GameTime gt)
    {
        float dt = (float)gt.ElapsedGameTime.TotalSeconds;
        var kb = Keyboard.GetState();

        switch (_phase)
        {
            case GamePhase.Title:
                UpdateTitle(kb);
                break;
            case GamePhase.Loot:
                UpdateLoot(kb);
                break;
            case GamePhase.Battle:
                UpdateBattle(kb, dt);
                break;

            case GamePhase.Victory:
                UpdateVictory(kb);
                break;

            case GamePhase.GameOver:
            case GamePhase.FinalScore:
                if (WasPressed(kb, Keys.Enter))
                {
                    // Reset everything
                    _depth = new DepthManager();
                    InitPlayer();
                    _phase = GamePhase.Title;
                }
                break;
        }

        _prevKb = kb;
        base.Update(gt);
    }

    private void UpdateTitle(KeyboardState kb)
    {
        int optionCount = SaveSystem.HasSave() ? 3 : 2;

        if (WasPressed(kb, Keys.Up))
            _titleIndex = (_titleIndex - 1 + optionCount) % optionCount;
        if (WasPressed(kb, Keys.Down))
            _titleIndex = (_titleIndex + 1) % optionCount;

        if (WasPressed(kb, Keys.Enter) || WasPressed(kb, Keys.Space))
        {
            if (SaveSystem.HasSave())
            {
                switch (_titleIndex)
                {
                    case 0: // New Game
                        SaveSystem.Delete();
                        InitPlayer();
                        _depth = new DepthManager();
                        StartNewBattle();
                        break;
                    case 1: // Continue
                        LoadSavedGame();
                        StartNewBattle();
                        break;
                    case 2: // Exit
                        Exit();
                        break;
                }
            }
            else
            {
                switch (_titleIndex)
                {
                    case 0: // Start Game
                        StartNewBattle();
                        break;
                    case 1: // Exit
                        Exit();
                        break;
                }
            }
        }
    }

    private void UpdateBattle(KeyboardState kb, float dt)
    {
        _battle.Update(dt);

        if (_battle.State == BattleTurnState.PlayerChoosing)
        {
            // Navigate menu
            if (WasPressed(kb, Keys.Up))
                _menuIndex = (_menuIndex - 1 + _menuOptions.Length)
                             % _menuOptions.Length;
            if (WasPressed(kb, Keys.Down))
                _menuIndex = (_menuIndex + 1) % _menuOptions.Length;

            // Confirm selection
            if (WasPressed(kb, Keys.Enter) || WasPressed(kb, Keys.Space))
                _battle.SubmitPlayerAction(_menuOptions[_menuIndex]);
        }

        // Transition on win/lose
        if (_battle.State == BattleTurnState.BattleWon)
        {
            _depth.OnVictory();
            _lootChoices = LootFactory.GenerateChoices(_depth.CurrentDepth, _rng);
            _lootIndex = 0;
            _victoryIndex = 0;
            _phase = GamePhase.Loot;
        }
        else if (_battle.State == BattleTurnState.BattleLost)
        {
            SaveSystem.Delete();
            _phase = GamePhase.GameOver;
        }
    }

    private void UpdateVictory(KeyboardState kb)
{
    if (WasPressed(kb, Keys.Up))
        _victoryIndex = (_victoryIndex - 1 + 3) % 3;
    if (WasPressed(kb, Keys.Down))
        _victoryIndex = (_victoryIndex + 1) % 3;

    if (WasPressed(kb, Keys.Enter) || WasPressed(kb, Keys.Space))
    {
        switch (_victoryIndex)
        {
            case 0: // Go Deeper
                _depth.GoDeeper();
                _depth.RestBetweenFloors(_player.Stats);
                SaveSystem.Save(_player, _depth);
                StartNewBattle();
                break;
            case 1: // Save & Quit
                _depth.GoDeeper();
                _depth.RestBetweenFloors(_player.Stats);
                SaveSystem.Save(_player, _depth);
                _phase = GamePhase.Title;
                _titleIndex = 0;
                break;
            case 2: // Cash Out
                SaveSystem.Delete();
                _phase = GamePhase.FinalScore;
                break;
        }
    }
}

    private void StartNewBattle()
    {
        var enemy = EnemyFactory.Create(
            _depth.CurrentDepth, _enemySprites, _rng);

        // Position sprites: player in foreground (bottom-left),
        // enemy in background (top-right, smaller)
        _player.Position = new Vector2(100, ScreenH - 250);
        _player.Scale = 2.0f;

        enemy.Position = new Vector2(ScreenW - 250, 80);
        enemy.Scale = 1.2f;
        enemy.FlipEffect = SpriteEffects.FlipHorizontally;

        _battle = new BattleSystem(_player, enemy, _depth.CurrentDepth);
        _menuIndex = 0;
        _phase = GamePhase.Battle;
    }

    // ────────────────────────────────────────────────────────
    //  DRAW
    // ────────────────────────────────────────────────────────
    protected override void Draw(GameTime gt)
    {
        GraphicsDevice.Clear(Color.Black);
        _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                  SamplerState.PointClamp); // PointClamp for pixel art

        switch (_phase)
        {
            case GamePhase.Title:      DrawTitle();    break;
            case GamePhase.Battle:     DrawBattle();   break;
            case GamePhase.Victory:    DrawVictory();  break;
            case GamePhase.GameOver:   DrawGameOver(); break;
            case GamePhase.FinalScore: DrawScore();    break;
            case GamePhase.Loot:       DrawLoot();     break;
        }

        _sb.End();
        base.Draw(gt);
    }

    // ── Title Screen ──
    private void DrawTitle()
    {
        string title = "DUNGEON CRAWLER";
        var size = _fontLarge.MeasureString(title);
        _sb.DrawString(_fontLarge, title,
            new Vector2((ScreenW - size.X) / 2, 160), Color.Gold);

        if (SaveSystem.HasSave())
        {
            DrawTitleOption("New Game", 0, 280, Color.White);
            DrawTitleOption("Continue", 1, 320, Color.LimeGreen);
            DrawTitleOption("Exit", 2, 360, Color.Gray);
        }
        else
        {
            DrawTitleOption("Start Game", 0, 280, Color.White);
            DrawTitleOption("Exit", 1, 320, Color.Gray);
        }
    }

    private void DrawTitleOption(string label, int index, int y, Color baseColor)
    {
        bool selected = _titleIndex == index;
        string prefix = selected ? "> " : "  ";
        Color color = selected ? Color.Yellow : baseColor;
        CenterText(prefix + label, y, color);
    }

    // ── Battle Screen ──
    private void DrawBattle()
    {
        // Background
        _sb.Draw(_bgBattle, Vector2.Zero, Color.White);

        // Draw fighters
        _battle.Enemy.Draw(_sb);
        _battle.Player.Draw(_sb);

        // ── HP Bars ──
        DrawHpBar(20, 20, 200, 20, _battle.Player);
        DrawHpBar(ScreenW - 220, 20, 200, 20, _battle.Enemy);

        // ── Action Menu (bottom-left) ──
        if (_battle.State == BattleTurnState.PlayerChoosing)
        {
            int menuX = 40, menuY = ScreenH - 140;
            DrawRect(menuX - 10, menuY - 10, 200, 130,
                     Color.Black * 0.8f);

            for (int i = 0; i < _menuOptions.Length; i++)
            {
                string label = _menuOptions[i].ToString();
                Color c = i == _menuIndex ? Color.Yellow : Color.White;
                string prefix = i == _menuIndex ? "> " : "  ";
                _sb.DrawString(_font, prefix + label,
                    new Vector2(menuX, menuY + i * 28), c);
            }
        }

        // ── Battle Log (bottom-right) ──
        int logX = ScreenW - 400, logY = ScreenH - 140;
        DrawRect(logX - 10, logY - 10, 390, 130, Color.Black * 0.8f);

        var log = _battle.Log;
        int start = Math.Max(0, log.Count - 4);
        for (int i = start; i < log.Count; i++)
        {
            _sb.DrawString(_font, log[i],
                new Vector2(logX, logY + (i - start) * 28),
                Color.LightGray);
        }
    }

    private void DrawEquipLine(int x, ref int y, string label, Equipment item)
    {
        Color c = item.Rarity switch
        {
            0 => Color.Gray,
            1 => Color.White,
            2 => Color.LimeGreen,
            3 => Color.CornflowerBlue,
            4 => Color.Gold,
            _ => Color.Gray
        };
        _sb.DrawString(_font, $"{label}: {item.Name}", new Vector2(x, y), c);
        y += 24;
    }

    // ── Loot Selection Screen ──
    private void UpdateLoot(KeyboardState kb)
    {
        if (WasPressed(kb, Keys.Left))
            _lootIndex = (_lootIndex - 1 + _lootChoices.Length) % _lootChoices.Length;
        if (WasPressed(kb, Keys.Right))
            _lootIndex = (_lootIndex + 1) % _lootChoices.Length;

        if (WasPressed(kb, Keys.Enter) || WasPressed(kb, Keys.Space))
        {
            _player.Equipment.Equip(_lootChoices[_lootIndex]);
            _phase = GamePhase.Victory;
        }

        if (WasPressed(kb, Keys.S))
        {
            // Skip loot
            _phase = GamePhase.Victory;
        }
    }

    private void DrawLoot()
    {
        CenterText("Choose Your Loot!", 40, Color.Gold);
        CenterText("Left/Right to browse, Enter to equip, S to skip", 70, Color.Gray);

        // Show current equipped item for comparison
        if (_lootChoices != null && _lootChoices.Length > 0)
        {
            var selected = _lootChoices[_lootIndex];
            var current = _player.Equipment.Get(selected.EquipmentType);

            // Current item
            int curX = 80, curY = 140;
            DrawRect(curX - 10, curY - 10, 300, 120, Color.Black * 0.8f);
            _sb.DrawString(_font, "Currently Equipped:", new Vector2(curX, curY), Color.Gray);
            _sb.DrawString(_font, $"{current.Name}", new Vector2(curX, curY + 26), Color.White);
            _sb.DrawString(_font, $"{current.StatSummary()}", new Vector2(curX, curY + 52), Color.Gray);

            // Loot choices
            int startX = 80;
            for (int i = 0; i < _lootChoices.Length; i++)
            {
                var item = _lootChoices[i];
                int ix = startX + i * 280;
                int iy = 290;
                bool selected2 = i == _lootIndex;

                Color border = selected2 ? Color.Yellow : Color.Gray;
                Color bg = selected2 ? Color.Black * 0.9f : Color.Black * 0.6f;

                DrawRect(ix - 10, iy - 10, 260, 140, bg);
                // Highlight border
                if (selected2)
                {
                    DrawRect(ix - 12, iy - 12, 264, 144, Color.Yellow * 0.3f);
                }

                Color tierColor = item.Rarity switch
                {
                    0 => Color.Gray,
                    1 => Color.White,
                    2 => Color.LimeGreen,
                    3 => Color.CornflowerBlue,
                    4 => Color.Gold,
                    _ => Color.Gray
                };

                _sb.DrawString(_font, $"[{item.EquipmentType}]", new Vector2(ix, iy), Color.Gray);
                _sb.DrawString(_font, item.Name, new Vector2(ix, iy + 26), tierColor);
                _sb.DrawString(_font, item.StatSummary(), new Vector2(ix, iy + 52), Color.White);

                // Show comparison
                var cur = _player.Equipment.Get(item.EquipmentType);
                string cmp = CompareGear(item, cur);
                Color cmpColor = cmp.Contains("+") ? Color.LimeGreen : Color.Red;
                if (cmp == "Same") cmpColor = Color.Gray;
                _sb.DrawString(_font, cmp, new Vector2(ix, iy + 82), cmpColor);
            }
        }
    }

    private string CompareGear(Equipment newItem, Equipment current)
    {
        int diff = (newItem.AttackBonus - current.AttackBonus)
                 + (newItem.DefenseBonus - current.DefenseBonus)
                 + (newItem.SpeedBoost - current.SpeedBoost)
                 + (newItem.MagicBonus - current.MagicBonus)
                 + (newItem.HealthBonus - current.HealthBonus);

        if (diff > 0) return $"+{diff} total stats";
        if (diff < 0) return $"{diff} total stats";
        return "Same";
    }

    // ── Victory / Continue Screen ──
    private void DrawVictory()
    {
        string msg = $"Victory!  Depth: {_depth.CurrentDepth}  " +
                    $"Score: {_depth.Score}";
        CenterText(msg, 140, Color.Gold);

        string hp = $"HP: {_player.Stats.Hp}/{_player.Stats.MaxHp}";
        CenterText(hp, 200, Color.LightGreen);

        DrawVictoryOption("Go Deeper",   0, 280, Color.White);
        DrawVictoryOption("Save & Quit", 1, 320, Color.CornflowerBlue);
        DrawVictoryOption("Cash Out",    2, 360, Color.Gray);
    }

    private void DrawVictoryOption(string label, int index, int y, Color baseColor)
    {
        bool selected = _victoryIndex == index;
        string prefix = selected ? "> " : "  ";
        Color color = selected ? Color.Yellow : baseColor;
        CenterText(prefix + label, y, color);
    }

    // ── Game Over ──
    private void DrawGameOver()
    {
        CenterText("YOU DIED", 180, Color.Red);
        CenterText($"Reached depth {_depth.CurrentDepth}  |  " +
                   $"Score: {_depth.Score}", 240, Color.Gray);
        CenterText("Press ENTER to restart", 340, Color.White);
    }

    // ── Final Score ──
    private void DrawScore()
    {
        CenterText("DUNGEON COMPLETE", 140, Color.Gold);
        CenterText($"Final Depth: {_depth.CurrentDepth}", 220, Color.White);
        CenterText($"Enemies Slain: {_depth.TotalKills}", 260, Color.White);
        CenterText($"Final Score: {_depth.Score}", 300, Color.Yellow);
        CenterText("Press ENTER to play again", 400, Color.Gray);
    }

    // ────────────────────────────────────────────────────────
    //  HELPERS
    // ────────────────────────────────────────────────────────
    private void DrawHpBar(int x, int y, int w, int h, Fighter f)
    {
        DrawRect(x, y, w, h, Color.DarkGray);
        int fill = (int)(w * f.Stats.HpPercent);
        Color barColor = f.Stats.HpPercent > 0.5f ? Color.LimeGreen
                       : f.Stats.HpPercent > 0.25f ? Color.Yellow
                       : Color.Red;
        DrawRect(x, y, fill, h, barColor);

        string label = $"{f.Stats.Name}  {f.Stats.Hp}/{f.Stats.MaxHp}";
        _sb.DrawString(_font, label,
            new Vector2(x + 4, y + 1), Color.White);
    }

    private void DrawRect(int x, int y, int w, int h, Color c)
    {
        _sb.Draw(_pixel, new Rectangle(x, y, w, h), c);
    }

    private void CenterText(string text, int y, Color color)
    {
        var size = _font.MeasureString(text);
        _sb.DrawString(_font, text,
            new Vector2((ScreenW - size.X) / 2, y), color);
    }

    private bool WasPressed(KeyboardState kb, Keys key)
        => kb.IsKeyDown(key) && _prevKb.IsKeyUp(key);
}