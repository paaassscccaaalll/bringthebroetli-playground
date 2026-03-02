namespace BringTheBrotli.Players
{
    /// <summary>
    /// Possible vote choices at a station stop.
    /// </summary>
    public enum VoteChoice
    {
        Trust,
        Eject
    }

    /// <summary>
    /// Manages the simultaneous secret vote at station stops.
    /// Each player selects Trust or Eject; votes are revealed together.
    /// </summary>
    public class VoteManager
    {
        public VoteChoice Player1Selection { get; set; } = VoteChoice.Trust;
        public VoteChoice Player2Selection { get; set; } = VoteChoice.Trust;
        public bool Player1Confirmed { get; set; }
        public bool Player2Confirmed { get; set; }

        public bool BothVoted => Player1Confirmed && Player2Confirmed;

        public void Reset()
        {
            Player1Selection = VoteChoice.Trust;
            Player2Selection = VoteChoice.Trust;
            Player1Confirmed = false;
            Player2Confirmed = false;
        }

        /// <summary>
        /// Resolves the vote outcome.
        /// Returns (ejectionHappened, ejectedPlayerIndex).
        /// ejectedPlayerIndex is the OTHER player (the one being voted against).
        /// </summary>
        public (bool ejectionHappened, int ejectedPlayerIndex) Resolve(Player player1, Player player2, bool hasImposter)
        {
            // Imposter's vote is silently ignored.
            bool loyalPlayer1VotesEject = player1.Role == PlayerRole.LoyalCitizen && Player1Selection == VoteChoice.Eject;
            bool loyalPlayer2VotesEject = player2.Role == PlayerRole.LoyalCitizen && Player2Selection == VoteChoice.Eject;

            // If no loyal citizen votes to eject, journey continues
            if (!loyalPlayer1VotesEject && !loyalPlayer2VotesEject)
                return (false, -1);

            // Determine who gets ejected: the partner of whoever voted eject.
            // If Player 1 (loyal) votes eject → eject Player 2
            // If Player 2 (loyal) votes eject → eject Player 1
            // If both loyal players vote eject, eject both (use first ejection)
            if (loyalPlayer1VotesEject)
                return (true, 1); // Eject Player 2

            return (true, 0); // Eject Player 1
        }
    }
}
