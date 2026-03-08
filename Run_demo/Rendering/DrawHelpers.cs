using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotliDemo
{
    public static class DrawHelpers
    {
        public static void DrawRectOutline(SpriteBatch sb, Texture2D pixel,
            Rectangle rect, Color color, int thickness)
        {
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            sb.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        public static void DrawLine(SpriteBatch sb, Texture2D pixel,
            Vector2 a, Vector2 b, Color color, int thickness)
        {
            Vector2 delta = b - a;
            float length = delta.Length();
            if (length < 0.5f) return;

            float angle = (float)Math.Atan2(delta.Y, delta.X);
            sb.Draw(pixel, a, null, color, angle, Vector2.Zero,
                new Vector2(length, thickness), SpriteEffects.None, 0f);
        }
    }
}
