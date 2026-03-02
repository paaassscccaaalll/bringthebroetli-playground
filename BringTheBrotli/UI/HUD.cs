using System;
using System.Collections.Generic;
using BringTheBrotli.Core;
using BringTheBrotli.Players;
using BringTheBrotli.Train;
using BringTheBrotli.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotli.UI
{
    /// <summary>
    /// Draws the HUD overlay using the styled hud_panel texture and gauge frame sprites.
    /// Positioned in the lower portion of the screen below the 2.5D world view.
    /// </summary>
    public class HUD
    {
        private readonly TextRenderer _text;
        private float _flashTimer;
        private bool _flashVisible = true;

        // Circular event log buffer
        private readonly List<string> _eventLog = new();
        private const int MaxEvents = 5;

        public HUD(TextRenderer text)
        {
            _text = text;
        }

        public void AddEvent(string message)
        {
            _eventLog.Add(message);
            if (_eventLog.Count > MaxEvents)
                _eventLog.RemoveAt(0);
        }

        public void ClearEvents()
        {
            _eventLog.Clear();
        }

        public void Update(float deltaTime)
        {
            _flashTimer += deltaTime;
            if (_flashTimer >= 0.5f)
            {
                _flashTimer -= 0.5f;
                _flashVisible = !_flashVisible;
            }
        }

        /// <summary>
        /// Draw the full HUD overlay with styled panel background.
        /// </summary>
        public void Draw(SpriteBatch sb, TrainSystem train, TrackScroller scroller,
                          PlayerCharacter pc1, PlayerCharacter pc2,
                          float trainOriginX, float totalTrainLength,
                          IReadOnlyList<TaskStation> stations)
        {
            int hudY = (int)Constants.HudTopY;
            int hudH = Constants.ScreenHeight - hudY;

            // ---- PANEL BACKGROUND (sprite) ----
            var panelTex = TextureAtlas.HUDPanel;
            if (panelTex != null)
            {
                sb.Draw(panelTex, new Rectangle(0, hudY, Constants.ScreenWidth, hudH),
                    new Rectangle(0, 0, panelTex.Width, panelTex.Height), Color.White);
            }
            else
            {
                _text.DrawRect(sb, new Rectangle(0, hudY, Constants.ScreenWidth, hudH), new Color(15, 15, 25));
            }

            // ---- TRAIN STATUS BAR ----
            int barY = hudY + 8;

            // Speed
            string speedStr = $"Speed: {train.Speed:F0} km/h";
            _text.DrawString(sb, speedStr, new Vector2(14, barY), Color.White, 0.8f);

            // Distance
            string distStr = $"Dist: {train.DistanceTraveled:F1}/{TrainSystem.TotalDistance} km";
            _text.DrawString(sb, distStr, new Vector2(200, barY), Color.White, 0.8f);

            // Time
            float remaining = TrainSystem.TimeLimit - train.TimeElapsed;
            int mins = (int)(remaining / 60f);
            int secs = (int)(remaining % 60f);
            string timeStr = $"Time: {mins}:{secs:D2}";
            _text.DrawString(sb, timeStr, new Vector2(430, barY),
                remaining < 30 ? Color.Red : Color.White, 0.8f);

            // ---- GAUGES with gauge frame sprite ----
            DrawStyledGauge(sb, 600, barY - 2, "Coal", train.Firebox.CoalLevel / 100f, Color.Orange);
            DrawStyledGauge(sb, 750, barY - 2, "Temp", train.Firebox.Temperature / 100f, Color.OrangeRed);
            DrawStyledGauge(sb, 900, barY - 2, "Water", train.Boiler.WaterLevel / 100f, Color.Cyan);
            DrawStyledGauge(sb, 1050, barY - 2, "Press", train.Boiler.SteamPressure / 100f, Color.LimeGreen);

            // Progress bar
            float progressPct = train.DistanceTraveled / TrainSystem.TotalDistance;
            _text.DrawBar(sb, new Rectangle(14, barY + 22, 580, 12), progressPct, Color.Gold, new Color(40, 40, 40));

            // Next station indicator
            float distToStation = scroller.DistanceToNextStation(train.DistanceTraveled);
            if (distToStation < float.MaxValue)
            {
                string stationStr = $"NEXT STN: {distToStation:F1} km";
                _text.DrawString(sb, stationStr, new Vector2(600, barY + 22), Color.LightGoldenrodYellow, 0.7f);
            }
            else
            {
                _text.DrawString(sb, "Final stretch to Zurich!", new Vector2(600, barY + 22),
                    Color.LightGoldenrodYellow, 0.7f);
            }

            // Brake indicator
            if (train.IsBraking)
            {
                _text.DrawString(sb, "BRAKING", new Vector2(1200, barY + 22), Color.Red, 0.8f);
            }

            // ---- MINIMAP ----
            int mapY = barY + 42;
            int mapH = 24;
            int mapX = 14;
            int mapW = Constants.ScreenWidth - 28;

            _text.DrawRect(sb, new Rectangle(mapX, mapY, mapW, mapH), new Color(20, 20, 30));
            _text.DrawRectBorder(sb, new Rectangle(mapX, mapY, mapW, mapH), new Color(120, 100, 60), 1);

            // Train body on minimap
            int trainBarX = mapX + 4;
            int trainBarW = mapW - 8;
            _text.DrawRect(sb, new Rectangle(trainBarX, mapY + 6, trainBarW, 12), new Color(60, 60, 70));

            // Station ticks
            foreach (var station in stations)
            {
                float relPos = (station.WorldX - trainOriginX) / totalTrainLength;
                int tickX = trainBarX + (int)(relPos * trainBarW);
                Color tickColor = station.IsAvailable ? station.IndicatorColor : new Color(50, 50, 50);
                _text.DrawRect(sb, new Rectangle(tickX - 1, mapY + 2, 3, mapH - 4), tickColor);
            }

            // Player 1 dot (blue)
            float p1Rel = (pc1.WorldX - trainOriginX) / totalTrainLength;
            int p1x = trainBarX + (int)(Math.Clamp(p1Rel, 0f, 1f) * trainBarW);
            _text.DrawRect(sb, new Rectangle(p1x - 4, mapY + 4, 8, 8), Color.CornflowerBlue);
            _text.DrawRectBorder(sb, new Rectangle(p1x - 4, mapY + 4, 8, 8), Color.White, 1);

            // Player 2 dot (red)
            float p2Rel = (pc2.WorldX - trainOriginX) / totalTrainLength;
            int p2x = trainBarX + (int)(Math.Clamp(p2Rel, 0f, 1f) * trainBarW);
            _text.DrawRect(sb, new Rectangle(p2x - 4, mapY + 12, 8, 8), Color.Salmon);
            _text.DrawRectBorder(sb, new Rectangle(p2x - 4, mapY + 12, 8, 8), Color.White, 1);

            // Minimap task progress indicators
            foreach (var station in stations)
            {
                if (station.InUse && station.OccupiedByPlayer != null)
                {
                    float sRel = (station.WorldX - trainOriginX) / totalTrainLength;
                    int sx = trainBarX + (int)(sRel * trainBarW);
                    int barW = 20;
                    int fillW = (int)(barW * Math.Clamp(station.Progress, 0f, 1f));
                    _text.DrawRect(sb, new Rectangle(sx - barW / 2, mapY - 4, barW, 4), new Color(40, 40, 40));
                    _text.DrawRect(sb, new Rectangle(sx - barW / 2, mapY - 4, fillW, 4), station.IndicatorColor);
                }
            }

            // "P1" / "P2" labels
            _text.DrawString(sb, "P1", new Vector2(p1x - 6, mapY - 10), Color.CornflowerBlue, 0.5f);
            _text.DrawString(sb, "P2", new Vector2(p2x - 6, mapY + mapH + 1), Color.Salmon, 0.5f);

            // ---- EVENT LOG ----
            int logY = mapY + mapH + 6;
            int logH = Constants.ScreenHeight - logY - 8;
            _text.DrawRect(sb, new Rectangle(mapX, logY, mapW, logH), new Color(15, 15, 25, 180));
            _text.DrawRectBorder(sb, new Rectangle(mapX, logY, mapW, logH), new Color(80, 70, 50), 1);
            _text.DrawString(sb, "EVENT LOG:", new Vector2(mapX + 6, logY + 2), new Color(180, 150, 80), 0.7f);

            for (int i = 0; i < _eventLog.Count; i++)
            {
                _text.DrawString(sb, $"> {_eventLog[i]}", new Vector2(mapX + 6, logY + 16 + i * 14),
                    Color.LightGray, 0.7f);
            }
        }

        private void DrawStyledGauge(SpriteBatch sb, int x, int y, string label, float percent, Color color)
        {
            // Draw gauge frame sprite (scaled down to fit)
            var frameTex = TextureAtlas.UIGaugeFrame;
            if (frameTex != null)
            {
                int frameW = 130;
                int frameH = 20;
                sb.Draw(frameTex, new Rectangle(x, y, frameW, frameH),
                    new Rectangle(0, 0, frameTex.Width, frameTex.Height), Color.White);

                // Label on top of frame
                _text.DrawString(sb, label, new Vector2(x + 4, y + 2), Color.Gray, 0.55f);
                // Bar inside frame
                int barX = x + 38;
                int barW = 86;
                int barH = 10;
                int barY2 = y + 5;
                _text.DrawBar(sb, new Rectangle(barX, barY2, barW, barH), percent,
                    GetGaugeColor(percent, color), new Color(30, 30, 40));
            }
            else
            {
                // Fallback: simple text gauge
                _text.DrawString(sb, label, new Vector2(x, y), Color.Gray, 0.6f);
                int barX = x + 40;
                int barW = 90;
                int barH = 10;
                _text.DrawBar(sb, new Rectangle(barX, y + 2, barW, barH), percent,
                    GetGaugeColor(percent, color), new Color(40, 40, 40));
            }
        }

        private Color GetGaugeColor(float percent, Color baseColor)
        {
            if (percent < 0.15f && !_flashVisible)
                return Color.Transparent;
            if (percent < 0.3f) return Color.Red;
            if (percent < 0.6f) return Color.Yellow;
            return baseColor;
        }
    }
}
