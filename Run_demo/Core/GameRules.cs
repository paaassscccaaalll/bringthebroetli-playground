using System;

namespace BringTheBrotliDemo
{
    public enum GameResult
    {
        InProgress,
        ChiefWins,
        ThiefWins
    }

    public static class GameRules
    {
        /// <summary>
        /// Continuous update each frame: train moves based on Steam, time counts down.
        /// </summary>
        public static void UpdateContinuous(GameState state, float deltaSeconds)
        {
            if (state.CurrentPhase != GamePhase.Gameplay) return;

            state.TrainProgress += state.Steam * GameConstants.TrainSpeedPerSteam * deltaSeconds;
            state.TimeRemaining = Math.Max(0f, state.TimeRemaining - deltaSeconds);
        }

        /// <summary>
        /// Manual turn processing (End Turn button for testing):
        /// 1. Strike check: if Coal > Water, Strikes++ (unsafe boiler pressure)
        /// 2. Conversion: X = min(C, W), W -= X, S += X (burn fuel into steam)
        /// </summary>
        public static void ProcessTurn(GameState state)
        {
            if (state.Coal > state.Water)
                state.Strikes = Math.Min(state.Strikes + 1, GameConstants.MaxStrikes);

            int converted = Math.Min(state.Coal, state.Water);
            state.Water -= converted;
            state.Steam += converted;
        }

        public static GameResult CheckWinConditions(GameState state)
        {
            if (state.TrainProgress >= GameConstants.TrainTargetDistance)
                return GameResult.ChiefWins;

            if (state.TimeRemaining <= 0f)
                return GameResult.ThiefWins;

            return GameResult.InProgress;
        }
    }
}
