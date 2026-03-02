using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotli.Core
{
    /// <summary>
    /// Helper utility for drawing text and colored rectangles using SpriteBatch.
    /// Wraps SpriteFont usage and provides bar-drawing helpers.
    /// </summary>
    public class TextRenderer
    {
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        public SpriteFont Font => _font;

        public TextRenderer(SpriteFont font, GraphicsDevice graphicsDevice)
        {
            _font = font;

            // Create a 1x1 white pixel texture used for drawing rectangles
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        /// <summary>
        /// Strips characters the SpriteFont cannot render (outside ASCII 32-126).
        /// Prevents crashes from em-dashes, Unicode, etc.
        /// </summary>
        private static string Sanitize(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c >= 32 && c <= 126)
                    sb.Append(c);
                else if (c == '\n' || c == '\r')
                    sb.Append(c);
                else
                    sb.Append('-'); // safe fallback for dashes, special chars, etc.
            }
            return sb.ToString();
        }

        /// <summary>Draw a string at the given position.</summary>
        public void DrawString(SpriteBatch sb, string text, Vector2 position, Color color, float scale = 1f)
        {
            sb.DrawString(_font, Sanitize(text), position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>Draw a string centered horizontally at the given Y position.</summary>
        public void DrawStringCentered(SpriteBatch sb, string text, float y, Color color, float scale = 1f)
        {
            string safe = Sanitize(text);
            var size = _font.MeasureString(safe) * scale;
            var x = (1280f - size.X) / 2f;
            DrawString(sb, safe, new Vector2(x, y), color, scale);
        }

        /// <summary>Draw a filled rectangle.</summary>
        public void DrawRect(SpriteBatch sb, Rectangle rect, Color color)
        {
            sb.Draw(_pixel, rect, color);
        }

        /// <summary>Draw a horizontal gauge bar with background and fill.</summary>
        public void DrawBar(SpriteBatch sb, Rectangle bounds, float percent, Color fillColor, Color bgColor)
        {
            // Background
            sb.Draw(_pixel, bounds, bgColor);
            // Fill
            int fillWidth = (int)(bounds.Width * MathHelper.Clamp(percent, 0f, 1f));
            sb.Draw(_pixel, new Rectangle(bounds.X, bounds.Y, fillWidth, bounds.Height), fillColor);
        }

        /// <summary>Draw a hollow rectangle (border only).</summary>
        public void DrawRectBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness = 2)
        {
            // Top
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // Left
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            sb.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        /// <summary>Get the color for a gauge value (green > 60%, yellow 30-60%, red < 30%).</summary>
        public static Color GetGaugeColor(float percent)
        {
            if (percent > 0.6f) return Color.LimeGreen;
            if (percent > 0.3f) return Color.Yellow;
            return Color.Red;
        }
    }
}
