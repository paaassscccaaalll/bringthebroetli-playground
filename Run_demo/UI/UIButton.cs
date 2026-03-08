using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BringTheBrotliDemo
{
    public class UIButton
    {
        public Rectangle Bounds { get; }
        public string Text { get; }
        public Color BackgroundColor { get; set; } = new Color(60, 60, 80);
        public Color HoverColor { get; set; } = new Color(80, 80, 110);
        public Color TextColor { get; set; } = Color.White;

        private readonly Action _onClick;
        private bool _wasPressed;
        private bool _hovering;

        public UIButton(Rectangle bounds, string text, Action onClick)
        {
            Bounds = bounds;
            Text = text;
            _onClick = onClick;
        }

        public void Update(MouseState mouse)
        {
            _hovering = Bounds.Contains(mouse.Position);
            bool isPressed = mouse.LeftButton == ButtonState.Pressed && _hovering;

            if (!isPressed && _wasPressed && _hovering)
                _onClick();

            _wasPressed = isPressed;
        }

        public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
        {
            Color bg = _hovering ? HoverColor : BackgroundColor;
            sb.Draw(pixel, Bounds, bg);
            DrawHelpers.DrawRectOutline(sb, pixel, Bounds, new Color(100, 100, 120), 1);

            Vector2 textSize = font.MeasureString(Text);
            Vector2 textPos = new Vector2(
                Bounds.X + (Bounds.Width - textSize.X) / 2f,
                Bounds.Y + (Bounds.Height - textSize.Y) / 2f);
            sb.DrawString(font, Text, textPos, TextColor);
        }
    }
}
