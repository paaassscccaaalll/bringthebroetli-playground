using System;
using BringTheBrotli.Core;
using BringTheBrotli.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BringTheBrotli.GameStates
{
    /// <summary>
    /// Main menu state. Shows title, tagline, and starts the role reveal sequence.
    /// After both players see their roles, transitions to PlayingState.
    /// </summary>
    public class MainMenuState : IGameState
    {
        private readonly GameStateManager _stateManager;
        private readonly InputManager _input;
        private readonly TextRenderer _text;
        private readonly Player _player1;
        private readonly Player _player2;
        private readonly Func<IGameState> _createPlayingState;

        private enum Phase
        {
            TitleScreen,
            Player1RevealPrompt,
            Player1ShowRole,
            Player2RevealPrompt,
            Player2ShowRole,
            ReadyToBoth
        }

        private Phase _phase;
        private float _roleShowTimer;
        private const float RoleShowDuration = 3f; // seconds to show role
        private bool _hasImposter;
        private float _titlePulse;

        private static readonly Random _rng = new();

        public MainMenuState(GameStateManager stateManager, InputManager input, TextRenderer text,
                              Player player1, Player player2, Func<IGameState> createPlayingState)
        {
            _stateManager = stateManager;
            _input = input;
            _text = text;
            _player1 = player1;
            _player2 = player2;
            _createPlayingState = createPlayingState;
        }

        public void Enter()
        {
            _phase = Phase.TitleScreen;
            _roleShowTimer = 0f;
            _titlePulse = 0f;
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _titlePulse += dt;

            switch (_phase)
            {
                case Phase.TitleScreen:
                    if (_input.IsKeyJustPressed(Keys.Space) || _input.IsKeyJustPressed(Keys.Enter))
                    {
                        AssignRoles();
                        _phase = Phase.Player1RevealPrompt;
                    }
                    break;

                case Phase.Player1RevealPrompt:
                    if (_input.IsKeyJustPressed(Keys.Space))
                    {
                        _phase = Phase.Player1ShowRole;
                        _roleShowTimer = 0f;
                    }
                    break;

                case Phase.Player1ShowRole:
                    _roleShowTimer += dt;
                    if (_roleShowTimer >= RoleShowDuration)
                    {
                        _phase = Phase.Player2RevealPrompt;
                    }
                    break;

                case Phase.Player2RevealPrompt:
                    if (_input.IsKeyJustPressed(Keys.Enter))
                    {
                        _phase = Phase.Player2ShowRole;
                        _roleShowTimer = 0f;
                    }
                    break;

                case Phase.Player2ShowRole:
                    _roleShowTimer += dt;
                    if (_roleShowTimer >= RoleShowDuration)
                    {
                        _phase = Phase.ReadyToBoth;
                    }
                    break;

                case Phase.ReadyToBoth:
                    if (_input.IsKeyDown(Keys.Space) && _input.IsKeyDown(Keys.Enter))
                    {
                        _stateManager.SetState(_createPlayingState());
                    }
                    break;
            }
        }

        public void Draw(SpriteBatch sb)
        {
            switch (_phase)
            {
                case Phase.TitleScreen:
                    DrawTitleScreen(sb);
                    break;
                case Phase.Player1RevealPrompt:
                    DrawRevealPrompt(sb, 1, "Press SPACE to see your role.");
                    break;
                case Phase.Player1ShowRole:
                    DrawRoleReveal(sb, _player1, 1);
                    break;
                case Phase.Player2RevealPrompt:
                    DrawRevealPrompt(sb, 2, "Press ENTER to see your role.");
                    break;
                case Phase.Player2ShowRole:
                    DrawRoleReveal(sb, _player2, 2);
                    break;
                case Phase.ReadyToBoth:
                    DrawReadyScreen(sb);
                    break;
            }
        }

        public void Exit() { }

        // ── Private Methods ──

        private void AssignRoles()
        {
            // 50% chance of 1 Imposter
            _hasImposter = _rng.NextDouble() < 0.5;

            _player1.Role = PlayerRole.LoyalCitizen;
            _player2.Role = PlayerRole.LoyalCitizen;

            if (_hasImposter)
            {
                // Randomly assign imposter to one player
                if (_rng.NextDouble() < 0.5)
                    _player1.Role = PlayerRole.Imposter;
                else
                    _player2.Role = PlayerRole.Imposter;
            }
        }

        private void DrawTitleScreen(SpriteBatch sb)
        {
            // Animated pulse for title
            float pulse = 1.0f + 0.05f * MathF.Sin(_titlePulse * 2f);

            _text.DrawRect(sb, new Rectangle(0, 0, 1280, 720), new Color(20, 15, 10));

            _text.DrawStringCentered(sb, "BRING THE BROETLI", 150, Color.Gold, 2.5f * pulse);
            _text.DrawStringCentered(sb, "The Spanisch-Broetli-Bahn, 1850", 250, Color.BurlyWood, 1.0f);
            _text.DrawStringCentered(sb, "A 2-Player Couch Co-op Social Deduction Game", 290, Color.Wheat, 0.9f);

            _text.DrawStringCentered(sb, "\"Play well, but not too well.\"", 380, Color.IndianRed, 1.1f);

            _text.DrawStringCentered(sb, "Player 1: W/S/A/D + Space", 470, Color.Orange);
            _text.DrawStringCentered(sb, "Player 2: Arrow Keys + Enter", 500, Color.CornflowerBlue);

            float alpha = 0.5f + 0.5f * MathF.Sin(_titlePulse * 3f);
            Color startColor = Color.Lerp(Color.Transparent, Color.White, alpha);
            _text.DrawStringCentered(sb, "Press SPACE or ENTER to start", 580, startColor, 1.2f);

            _text.DrawStringCentered(sb, "Prototype v0.1", 680, Color.DarkGray, 0.8f);
        }

        private void DrawRevealPrompt(SpriteBatch sb, int playerNum, string instruction)
        {
            _text.DrawRect(sb, new Rectangle(0, 0, 1280, 720), new Color(10, 10, 20));

            string otherPlayer = playerNum == 1 ? "Player 2" : "Player 1";
            _text.DrawStringCentered(sb, $"PLAYER {playerNum}", 200, Color.Gold, 2.0f);
            _text.DrawStringCentered(sb, $"Look at the screen. {otherPlayer}, look away!", 300, Color.White, 1.1f);
            _text.DrawStringCentered(sb, instruction, 400, Color.Yellow, 1.2f);
        }

        private void DrawRoleReveal(SpriteBatch sb, Player player, int playerNum)
        {
            _text.DrawRect(sb, new Rectangle(0, 0, 1280, 720), new Color(10, 10, 20));

            Color roleColor = player.Role == PlayerRole.Imposter ? Color.Red : Color.LimeGreen;
            _text.DrawStringCentered(sb, $"PLAYER {playerNum} - YOUR ROLE:", 180, Color.White, 1.2f);
            _text.DrawStringCentered(sb, player.RoleName, 260, roleColor, 2.0f);

            if (player.Role == PlayerRole.Imposter)
            {
                _text.DrawStringCentered(sb, "Sabotage the train. Be subtle.", 360, Color.IndianRed, 1.0f);
                _text.DrawStringCentered(sb, "Let the revolution begin.", 390, Color.IndianRed, 1.0f);
            }
            else
            {
                _text.DrawStringCentered(sb, "Keep the train running. Deliver the Broetli.", 360, Color.LightGreen, 1.0f);
                _text.DrawStringCentered(sb, "Trust no one.", 390, Color.LightGreen, 1.0f);
            }

            float remaining = RoleShowDuration - _roleShowTimer;
            _text.DrawStringCentered(sb, $"Auto-hiding in {remaining:F1}s...", 500, Color.Gray);
        }

        private void DrawReadyScreen(SpriteBatch sb)
        {
            _text.DrawRect(sb, new Rectangle(0, 0, 1280, 720), new Color(10, 10, 20));
            _text.DrawStringCentered(sb, "Both players ready.", 250, Color.White, 1.5f);
            _text.DrawStringCentered(sb, "The journey begins...", 320, Color.Gold, 1.2f);
            _text.DrawStringCentered(sb, "Hold SPACE + ENTER together to depart!", 420, Color.Yellow, 1.1f);
        }
    }
}
