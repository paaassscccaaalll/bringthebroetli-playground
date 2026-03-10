using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotliDemo
{
    public class MinigameManager
    {
        private const int OverlayWidth = 250;
        private const int OverlayHeight = 120;
        private const int OverlayGap = 10;

        private readonly MinigameRegistry _registry;
        private readonly CollisionSystem _collision;
        private readonly Dictionary<int, IMinigame> _active = new();

        public bool IsPlayerInMinigame(int playerIndex) => _active.ContainsKey(playerIndex);

        public MinigameManager(MinigameRegistry registry, CollisionSystem collision)
        {
            _registry = registry;
            _collision = collision;
        }

        public bool TryStartMinigame(int playerIndex, string zoneLabel, GameState state)
        {
            if (_active.ContainsKey(playerIndex)) return false;
            if (!_registry.HasMinigame(zoneLabel)) return false;

            Rectangle zoneBounds = FindZoneBounds(zoneLabel);
            int overlayX = zoneBounds.Center.X - OverlayWidth / 2;
            int overlayY = zoneBounds.Top - OverlayHeight - OverlayGap;
            var overlayBounds = new Rectangle(overlayX, overlayY, OverlayWidth, OverlayHeight);

            var minigame = _registry.Create(zoneLabel, overlayBounds);
            if (minigame == null) return false;

            minigame.Start();
            _active[playerIndex] = minigame;
            return true;
        }

        public void Update(GameTime gameTime, GameState state)
        {
            var completed = new List<int>();
            foreach (var (playerIndex, minigame) in _active)
            {
                minigame.Update(gameTime);
                if (minigame.IsComplete)
                {
                    ApplyResult(minigame.Result, state, playerIndex);
                    completed.Add(playerIndex);
                }
            }
            foreach (var idx in completed)
                _active.Remove(idx);
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel)
        {
            foreach (var minigame in _active.Values)
                minigame.Draw(spriteBatch, font, pixel);
        }

        private static void ApplyResult(MinigameResult result, GameState state, int playerIndex)
        {
            var inv = state.Inventories[playerIndex];
            switch (result.ResourceType)
            {
                case ResourceType.Coal:
                case ResourceType.Water:
                    inv.Add(result.ResourceType, result.ResourceDelta,
                        GameConstants.MaxCarryCapacity);
                    break;
                case ResourceType.Steam:
                    float prev = state.Steam;
                    state.Steam = Math.Max(0f, state.Steam + result.ResourceDelta);
                    if (prev > 0f && state.Steam <= 0f)
                        state.Strikes = Math.Min(state.Strikes + 1, GameConstants.MaxStrikes);
                    break;
            }
        }

        private Rectangle FindZoneBounds(string label)
        {
            foreach (var zone in _collision.ActionZones)
            {
                if (string.Equals(zone.Label, label, StringComparison.OrdinalIgnoreCase))
                    return zone.Bounds;
            }
            return Rectangle.Empty;
        }
    }
}
