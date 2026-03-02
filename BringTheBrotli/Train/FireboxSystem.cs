using System;

namespace BringTheBrotli.Train
{
    /// <summary>
    /// Manages the firebox: coal level, temperature, and heat output.
    /// Player 1 shovels coal by holding W for 1 second.
    /// </summary>
    public class FireboxSystem
    {
        // --- Constants ---
        public const float MaxCoal = 100f;
        public const float MaxTemperature = 100f;
        public const float CoalDrainRate = 1f;           // per second
        public const float TempRiseRate = 8f;             // per second when coal > 20
        public const float TempFallRate = 12f;            // per second when coal <= 20
        public const float CoalThreshold = 20f;           // minimum coal for temp to rise
        public const float ShovelCoalAmount = 25f;        // coal added per shovel action
        public const float ColdCoalDip = 10f;             // temperature dip when adding cold coal

        // --- Properties ---
        public float CoalLevel { get; set; } = 70f;       // Start with some coal
        public float Temperature { get; set; } = 50f;     // Start warm

        /// <summary>Derived heat output used by BoilerSystem. Range ~0..1.</summary>
        public float HeatOutput => Temperature * 0.01f;

        // --- Drain multiplier for random events ---
        public float CoalDrainMultiplier { get; set; } = 1f;

        /// <summary>Update firebox simulation each frame.</summary>
        public void Update(float deltaTime)
        {
            // Coal drains over time
            CoalLevel -= CoalDrainRate * CoalDrainMultiplier * deltaTime;
            CoalLevel = MathF.Max(CoalLevel, 0f);

            // Temperature dynamics
            if (CoalLevel > CoalThreshold)
            {
                Temperature += TempRiseRate * deltaTime;
            }
            else
            {
                Temperature -= TempFallRate * deltaTime;
            }
            Temperature = Math.Clamp(Temperature, 0f, MaxTemperature);
        }

        /// <summary>
        /// Called when Player 1 finishes the shovel action (held W for 1 second).
        /// Adds coal and causes a brief temperature dip.
        /// </summary>
        public void ShovelCoal()
        {
            CoalLevel = MathF.Min(CoalLevel + ShovelCoalAmount, MaxCoal);
            Temperature = MathF.Max(Temperature - ColdCoalDip, 0f);
        }

        public void Reset()
        {
            CoalLevel = 70f;
            Temperature = 50f;
            CoalDrainMultiplier = 1f;
        }
    }
}
