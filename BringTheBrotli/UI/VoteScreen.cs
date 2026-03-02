using BringTheBrotli.Core;
using BringTheBrotli.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotli.UI
{
    /// <summary>
    /// Draws the station vote UI — split screen with each player's vote area.
    /// </summary>
    public class VoteScreen
    {
        private readonly TextRenderer _text;

        public VoteScreen(TextRenderer text)
        {
            _text = text;
        }

        public void Draw(SpriteBatch sb, VoteManager votes, bool revealed, int stationNumber)
        {
            // Background overlay
            _text.DrawRect(sb, new Rectangle(0, 0, 1280, 720), new Color(0, 0, 0, 200));

            // Title
            _text.DrawStringCentered(sb, $"=== STATION {stationNumber} - DO YOU TRUST YOUR PARTNER? ===", 40, Color.Gold, 1.2f);

            if (!revealed)
            {
                // Player 1 vote area (left)
                DrawVotePanel(sb, 40, 120, 560, 400,
                    "PLAYER 1", votes.Player1Selection, votes.Player1Confirmed,
                    "[A/D] to choose, [SPACE] to confirm", Color.Orange);

                // Player 2 vote area (right)
                DrawVotePanel(sb, 680, 120, 560, 400,
                    "PLAYER 2", votes.Player2Selection, votes.Player2Confirmed,
                    "[Left/Right] to choose, [ENTER] to confirm", Color.CornflowerBlue);

                if (votes.Player1Confirmed && !votes.Player2Confirmed)
                    _text.DrawStringCentered(sb, "Waiting for Player 2...", 550, Color.Gray);
                else if (!votes.Player1Confirmed && votes.Player2Confirmed)
                    _text.DrawStringCentered(sb, "Waiting for Player 1...", 550, Color.Gray);
                else if (!votes.Player1Confirmed && !votes.Player2Confirmed)
                    _text.DrawStringCentered(sb, "Both players must vote!", 550, Color.Gray);
            }
            else
            {
                // Reveal votes
                _text.DrawStringCentered(sb, "VOTES REVEALED!", 140, Color.White, 1.5f);

                string p1Vote = votes.Player1Selection == VoteChoice.Trust ? "TRUST" : "EJECT";
                string p2Vote = votes.Player2Selection == VoteChoice.Trust ? "TRUST" : "EJECT";
                Color p1Color = votes.Player1Selection == VoteChoice.Trust ? Color.LimeGreen : Color.Red;
                Color p2Color = votes.Player2Selection == VoteChoice.Trust ? Color.LimeGreen : Color.Red;

                _text.DrawString(sb, $"Player 1 voted: {p1Vote}", new Vector2(200, 240), p1Color, 1.3f);
                _text.DrawString(sb, $"Player 2 voted: {p2Vote}", new Vector2(700, 240), p2Color, 1.3f);
            }
        }

        private void DrawVotePanel(SpriteBatch sb, int x, int y, int w, int h,
                                    string title, VoteChoice selection, bool confirmed,
                                    string instructions, Color accentColor)
        {
            _text.DrawRect(sb, new Rectangle(x, y, w, h), new Color(20, 20, 40));
            _text.DrawRectBorder(sb, new Rectangle(x, y, w, h), accentColor);

            _text.DrawString(sb, title, new Vector2(x + 20, y + 15), accentColor, 1.2f);
            _text.DrawString(sb, instructions, new Vector2(x + 20, y + 50), Color.Gray);

            // Vote options
            int optY = y + 120;
            int optW = 200;
            int optH = 60;

            // TRUST button
            bool trustSelected = selection == VoteChoice.Trust;
            Color trustBg = trustSelected ? new Color(0, 100, 0) : new Color(40, 40, 40);
            Color trustBorder = trustSelected ? Color.LimeGreen : Color.Gray;
            _text.DrawRect(sb, new Rectangle(x + 40, optY, optW, optH), trustBg);
            _text.DrawRectBorder(sb, new Rectangle(x + 40, optY, optW, optH), trustBorder, 3);
            _text.DrawString(sb, "TRUST", new Vector2(x + 90, optY + 18), trustSelected ? Color.White : Color.Gray, 1.2f);

            // EJECT button
            bool ejectSelected = selection == VoteChoice.Eject;
            Color ejectBg = ejectSelected ? new Color(100, 0, 0) : new Color(40, 40, 40);
            Color ejectBorder = ejectSelected ? Color.Red : Color.Gray;
            _text.DrawRect(sb, new Rectangle(x + 300, optY, optW, optH), ejectBg);
            _text.DrawRectBorder(sb, new Rectangle(x + 300, optY, optW, optH), ejectBorder, 3);
            _text.DrawString(sb, "EJECT", new Vector2(x + 350, optY + 18), ejectSelected ? Color.White : Color.Gray, 1.2f);

            // Confirmed indicator
            if (confirmed)
            {
                _text.DrawString(sb, "VOTE LOCKED IN!", new Vector2(x + 150, optY + 100), Color.Gold);
            }
        }
    }
}
