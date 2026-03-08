using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BringTheBrotliDemo
{
    public class HUD
    {
        private const int HudY = 830;
        private const int InfoY = 900;

        private readonly GameState _state;
        private readonly List<ResourceDisplay> _resources = new();
        private readonly List<UIButton> _buttons = new();

        public HUD(GameState state)
        {
            _state = state;
            BuildResourceDisplays();
            BuildControlButtons();
        }

        private void BuildResourceDisplays()
        {
            var coal = new ResourceDisplay("Coal", new Vector2(20, HudY), () => _state.Coal);
            coal.AddButton("+1", () => _state.Coal += 1);
            coal.AddButton("+2", () => _state.Coal += 2);
            coal.AddButton("+3", () => _state.Coal += 3);
            coal.AddButton("+4", () => _state.Coal += 4);
            coal.AddButton("+5", () => _state.Coal += 5);
            coal.AddButton("-1", () => _state.Coal = Math.Max(0, _state.Coal - 1));
            _resources.Add(coal);

            var water = new ResourceDisplay("Water", new Vector2(320, HudY), () => _state.Water);
            water.AddButton("+1", () => _state.Water += 1);
            water.AddButton("+2", () => _state.Water += 2);
            water.AddButton("+3", () => _state.Water += 3);
            water.AddButton("+4", () => _state.Water += 4);
            water.AddButton("+5", () => _state.Water += 5);
            water.AddButton("-1", () => _state.Water = Math.Max(0, _state.Water - 1));
            _resources.Add(water);

            var steam = new ResourceDisplay("Steam", new Vector2(620, HudY), () => _state.Steam);
            steam.AddButton("+1", () => _state.Steam += 1);
            steam.AddButton("-1", () => _state.Steam = Math.Max(0, _state.Steam - 1));
            steam.AddButton("-2", () => _state.Steam = Math.Max(0, _state.Steam - 2));
            steam.AddButton("-3", () => _state.Steam = Math.Max(0, _state.Steam - 3));
            steam.AddButton("-4", () => _state.Steam = Math.Max(0, _state.Steam - 4));
            steam.AddButton("-5", () => _state.Steam = Math.Max(0, _state.Steam - 5));
            steam.AddButton("=0", () => _state.Steam = 0);
            _resources.Add(steam);

            var strikes = new ResourceDisplay(
                "Broetli Strikes", new Vector2(960, HudY), () => _state.Strikes);
            strikes.LabelColor = Color.OrangeRed;
            strikes.AddButton("+1", () =>
                _state.Strikes = Math.Min(GameConstants.MaxStrikes, _state.Strikes + 1));
            strikes.AddButton("-1", () =>
                _state.Strikes = Math.Max(0, _state.Strikes - 1));
            _resources.Add(strikes);
        }

        private void BuildControlButtons()
        {
            _buttons.Add(new UIButton(
                new Rectangle(20, InfoY + 40, 160, 36), "Convert C+W",
                () => GameRules.ProcessTurn(_state))
            { BackgroundColor = new Color(40, 80, 40) });

            _buttons.Add(new UIButton(
                new Rectangle(200, InfoY + 40, 100, 36), "Reset",
                () => _state.Reset())
            { BackgroundColor = new Color(80, 40, 40) });
        }

        public void Update(MouseState mouse)
        {
            foreach (var r in _resources)
                r.Update(mouse);
            foreach (var b in _buttons)
                b.Update(mouse);
        }

        public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
        {
            sb.Draw(pixel, new Rectangle(0, HudY - 10, 1920, 260), new Color(20, 20, 30, 200));

            foreach (var r in _resources)
                r.Draw(sb, font, pixel);
            foreach (var b in _buttons)
                b.Draw(sb, font, pixel);

            int mins = (int)_state.TimeRemaining / 60;
            int secs = (int)_state.TimeRemaining % 60;
            sb.DrawString(font, $"Time: {mins}:{secs:D2}", new Vector2(400, InfoY), Color.White);
            sb.DrawString(font,
                $"Progress: {_state.TrainProgress:F1} / {GameConstants.TrainTargetDistance:F0}",
                new Vector2(560, InfoY), Color.White);
            sb.DrawString(font, $"Velocity: {_state.Steam} steam", new Vector2(820, InfoY), Color.Gray);
        }

        public void DrawTooltips(SpriteBatch sb, SpriteFont font, Texture2D pixel,
                                 PlayerCharacter[] players)
        {
            foreach (var player in players)
            {
                if (player.CurrentZoneLabel == null) continue;

                Vector2 textSize = font.MeasureString(player.CurrentZoneLabel);
                int padX = 8, padY = 4;
                float tooltipX = player.Position.X - textSize.X / 2f - padX;
                float tooltipY = player.Position.Y - 90f;

                Rectangle bg = new Rectangle(
                    (int)tooltipX, (int)tooltipY,
                    (int)textSize.X + padX * 2,
                    (int)textSize.Y + padY * 2);

                sb.Draw(pixel, bg, new Color(255, 255, 255, 220));
                DrawHelpers.DrawRectOutline(sb, pixel, bg, new Color(60, 60, 60), 1);
                sb.DrawString(font, player.CurrentZoneLabel,
                    new Vector2(bg.X + padX, bg.Y + padY), new Color(40, 40, 40));
            }
        }
    }
}
