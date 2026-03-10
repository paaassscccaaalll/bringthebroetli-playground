using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotliDemo
{
    public class HUD
    {
        private const int HudY = 830;
        private const int HudPanelPadding = 10;
        private const int HudPanelHeight = 260;
        private const int ColumnCoal = 20;
        private const int ColumnWater = 180;
        private const int ColumnSteam = 360;
        private const int ColumnSpeed = 540;
        private const int ColumnStrikes = 720;
        private const int ColumnProgress = 200;
        private const int RowSpacing = 30;
        private const int InventoryRowOffset = 60;
        private const int InventoryRowHeight = 25;

        private readonly GameState _state;

        public HUD(GameState state)
        {
            _state = state;
        }

        public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
        {
            sb.Draw(pixel, new Rectangle(0, HudY - HudPanelPadding, 1920, HudPanelHeight),
                new Color(20, 20, 30, 200));

            // Global train resources
            sb.DrawString(font, $"Coal: {_state.Coal:F0}", new Vector2(ColumnCoal, HudY), Color.Orange);
            sb.DrawString(font, $"Water: {_state.Water:F0}", new Vector2(ColumnWater, HudY), Color.CornflowerBlue);
            sb.DrawString(font, $"Steam: {_state.Steam:F0}", new Vector2(ColumnSteam, HudY), Color.LightGray);

            float velocity = _state.Steam * GameConstants.TrainSpeedPerSteam;
            sb.DrawString(font, $"Speed: {velocity:F1}", new Vector2(ColumnSpeed, HudY), Color.Gray);

            Color strikeColor = _state.Strikes >= GameConstants.MaxStrikes ? Color.Red : Color.OrangeRed;
            sb.DrawString(font,
                $"Broetli Strikes: {_state.Strikes}/{GameConstants.MaxStrikes}",
                new Vector2(ColumnStrikes, HudY), strikeColor);

            // Time and progress
            int mins = (int)_state.TimeRemaining / 60;
            int secs = (int)_state.TimeRemaining % 60;
            Color timeColor = _state.TimeRemaining < 30f ? Color.Red : Color.White;
            sb.DrawString(font, $"Time: {mins}:{secs:D2}", new Vector2(ColumnCoal, HudY + RowSpacing), timeColor);
            sb.DrawString(font,
                $"Progress: {_state.TrainProgress:F1} / {GameConstants.TrainTargetDistance:F0}",
                new Vector2(ColumnProgress, HudY + RowSpacing), Color.White);

            // Player inventories
            for (int i = 0; i < _state.Inventories.Length; i++)
            {
                var inv = _state.Inventories[i];
                float y = HudY + InventoryRowOffset + i * InventoryRowHeight;
                string label = $"P{i + 1}: Coal {inv.Get(ResourceType.Coal)}/{GameConstants.MaxCarryCapacity}" +
                               $"  Water {inv.Get(ResourceType.Water)}/{GameConstants.MaxCarryCapacity}";
                Color pColor = i == 0 ? Color.White : new Color(180, 200, 255);
                sb.DrawString(font, label, new Vector2(ColumnCoal, y), pColor);
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
