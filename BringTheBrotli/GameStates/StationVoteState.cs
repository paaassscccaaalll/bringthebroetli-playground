using System;
using BringTheBrotli.Core;
using BringTheBrotli.Players;
using BringTheBrotli.Train;
using BringTheBrotli.UI;
using BringTheBrotli.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BringTheBrotli.GameStates
{
    /// <summary>
    /// Station vote state. Pauses the train and lets both players vote
    /// to trust or eject their partner. Votes are revealed simultaneously.
    /// </summary>
    public class StationVoteState : IGameState
    {
        private readonly GameStateManager _stateManager;
        private readonly InputManager _input;
        private readonly TextRenderer _text;
        private readonly Player _player1;
        private readonly Player _player2;
        private readonly VoteManager _votes;
        private readonly VoteScreen _voteScreen;
        private readonly TrainSystem _train;
        private readonly TrackScroller _scroller;
        private readonly HUD _hud;
        private readonly int _stationNumber;

        // Factories
        private readonly Func<IGameState> _createPlayingState;
        private readonly Func<TrainSystem, string, bool, IGameState> _createGameOverState;

        private bool _revealed;
        private float _revealTimer;
        private const float RevealDuration = 4f; // seconds to show result before transitioning

        // Result tracking
        private bool _ejectionHappened;
        private int _ejectedPlayerIndex;

        public StationVoteState(GameStateManager stateManager, InputManager input, TextRenderer text,
                                 Player player1, Player player2, VoteManager votes,
                                 VoteScreen voteScreen, TrainSystem train, TrackScroller scroller, HUD hud,
                                 int stationNumber,
                                 Func<IGameState> createPlayingState,
                                 Func<TrainSystem, string, bool, IGameState> createGameOverState)
        {
            _stateManager = stateManager;
            _input = input;
            _text = text;
            _player1 = player1;
            _player2 = player2;
            _votes = votes;
            _voteScreen = voteScreen;
            _train = train;
            _scroller = scroller;
            _hud = hud;
            _stationNumber = stationNumber;
            _createPlayingState = createPlayingState;
            _createGameOverState = createGameOverState;
        }

        public void Enter()
        {
            _votes.Reset();
            _revealed = false;
            _revealTimer = 0f;
            _ejectionHappened = false;
            _ejectedPlayerIndex = -1;
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (!_revealed)
            {
                // --- Player 1 voting (A/D + Space) ---
                if (!_votes.Player1Confirmed)
                {
                    if (_input.IsKeyJustPressed(Keys.A))
                        _votes.Player1Selection = VoteChoice.Trust;
                    if (_input.IsKeyJustPressed(Keys.D))
                        _votes.Player1Selection = VoteChoice.Eject;
                    if (_input.IsKeyJustPressed(Keys.Space))
                        _votes.Player1Confirmed = true;
                }

                // --- Player 2 voting (Left/Right + Enter) ---
                if (!_votes.Player2Confirmed)
                {
                    if (_input.IsKeyJustPressed(Keys.Left))
                        _votes.Player2Selection = VoteChoice.Trust;
                    if (_input.IsKeyJustPressed(Keys.Right))
                        _votes.Player2Selection = VoteChoice.Eject;
                    if (_input.IsKeyJustPressed(Keys.Enter))
                        _votes.Player2Confirmed = true;
                }

                // Both voted — reveal
                if (_votes.BothVoted)
                {
                    _revealed = true;
                    _revealTimer = 0f;

                    bool hasImposter = _player1.Role == PlayerRole.Imposter || _player2.Role == PlayerRole.Imposter;
                    (_ejectionHappened, _ejectedPlayerIndex) = _votes.Resolve(_player1, _player2, hasImposter);
                }
            }
            else
            {
                _revealTimer += dt;
                if (_revealTimer >= RevealDuration)
                {
                    ResolveAndTransition();
                }
            }
        }

        public void Draw(SpriteBatch sb)
        {
            _voteScreen.Draw(sb, _votes, _revealed, _stationNumber);

            if (_revealed)
            {
                // Draw outcome message
                string outcome;
                Color outcomeColor;

                if (!_ejectionHappened)
                {
                    outcome = "No ejection. The journey continues!";
                    outcomeColor = Color.LimeGreen;
                }
                else
                {
                    Player ejected = _ejectedPlayerIndex == 0 ? _player1 : _player2;
                    int ejectedNum = _ejectedPlayerIndex + 1;

                    if (ejected.Role == PlayerRole.Imposter)
                    {
                        outcome = $"Player {ejectedNum} was the COMMUNIST! Citizens win!";
                        outcomeColor = Color.Gold;
                    }
                    else
                    {
                        bool hasImposter = _player1.Role == PlayerRole.Imposter || _player2.Role == PlayerRole.Imposter;
                        if (hasImposter)
                        {
                            outcome = $"Player {ejectedNum} was LOYAL! The Imposter wins!";
                            outcomeColor = Color.Red;
                        }
                        else
                        {
                            outcome = $"Player {ejectedNum} was LOYAL! No imposter existed - both lose!";
                            outcomeColor = Color.Red;
                        }
                    }
                }

                _text.DrawStringCentered(sb, outcome, 350, outcomeColor, 1.2f);
                _text.DrawStringCentered(sb, $"Continuing in {(RevealDuration - _revealTimer):F1}s...", 420, Color.Gray);
            }
        }

        public void Exit() { }

        private void ResolveAndTransition()
        {
            if (!_ejectionHappened)
            {
                // Continue the journey
                _hud.AddEvent($"Station {_stationNumber}: No ejection. Moving on!");
                _stateManager.SetState(_createPlayingState());
                return;
            }

            Player ejected = _ejectedPlayerIndex == 0 ? _player1 : _player2;
            int ejectedNum = _ejectedPlayerIndex + 1;

            if (ejected.Role == PlayerRole.Imposter)
            {
                // Correct ejection — citizens win!
                _stateManager.SetState(_createGameOverState(_train,
                    $"Player {ejectedNum} was the Undercover Communist!\nThe saboteur was caught! Citizens win!",
                    true));
            }
            else
            {
                bool hasImposter = _player1.Role == PlayerRole.Imposter || _player2.Role == PlayerRole.Imposter;
                if (hasImposter)
                {
                    _stateManager.SetState(_createGameOverState(_train,
                        $"Player {ejectedNum} was a Loyal Citizen!\nThe wrong person was ejected! Imposter wins!",
                        false));
                }
                else
                {
                    _stateManager.SetState(_createGameOverState(_train,
                        $"Player {ejectedNum} was a Loyal Citizen!\nThere was no imposter - both citizens lose!",
                        false));
                }
            }
        }
    }
}
