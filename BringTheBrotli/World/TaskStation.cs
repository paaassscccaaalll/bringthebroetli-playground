using System;
using Microsoft.Xna.Framework;

namespace BringTheBrotli.World
{
    /// <summary>
    /// A task station anchored to a specific world-space X position on the train.
    /// Players walk to a station and hold their interact key to perform the task.
    /// </summary>
    public class TaskStation
    {
        public string Name { get; }
        public float WorldX { get; set; }           // world-space X position (center of station)
        public float InteractRadius { get; }        // default 40px
        public float TaskDuration { get; }          // seconds player must hold to complete
        public Color IndicatorColor { get; }        // color of the station marker
        public Action? OnComplete { get; set; }     // callback into TrainSystem (wired at init)
        public bool IsAvailable => CooldownTimer <= 0f && !InUse;
        public float Cooldown { get; }              // seconds before usable again after completion
        public float CooldownTimer { get; set; }
        public bool InUse { get; set; }             // true while a player is actively working this station
        public int? OccupiedByPlayer { get; set; }  // player index currently using this station, or null

        // Progress for the player currently working this station (0..1)
        public float Progress { get; set; }

        public TaskStation(string name, float worldX, float taskDuration, Color indicatorColor,
                           float interactRadius = 40f, float cooldown = 3f)
        {
            Name = name;
            WorldX = worldX;
            InteractRadius = interactRadius;
            TaskDuration = taskDuration;
            IndicatorColor = indicatorColor;
            Cooldown = cooldown;
        }

        /// <summary>Returns true if the given world X is within interact range of this station.</summary>
        public bool IsInRange(float playerWorldX)
        {
            return MathF.Abs(playerWorldX - WorldX) <= InteractRadius;
        }

        /// <summary>Tick cooldown timer.</summary>
        public void UpdateCooldown(float dt)
        {
            if (CooldownTimer > 0f)
                CooldownTimer -= dt;
        }

        /// <summary>Reset station state when task completes or is cancelled.</summary>
        public void ReleaseStation()
        {
            InUse = false;
            OccupiedByPlayer = null;
            Progress = 0f;
        }
    }
}
