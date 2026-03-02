using Microsoft.Xna.Framework.Input;

namespace BringTheBrotli.Core
{
    /// <summary>
    /// Centralized keyboard input manager.
    /// Tracks current and previous frame states for edge detection.
    /// All input queries go through this — never call Keyboard.GetState() elsewhere.
    /// </summary>
    public class InputManager
    {
        private KeyboardState _currentState;
        private KeyboardState _previousState;

        public void Update()
        {
            _previousState = _currentState;
            _currentState = Keyboard.GetState();
        }

        /// <summary>Returns true while the key is held down.</summary>
        public bool IsKeyDown(Keys key) => _currentState.IsKeyDown(key);

        /// <summary>Returns true the first frame the key is pressed.</summary>
        public bool IsKeyJustPressed(Keys key) =>
            _currentState.IsKeyDown(key) && !_previousState.IsKeyDown(key);

        /// <summary>Returns true the first frame the key is released.</summary>
        public bool IsKeyJustReleased(Keys key) =>
            !_currentState.IsKeyDown(key) && _previousState.IsKeyDown(key);
    }
}
