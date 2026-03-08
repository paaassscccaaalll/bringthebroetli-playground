using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BringTheBrotliDemo
{
    public class MinigameRegistry
    {
        private readonly Dictionary<string, Func<Rectangle, IMinigame>> _factories
            = new(StringComparer.OrdinalIgnoreCase);

        public void Register(string zoneLabel, Func<Rectangle, IMinigame> factory)
        {
            _factories[zoneLabel] = factory;
        }

        public IMinigame? Create(string zoneLabel, Rectangle overlayBounds)
        {
            return _factories.TryGetValue(zoneLabel, out var factory)
                ? factory(overlayBounds)
                : null;
        }

        public bool HasMinigame(string zoneLabel) => _factories.ContainsKey(zoneLabel);
    }
}
