namespace BringTheBrotliDemo
{
    public class GameState
    {
        public int Coal;
        public int Water;
        public int Steam;
        public int Strikes;
        public float TimeRemaining;
        public float TrainProgress;
        public GamePhase CurrentPhase;

        public GameState()
        {
            Reset();
        }

        public void Reset()
        {
            Coal = 0;
            Water = 0;
            Steam = 0;
            Strikes = 0;
            TimeRemaining = GameConstants.DefaultTimeLimit;
            TrainProgress = 0f;
            CurrentPhase = GamePhase.Gameplay;
        }
    }
}
