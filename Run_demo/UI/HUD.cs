using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotliDemo
{
    public class HUD
    {
        private const int HudY = 830;
        private readonly GameState _state;

        public HUD(GameState state)
        {
            _state = state;
        }

        public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
        {
            sb.Draw(pixel, new Rectangle(0, HudY - 10, 1920, 260), new Color(20, 20, 30, 200));

            // Global train resources
            sb.DrawString(font, $"Coal: {_state.Coal:F0}", new Vector2(20, HudY), Color.Orange);
            sb.DrawString(font, $"Water: {_state.Water:F0}", new Vector2(180, HudY), Color.CornflowerBlue);
            sb.DrawString(font, $"Steam: {_state.Steam:F0}", new Vector2(360, HudY), Color.LightGray);

            float velocity = _state.Steam * GameConstants.TrainSpeedPerSteam;
            sb.DrawString(font, $"Speed: {velocity:F1}", new Vector2(540, HudY), Color.Gray);

            Color strikeColor = _state.Strikes >= GameConstants.MaxStrikes ? Color.Red : Color.OrangeRed;
            sb.DrawString(font,
                $"Broetli Strikes: {_state.Strikes}/{GameConstants.MaxStrikes}",
                new Vector2(720, HudY), strikeColor);

            // Time and progress
            int mins = (int)_state.TimeRemaining / 60;
            int secs = (int)_state.TimeRemaining % 60;
            Color timeColor = _state.TimeRemaining < 30f ? Color.Red : Color.White;
            sb.DrawString(font, $"Time: {mins}:{secs:D2}", new Vector2(20, HudY + 30), timeColor);
            sb.DrawString(font,
                $"Progress: {_state.TrainProgress:F1} / {GameConstants.TrainTargetDistance:F0}",
                new Vector2(200, HudY + 30), Color.White);

            // Player inventories
            for (int i = 0; i < _state.Inventories.Length; i++)
            {
                var inv = _state.Inventories[i];
                float y = HudY + 60 + i * 25;
                string label = $"P{i + 1}: Coal {inv.Get(ResourceType.Coal)}/{GameConstants.MaxCarryCapacity}" +
                               $"  Water {inv.Get(ResourceType.Water)}/{GameConstants.MaxCarryCapacity}";
                Color pColor = i == 0 ? Color.White : new Color(180, 200, 255);
                sb.DrawString(font, label, new Vector2(20, y), pColor);
            }
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
