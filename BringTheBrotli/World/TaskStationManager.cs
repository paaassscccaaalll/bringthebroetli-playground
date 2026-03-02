using System;
using System.Collections.Generic;
using BringTheBrotli.Core;
using BringTheBrotli.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotli.World
{
    /// <summary>
    /// Owns all TaskStation instances. Each frame:
    /// - Updates cooldown timers
    /// - Checks proximity of each player to each station
    /// - Handles hold-to-act progress and fires callbacks on completion
    /// - Draws station markers and interaction prompts/progress bars
    /// </summary>
    public class TaskStationManager
    {
        private readonly TextRenderer _text;
        private readonly List<TaskStation> _stations = new();

        // Track the nearest station for each player (for UI prompts)
        public TaskStation? P1NearestStation { get; private set; }
        public TaskStation? P2NearestStation { get; private set; }

        public IReadOnlyList<TaskStation> Stations => _stations;

        public TaskStationManager(TextRenderer text)
        {
            _text = text;
        }

        public void AddStation(TaskStation station)
        {
            _stations.Add(station);
        }

        /// <summary>
        /// Update all stations: cooldowns, proximity detection, hold-to-act progress.
        /// </summary>
        public void Update(float dt, PlayerCharacter p1, PlayerCharacter p2,
                           bool p1Holding, bool p2Holding, Action<string> addEvent)
        {
            // Update cooldowns
            foreach (var station in _stations)
                station.UpdateCooldown(dt);

            // Find nearest available station for each player
            P1NearestStation = FindNearestStation(p1.WorldX);
            P2NearestStation = FindNearestStation(p2.WorldX);

            // Handle Player 1 interaction
            HandlePlayerInteraction(dt, p1, p1Holding, P1NearestStation, addEvent, "Player 1");

            // Handle Player 2 interaction
            HandlePlayerInteraction(dt, p2, p2Holding, P2NearestStation, addEvent, "Player 2");
        }

        private void HandlePlayerInteraction(float dt, PlayerCharacter pc, bool isHolding,
                                              TaskStation? nearestStation, Action<string> addEvent,
                                              string playerLabel)
        {
            if (isHolding && nearestStation != null && nearestStation.IsAvailable)
            {
                // Start or continue working this station
                if (!nearestStation.InUse)
                {
                    nearestStation.InUse = true;
                    nearestStation.OccupiedByPlayer = pc.PlayerIndex;
                    nearestStation.Progress = 0f;
                    pc.IsPerformingTask = true;
                }

                // Only the player who started can continue
                if (nearestStation.OccupiedByPlayer == pc.PlayerIndex)
                {
                    nearestStation.Progress += dt / nearestStation.TaskDuration;
                    if (nearestStation.Progress >= 1f)
                    {
                        // Task complete!
                        nearestStation.OnComplete?.Invoke();
                        addEvent($"{playerLabel} completed: {nearestStation.Name}!");
                        nearestStation.CooldownTimer = nearestStation.Cooldown;
                        nearestStation.ReleaseStation();
                        pc.IsPerformingTask = false;
                    }
                }
            }
            else
            {
                // Player released key or moved out of range -- cancel any in-progress task
                foreach (var station in _stations)
                {
                    if (station.OccupiedByPlayer == pc.PlayerIndex && station.InUse)
                    {
                        station.ReleaseStation();
                        pc.IsPerformingTask = false;
                    }
                }
            }
        }

        private TaskStation? FindNearestStation(float worldX)
        {
            TaskStation? best = null;
            float bestDist = float.MaxValue;
            foreach (var station in _stations)
            {
                float dist = MathF.Abs(worldX - station.WorldX);
                if (dist <= station.InteractRadius && dist < bestDist)
                {
                    bestDist = dist;
                    best = station;
                }
            }
            return best;
        }

        /// <summary>Draw station markers on the train roof.</summary>
        public void DrawMarkers(SpriteBatch sb, Camera camera, PlayerCharacter p1, PlayerCharacter p2)
        {
            foreach (var station in _stations)
            {
                float screenX = camera.WorldToScreenX(station.WorldX);
                int sx = (int)screenX - 10;
                int markerY = (int)Constants.RoofTopY + 5;
                int markerH = (int)Constants.RoofHeight - 10;

                Color color = station.IndicatorColor;
                bool highlighted = station.IsInRange(p1.WorldX) || station.IsInRange(p2.WorldX);

                if (!station.IsAvailable && !station.InUse)
                {
                    // On cooldown -- dim the color
                    color = new Color(color.R / 3, color.G / 3, color.B / 3);
                }

                // Marker rectangle on the roof
                _text.DrawRect(sb, new Rectangle(sx, markerY, 20, markerH), color);

                // Highlight border when player is nearby
                if (highlighted)
                {
                    _text.DrawRectBorder(sb, new Rectangle(sx - 1, markerY - 1, 22, markerH + 2), Color.White, 1);
                }

                // Station name label above marker
                _text.DrawString(sb, station.Name, new Vector2(sx - 10, markerY - 14), Color.White, 0.6f);
            }
        }

        /// <summary>Draw interaction prompts and progress bars above player characters.</summary>
        public void DrawPrompts(SpriteBatch sb, Camera camera, PlayerCharacter p1, PlayerCharacter p2)
        {
            DrawPlayerPrompt(sb, camera, p1, P1NearestStation, "[SPACE]");
            DrawPlayerPrompt(sb, camera, p2, P2NearestStation, "[ENTER]");
        }

        private void DrawPlayerPrompt(SpriteBatch sb, Camera camera, PlayerCharacter pc,
                                       TaskStation? nearestStation, string keyHint)
        {
            if (nearestStation == null) return;

            float screenX = camera.WorldToScreenX(pc.WorldX);
            float promptY = pc.TopY - 30;

            if (nearestStation.InUse && nearestStation.OccupiedByPlayer == pc.PlayerIndex)
            {
                // Draw progress bar above player head
                int barW = 80;
                int barH = 10;
                int barX = (int)(screenX - barW / 2f);
                int barY = (int)(promptY - 4);

                // Background
                _text.DrawRect(sb, new Rectangle(barX, barY, barW, barH), new Color(40, 40, 40));
                // Fill
                int fillW = (int)(barW * Math.Clamp(nearestStation.Progress, 0f, 1f));
                _text.DrawRect(sb, new Rectangle(barX, barY, fillW, barH), nearestStation.IndicatorColor);
                // Border
                _text.DrawRectBorder(sb, new Rectangle(barX, barY, barW, barH), Color.White, 1);
            }
            else if (nearestStation.InUse && nearestStation.OccupiedByPlayer != pc.PlayerIndex)
            {
                // Someone else is using this station
                DrawTooltip(sb, screenX, promptY, "In use", Color.Gray);
            }
            else if (!nearestStation.IsAvailable)
            {
                // On cooldown
                string coolText = $"Cooldown {nearestStation.CooldownTimer:F1}s";
                DrawTooltip(sb, screenX, promptY, coolText, Color.Gray);
            }
            else
            {
                // Show interact prompt
                string prompt = $"{keyHint} {nearestStation.Name}";
                DrawTooltip(sb, screenX, promptY, prompt, Color.Yellow);
            }
        }

        private void DrawTooltip(SpriteBatch sb, float centerX, float y, string text, Color textColor)
        {
            // Measure text width roughly (assume ~8px per char at scale 0.7)
            int textW = text.Length * 7;
            int padX = 6;
            int padY = 3;
            int bgX = (int)(centerX - textW / 2f - padX);
            int bgY = (int)(y - padY);

            // Dark semi-transparent background
            _text.DrawRect(sb, new Rectangle(bgX, bgY, textW + padX * 2, 16 + padY * 2),
                new Color(0, 0, 0, 180));
            _text.DrawString(sb, text, new Vector2(bgX + padX, bgY + padY), textColor, 0.7f);
        }

        /// <summary>Update all station WorldX positions relative to a train origin offset.</summary>
        public void SetTrainOrigin(float trainOriginX, List<(float offset, TaskStation station)> stationOffsets)
        {
            foreach (var (offset, station) in stationOffsets)
            {
                station.WorldX = trainOriginX + offset;
            }
        }
    }
}
