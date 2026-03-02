namespace BringTheBrotli.Players
{
    /// <summary>
    /// Holds per-player data: assigned role, current action state, and physical character.
    /// </summary>
    public class Player
    {
        public int PlayerIndex { get; }       // 0 = Player 1, 1 = Player 2
        public PlayerRole Role { get; set; }
        public string RoleName => Role == PlayerRole.Imposter ? "UNDERCOVER COMMUNIST" : "LOYAL CITIZEN";

        // Hold-to-act progress (0..1). When >= 1 the action fires.
        public float ActionProgress { get; set; }
        public bool IsPerformingAction { get; set; }

        /// <summary>Reference to the physical player character (set by PlayingState).</summary>
        public PlayerCharacter? Character { get; set; }

        public Player(int index)
        {
            PlayerIndex = index;
            Role = PlayerRole.LoyalCitizen;
            ActionProgress = 0f;
            IsPerformingAction = false;
        }

        /// <summary>
        /// Resets transient action state between rounds.
        /// </summary>
        public void ResetAction()
        {
            ActionProgress = 0f;
            IsPerformingAction = false;
        }
    }
}
