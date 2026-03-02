using BringTheBrotli.Core;
using BringTheBrotli.Train;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BringTheBrotli.GameStates
{
    /// <summary>
    /// Game over screen. Shows the result, final stats, and option to return to menu.
    /// </summary>
    public class GameOverState : IGameState
    {
        private readonly GameStateManager _stateManager;
        private readonly InputManager _input;
        private readonly TextRenderer _text;
        private readonly TrainSystem _train;
        private readonly string _reason;
        private readonly bool _citizensWin;
        private readonly System.Func<IGameState> _createMenuState;

        private float _animTimer;

        public GameOverState(GameStateManager stateManager, InputManager input, TextRenderer text,
                              TrainSystem train, string reason, bool citizensWin,
                              System.Func<IGameState> createMenuState)
        {
            _stateManager = stateManager;
            _input = input;
            _text = text;
            _train = train;
            _reason = reason;
            _citizensWin = citizensWin;
            _createMenuState = createMenuState;
        }

        public void Enter()
        {
            _animTimer = 0f;
        }

        public void Update(GameTime gameTime)
        {
            _animTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_input.IsKeyJustPressed(Keys.Space) || _input.IsKeyJustPressed(Keys.Enter))
            {
                // Reset train for next game
                _train.Reset();
                _stateManager.SetState(_createMenuState());
            }
        }

        public void Draw(SpriteBatch sb)
        {
            Color bgColor = _citizensWin ? new Color(10, 30, 10) : new Color(40, 10, 10);
            _text.DrawRect(sb, new Rectangle(0, 0, 1280, 720), bgColor);

            // Victory/Defeat banner
            if (_citizensWin)
            {
                _text.DrawStringCentered(sb, "=== VICTORY ===", 80, Color.Gold, 2.5f);
                _text.DrawStringCentered(sb, "The Broetli have been delivered!", 160, Color.LimeGreen, 1.2f);
            }
            else
            {
                _text.DrawStringCentered(sb, "=== DEFEAT ===", 80, Color.Red, 2.5f);
                _text.DrawStringCentered(sb, "The mission has failed...", 160, Color.IndianRed, 1.2f);
            }

            // Reason (may contain newlines — split them)
            string[] lines = _reason.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                _text.DrawStringCentered(sb, lines[i], 240 + i * 30, Color.White, 1.0f);
            }

            // Stats
            int statsY = 360;
            _text.DrawStringCentered(sb, "--- FINAL STATS ---", statsY, Color.DarkGoldenrod, 1.1f);

            _text.DrawString(sb, $"Distance Traveled: {_train.DistanceTraveled:F1} / {TrainSystem.TotalDistance} km",
                new Vector2(400, statsY + 40), Color.White);
            _text.DrawString(sb, $"Time Elapsed: {_train.TimeElapsed:F0} / {TrainSystem.TimeLimit} seconds",
                new Vector2(400, statsY + 65), Color.White);
            _text.DrawString(sb, $"Final Speed: {_train.Speed:F0} km/h",
                new Vector2(400, statsY + 90), Color.White);
            _text.DrawString(sb, $"Final Coal: {_train.Firebox.CoalLevel:F0}%",
                new Vector2(400, statsY + 115), Color.White);
            _text.DrawString(sb, $"Final Water: {_train.Boiler.WaterLevel:F0}%",
                new Vector2(400, statsY + 140), Color.White);
            _text.DrawString(sb, $"Final Pressure: {_train.Boiler.SteamPressure:F0}%",
                new Vector2(400, statsY + 165), Color.White);

            // Return prompt
            float alpha = 0.5f + 0.5f * System.MathF.Sin(_animTimer * 3f);
            Color promptColor = Color.Lerp(Color.Transparent, Color.White, alpha);
            _text.DrawStringCentered(sb, "Press SPACE or ENTER to return to menu", 620, promptColor, 1.1f);
        }

        public void Exit() { }
    }
}
