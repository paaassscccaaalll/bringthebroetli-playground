using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotliDemo
{
    public class PlaceholderMinigame : IMinigame
    {
        private readonly string _label;
        private readonly ResourceType _resourceType;
        private readonly int _resourceDelta;
        private readonly Rectangle _overlayBounds;
        private float _elapsed;

        public bool IsComplete => _elapsed >= GameConstants.MinigameDuration;
        public MinigameResult Result { get; private set; }
        public Rectangle OverlayBounds => _overlayBounds;

        public PlaceholderMinigame(string label, ResourceType resourceType,
                                   int resourceDelta, Rectangle overlayBounds)
        {
            _label = label;
            _resourceType = resourceType;
            _resourceDelta = resourceDelta;
            _overlayBounds = overlayBounds;
        }

        public void Start()
        {
            _elapsed = 0f;
            Result = new MinigameResult(_resourceType, _resourceDelta);
        }

        public void Update(GameTime gameTime)
        {
            _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
        {
            sb.Draw(pixel, _overlayBounds, new Color(30, 30, 50, 230));
            DrawHelpers.DrawRectOutline(sb, pixel, _overlayBounds, Color.White, 2);

            string sign = _resourceDelta >= 0 ? "+" : "";
            string text = $"{_label}\n{sign}{_resourceDelta} {_resourceType}";
            Vector2 textPos = new Vector2(_overlayBounds.X + 12, _overlayBounds.Y + 12);
            sb.DrawString(font, text, textPos, Color.White);

            float progress = Math.Clamp(_elapsed / GameConstants.MinigameDuration, 0f, 1f);
            int barWidth = _overlayBounds.Width - 24;
            int barHeight = 14;
            int barX = _overlayBounds.X + 12;
            int barY = _overlayBounds.Bottom - barHeight - 12;

            sb.Draw(pixel, new Rectangle(barX, barY, barWidth, barHeight), new Color(60, 60, 60));
            sb.Draw(pixel, new Rectangle(barX, barY, (int)(barWidth * progress), barHeight), Color.LimeGreen);
        }
    }
}
