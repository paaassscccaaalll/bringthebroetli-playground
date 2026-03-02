using System;

namespace BringTheBrotli.Train
{
    /// <summary>
    /// Manages the boiler: water level, steam pressure, and auto-vent logic.
    /// Player 2 fills water by holding Up for 1 second.
    /// Player 1 can manually vent steam with S.
    /// </summary>
    public class BoilerSystem
    {
        // --- Constants ---
        public const float MaxWater = 100f;
        public const float MaxPressure = 100f;
        public const float WaterDrainRate = 1.5f;              // per second when pressure > 10
        public const float PressureGainRate = 0.3f;            // multiplier on heat output
        public const float PressurePassiveLoss = 5f;           // per second (steam being used)
        public const float WaterFillAmount = 30f;              // water added per fill action
        public const float ManualVentReduction = 20f;          // pressure reduced by S key
        public const float AutoVentDropTarget = 40f;           // pressure drops to this on auto-vent
        public const float WaterMinForSteam = 20f;             // water must be > this for pressure gain
        public const float LowWaterPressureDrain = 15f;        // extra drain when water <= 0

        // --- Properties ---
        public float WaterLevel { get; set; } = 80f;
        public float SteamPressure { get; set; } = 30f;

        /// <summary>Hidden auto-vent threshold, randomized at game start (80–95).</summary>
        public float AutoVentThreshold { get; private set; }
        public bool AutoVentTriggered { get; set; }

        // --- Drain multiplier for random events ---
        public float PressureDrainMultiplier { get; set; } = 1f;

        private static readonly Random _rng = new();

        public BoilerSystem()
        {
            RandomizeAutoVentThreshold();
        }

        public void RandomizeAutoVentThreshold()
        {
            AutoVentThreshold = 80f + (float)_rng.NextDouble() * 15f; // 80-95
        }

        /// <summary>Update boiler simulation each frame.</summary>
        /// <param name="heatOutput">HeatOutput from FireboxSystem (0..1).</param>
        /// <param name="deltaTime">Seconds since last frame.</param>
        /// <returns>Event message if auto-vent triggered, otherwise null.</returns>
        public string? Update(float heatOutput, float deltaTime)
        {
            string? eventMessage = null;

            // Steam pressure increases when there's water and heat
            if (WaterLevel > WaterMinForSteam)
            {
                SteamPressure += heatOutput * PressureGainRate * deltaTime * 60f;
                // Note: multiplied by 60 to make the rate feel meaningful per second
            }

            // Passive pressure loss (steam being consumed)
            SteamPressure -= PressurePassiveLoss * PressureDrainMultiplier * deltaTime;

            // Water drains when there's active steam
            if (SteamPressure > 10f)
            {
                WaterLevel -= WaterDrainRate * deltaTime;
            }

            // Penalty if water is empty
            if (WaterLevel <= 0f)
            {
                SteamPressure -= LowWaterPressureDrain * deltaTime;
            }

            // Auto-vent check
            AutoVentTriggered = false;
            if (SteamPressure >= AutoVentThreshold)
            {
                SteamPressure = AutoVentDropTarget;
                AutoVentTriggered = true;
                eventMessage = "AUTO-VENT triggered! Pressure dropped!";
            }

            // Clamp values
            WaterLevel = Math.Clamp(WaterLevel, 0f, MaxWater);
            SteamPressure = Math.Clamp(SteamPressure, 0f, MaxPressure);

            return eventMessage;
        }

        /// <summary>Player 2 fills water (after holding Up for 1 second).</summary>
        public void FillWater()
        {
            WaterLevel = MathF.Min(WaterLevel + WaterFillAmount, MaxWater);
        }

        /// <summary>Player 1 manually vents steam (press S).</summary>
        public void ManualVent()
        {
            SteamPressure = MathF.Max(SteamPressure - ManualVentReduction, 0f);
        }

        public void Reset()
        {
            WaterLevel = 80f;
            SteamPressure = 30f;
            PressureDrainMultiplier = 1f;
            AutoVentTriggered = false;
            RandomizeAutoVentThreshold();
        }
    }
}
