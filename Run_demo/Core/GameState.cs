namespace BringTheBrotliDemo
{
    public class GameState
    {
        public const int PlayerCount = 2;

        public float Coal;
        public float Water;
        public float Steam;
        public int Strikes;
        public float StrikeDanger;
        public float TimeRemaining;
        public float TrainProgress;
        public GamePhase CurrentPhase;
        public PlayerInventory[] Inventories;

        public GameState()
        {
            Inventories = new PlayerInventory[PlayerCount];
            for (int i = 0; i < PlayerCount; i++)
                Inventories[i] = new PlayerInventory();
            Reset();
        }

        public void Reset()
        {
            Coal = 0f;
            Water = 0f;
            Steam = 0f;
            Strikes = 0;
            StrikeDanger = 0f;
            TimeRemaining = GameConstants.DefaultTimeLimit;
            TrainProgress = 0f;
            CurrentPhase = GamePhase.Gameplay;
            foreach (var inv in Inventories)
                inv.Reset();
        }
    }
}
