using System;

namespace BringTheBrotli.Train
{
    /// <summary>
    /// Central train simulation aggregating boiler and firebox systems.
    /// Manages speed, distance, time, and win/loss conditions.
    /// </summary>
    public class TrainSystem
    {
        // --- Constants ---
        public const float MaxSpeed = 120f;
        public const float SpeedAccelRate = 5f;           // how fast speed approaches target
        public const float DistanceScale = 0.01f;         // speed-to-distance scaling
        public const float TotalDistance = 30f;            // km to destination
        public const float TimeLimit = 180f;              // seconds (3 minutes)
        public const float BrakeSpeedReduction = 30f;     // speed reduction when braking

        // --- Sub-systems ---
        public BoilerSystem Boiler { get; } = new();
        public FireboxSystem Firebox { get; } = new();

        // --- Properties ---
        public float Speed { get; set; }
        public float DistanceTraveled { get; set; }
        public float TimeElapsed { get; set; }
        public bool GameOver { get; set; }
        public bool CitizensWin { get; set; }
        public string GameOverReason { get; set; } = "";

        // Braking state
        public bool IsBraking { get; set; }

        /// <summary>Target speed derived from steam pressure.</summary>
        public float TargetSpeed => (Boiler.SteamPressure / BoilerSystem.MaxPressure) * MaxSpeed;

        // --- Wind gust penalty ---
        public float SpeedPenalty { get; set; }

        /// <summary>
        /// Update the full train simulation for one frame.
        /// Returns any event message from sub-systems (e.g. auto-vent).
        /// </summary>
        public string? Update(float deltaTime)
        {
            if (GameOver) return null;

            TimeElapsed += deltaTime;

            // Update sub-systems
            Firebox.Update(deltaTime);
            string? boilerEvent = Boiler.Update(Firebox.HeatOutput, deltaTime);

            // Speed trends toward target
            float target = TargetSpeed - SpeedPenalty;
            if (IsBraking)
            {
                target = MathF.Max(target - BrakeSpeedReduction, 0f);
            }
            target = Math.Clamp(target, 0f, MaxSpeed);

            if (Speed < target)
                Speed += SpeedAccelRate * deltaTime;
            else if (Speed > target)
                Speed -= SpeedAccelRate * deltaTime;

            Speed = Math.Clamp(Speed, 0f, MaxSpeed);

            // Distance
            DistanceTraveled += Speed * deltaTime * DistanceScale;

            // Win/loss checks
            if (DistanceTraveled >= TotalDistance)
            {
                GameOver = true;
                CitizensWin = true;
                GameOverReason = "Broetli delivered! The citizens feast!";
            }
            else if (TimeElapsed >= TimeLimit)
            {
                GameOver = true;
                CitizensWin = false;
                GameOverReason = "Time ran out! The Broetli grew stale...";
            }

            return boilerEvent;
        }

        public void Reset()
        {
            Speed = 0f;
            DistanceTraveled = 0f;
            TimeElapsed = 0f;
            GameOver = false;
            CitizensWin = false;
            GameOverReason = "";
            IsBraking = false;
            SpeedPenalty = 0f;
            Boiler.Reset();
            Firebox.Reset();
        }
    }
}
