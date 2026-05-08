// ============================================================
// FILE: screens/ShopScreen.cs — Spend gold on stat upgrades
// ============================================================
using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DungeonCrawler.logic;
using DungeonCrawler.utils;

namespace DungeonCrawler.screens;

/// <summary>
/// Mid-run shop. Displays N stat-upgrade offers and a reroll option.
/// Purchases mutate the player's BASE stats (gear bonuses still stack on top).
/// One shop is guaranteed per floor by MapGenerator.
/// </summary>
public class ShopScreen : IGameScreen
{
    private readonly GameContext         _ctx;
    private readonly Action<IGameScreen> _setScreen;
    private readonly IGameScreen         _returnTo;
    private readonly MenuSelector        _menu;

    private ShopOffer[] _offers;
    private bool[]      _purchased;
    private int         _rerollsThisVisit;

    private string _flashMsg;
    private float  _flashTimer;

    // Once Esc/Back is pressed we wait for it to release before transitioning,
    // so MapScreen's stale watcher doesn't see the same press as a fresh
    // Esc → save & quit.
    private bool _exitArmed;

    public ShopScreen(GameContext ctx, Action<IGameScreen> setScreen, IGameScreen returnTo)
    {
        _ctx       = ctx;
        _setScreen = setScreen;
        _returnTo  = returnTo;

        RollOffers();
        _menu = new MenuSelector(Math.Max(1, _offers.Length));
    }

    private void RollOffers()
    {
        _offers    = ShopOfferFactory.Generate(_ctx.Depth.CurrentDepth, _ctx.Rng);
        _purchased = new bool[_offers.Length];
    }

    private int CurrentRerollPrice
    {
        get
        {
            var cfg = GameConfig.Instance;
            return cfg.ShopRerollBasePrice + cfg.ShopRerollPriceIncrement * _rerollsThisVisit;
        }
    }

    public void Update(float dt)
    {
        if (_flashTimer > 0f) _flashTimer -= dt;

        _menu.UpdateHorizontal();

        // Once an exit has been requested, ignore further input and wait for
        // both Esc and Backspace to release before handing control back —
        // otherwise MapScreen sees the same Esc as a fresh press and saves &
        // quits immediately.
        if (_exitArmed)
        {
            if (!_menu.IsDown(Keys.Escape) && !_menu.IsDown(Keys.Back))
                _setScreen(_returnTo);
            return;
        }

        // Esc / Backspace to leave
        if (_menu.WasPressed(Keys.Escape) || _menu.WasPressed(Keys.Back))
        {
            _exitArmed = true;
            return;
        }

        // R to reroll
        if (_menu.WasPressed(Keys.R))
        {
            int cost = CurrentRerollPrice;
            if (_ctx.Depth.TrySpend(cost))
            {
                RollOffers();
                _menu.Index = 0;
                _rerollsThisVisit++;
                Flash($"Rerolled (-{cost}g)");
            }
            else
            {
                Flash("Not enough gold to reroll.");
            }
            return;
        }

        if (_menu.Confirmed)
            TryBuy(_menu.Index);
    }

    private void TryBuy(int idx)
    {
        if (idx < 0 || idx >= _offers.Length) return;
        if (_purchased[idx])
        {
            Flash("Already bought.");
            return;
        }

        var offer = _offers[idx];
        if (!_ctx.Depth.TrySpend(offer.Price))
        {
            Flash("Not enough gold.");
            return;
        }

        offer.ApplyTo(_ctx.Player);
        _purchased[idx] = true;
        Flash($"{offer.EffectLabel}!");
    }

    private void Flash(string msg)
    {
        _flashMsg   = msg;
        _flashTimer = 1.6f;
    }

