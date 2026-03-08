using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BringTheBrotliDemo
{
    public class ResourceDisplay
    {
        private const int ButtonWidth = 44;
        private const int ButtonHeight = 26;
        private const int ButtonGap = 3;
        private const int LabelToButtonGap = 26;

        public string Label { get; }
        public Color LabelColor { get; set; } = Color.White;

        private readonly Func<int> _getValue;
        private readonly Vector2 _position;
        private readonly List<UIButton> _buttons = new();

        public ResourceDisplay(string label, Vector2 position, Func<int> getValue)
        {
            Label = label;
            _position = position;
            _getValue = getValue;
        }

        public void AddButton(string text, Action onClick)
        {
            int x = (int)_position.X + _buttons.Count * (ButtonWidth + ButtonGap);
            int y = (int)_position.Y + LabelToButtonGap;
            _buttons.Add(new UIButton(
                new Rectangle(x, y, ButtonWidth, ButtonHeight), text, onClick));
        }

        public void Update(MouseState mouse)
        {
            foreach (var btn in _buttons)
                btn.Update(mouse);
        }

        public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
        {
            string display = $"{Label}: {_getValue()}";
            sb.DrawString(font, display, _position, LabelColor);

            foreach (var btn in _buttons)
                btn.Draw(sb, font, pixel);
        }
    }
}
