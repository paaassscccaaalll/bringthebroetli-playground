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
        public static void UpdateContinuous(GameState state, float dt)
        {
            if (state.CurrentPhase != GamePhase.Gameplay) return;

            // Furnace: convert Coal + Water → Steam
            float toConvert = Math.Min(state.Coal,
                Math.Min(state.Water, GameConstants.FurnaceConversionRate * dt));
            state.Coal -= toConvert;
            state.Water -= toConvert;
            state.Steam += toConvert;

            // Strike danger: coal in furnace with no water
            if (state.Coal > 0f && state.Water <= 0f)
            {
                state.StrikeDanger += dt;
                if (state.StrikeDanger >= GameConstants.StrikeDangerThreshold)
                {
                    state.Strikes = Math.Min(state.Strikes + 1, GameConstants.MaxStrikes);
                    state.StrikeDanger = 0f;
                }
            }
            else
            {
                state.StrikeDanger = 0f;
            }

            // Steam drives train
            float velocity = state.Steam * GameConstants.TrainSpeedPerSteam;
            state.TrainProgress += velocity * dt;

            // Steam dissipates (cooling)
            if (state.Steam > 0f)
                state.Steam = Math.Max(0f, state.Steam - GameConstants.SteamDecayRate * dt);

            // Coal decays slowly
            if (state.Coal > 0f)
                state.Coal = Math.Max(0f, state.Coal - GameConstants.CoalDecayRate * dt);

            state.TimeRemaining = Math.Max(0f, state.TimeRemaining - dt);
        }

        public static void ProcessBurnCoal(GameState state, int playerIndex)
        {
            var inv = state.Inventories[playerIndex];
            int coal = inv.Get(ResourceType.Coal);
            if (coal <= 0) return;
            state.Coal += coal;
            inv.Set(ResourceType.Coal, 0);
        }

        public static void ProcessPourWater(GameState state, int playerIndex)
        {
            var inv = state.Inventories[playerIndex];
            int water = inv.Get(ResourceType.Water);
            if (water <= 0) return;
            state.Water += water;
            inv.Set(ResourceType.Water, 0);
        }

        public static GameResult CheckWinConditions(GameState state)
        {
            if (state.Strikes >= GameConstants.MaxStrikes)
                return GameResult.ThiefWins;

            if (state.TrainProgress >= GameConstants.TrainTargetDistance)
                return GameResult.ChiefWins;

            if (state.TimeRemaining <= 0f)
                return GameResult.ThiefWins;

            return GameResult.InProgress;
        }
    }
}