    public void Draw(SpriteBatch sb)
    {
        var font = Game1.Resources.Font;

        // ── Header ─────────────────────────────────────────────
        DrawHelpers.CenterTextLarge(sb, "SHOPKEEP", 60, new Color(120, 180, 255));
        DrawHelpers.CenterText(sb,
            "\"Coin always finds a use down here, friend.\"",
            108, Color.LightGray);

        // ── Gold + floor banner ────────────────────────────────
        string gold  = $"Gold: {_ctx.Depth.Gold}";
        sb.DrawString(font, gold, new Vector2(40, 150), new Color(255, 215, 0));

        string floor = $"Floor {_ctx.Depth.CurrentDepth}";
        var floorSize = font.MeasureString(floor);
        sb.DrawString(font, floor,
            new Vector2(Game1.ScreenW - floorSize.X - 40, 150), Color.Gray);

        // ── Offer cards ────────────────────────────────────────
        if (_offers.Length > 0)
        {
            int cardW = 240, cardH = 150, gap = 30;
            int totalW = _offers.Length * cardW + (_offers.Length - 1) * gap;
            int startX = (Game1.ScreenW - totalW) / 2;
            int y = 230;

            for (int i = 0; i < _offers.Length; i++)
                DrawOfferCard(sb, _offers[i], i, startX + i * (cardW + gap), y, cardW, cardH);
        }

        // ── Controls + reroll ──────────────────────────────────
        int controlsY = 420;
        string rerollLine = $"[R] Reroll wares — {CurrentRerollPrice} gold";
        Color rerollColor = _ctx.Depth.Gold >= CurrentRerollPrice ? Color.White : Color.DarkGray;
        DrawHelpers.CenterText(sb, rerollLine, controlsY, rerollColor);

        DrawHelpers.CenterText(sb,
            "Left/Right to browse  ·  Enter to buy  ·  Esc to leave",
            controlsY + 30, Color.Gray);

        // ── Flash message ──────────────────────────────────────
        if (_flashTimer > 0f && !string.IsNullOrEmpty(_flashMsg))
        {
            float alpha = Math.Min(_flashTimer / 0.8f, 1f);
            DrawHelpers.CenterText(sb, _flashMsg, controlsY + 70,
                Color.Yellow * alpha);
        }
    }

    private void DrawOfferCard(SpriteBatch sb, ShopOffer offer, int idx, int x, int y, int w, int h)
    {
        var font = Game1.Resources.Font;
        bool isSelected  = idx == _menu.Index;
        bool isPurchased = _purchased[idx];
        bool canAfford   = !isPurchased && _ctx.Depth.Gold >= offer.Price;

        // Background — dimmer when sold out, brighter when selected
        Color bg;
        if (isPurchased)       bg = new Color(20, 20, 20) * 0.9f;
        else if (isSelected)   bg = Color.Black * 0.9f;
        else                   bg = Color.Black * 0.6f;
        DrawHelpers.DrawRect(sb, x - 10, y - 10, w, h, bg);

        // Selection highlight
        if (isSelected && !isPurchased)
            DrawHelpers.DrawRect(sb, x - 12, y - 12, w + 4, h + 4,
                new Color(120, 180, 255) * 0.3f);

        // Title
        Color titleColor = isPurchased ? new Color(60, 60, 60)
                                       : new Color(200, 220, 255);
        sb.DrawString(font, offer.Name, new Vector2(x, y), titleColor);

        // Effect line
        Color effectColor = isPurchased ? new Color(50, 50, 50) : Color.LimeGreen;
        sb.DrawString(font, offer.EffectLabel,
            new Vector2(x, y + 32), effectColor);

        // Price line
        string priceLine = isPurchased ? "SOLD" : $"{offer.Price} gold";
        Color priceColor;
        if (isPurchased)        priceColor = new Color(70, 70, 70);
        else if (canAfford)     priceColor = new Color(255, 215, 0);
        else                    priceColor = new Color(180, 60, 60);
        sb.DrawString(font, priceLine, new Vector2(x, y + h - 40), priceColor);
    }
}
