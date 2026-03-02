using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotli.Core
{
    /// <summary>
    /// Interface that all game states must implement.
    /// The GameStateManager delegates Update/Draw to the current state.
    /// </summary>
    public interface IGameState
    {
        void Enter();
        void Update(GameTime gameTime);
        void Draw(SpriteBatch spriteBatch);
        void Exit();
    }

    /// <summary>
    /// Manages game state transitions. Only one state is active at a time.
    /// </summary>
    public class GameStateManager
    {
        private IGameState? _currentState;

        public IGameState? CurrentState => _currentState;

        public void SetState(IGameState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        public void Update(GameTime gameTime)
        {
            _currentState?.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _currentState?.Draw(spriteBatch);
        }
    }
}
