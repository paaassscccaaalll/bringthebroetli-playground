using Microsoft.Xna.Framework.Input;

namespace BringTheBrotliDemo
{
    public class PlayerInput
    {
        public Keys Up { get; }
        public Keys Down { get; }
        public Keys Left { get; }
        public Keys Right { get; }
        public Keys Run { get; }
        public Keys Jump { get; }
        public Keys Interact { get; }

        public PlayerInput(Keys up, Keys down, Keys left, Keys right,
                           Keys run, Keys jump, Keys interact)
        {
            Up = up;
            Down = down;
            Left = left;
            Right = right;
            Run = run;
            Jump = jump;
            Interact = interact;
        }

        public static PlayerInput Player1 => new(
            Keys.W, Keys.S, Keys.A, Keys.D,
            Keys.LeftShift, Keys.Space, Keys.E);

        public static PlayerInput Player2 => new(
            Keys.Up, Keys.Down, Keys.Left, Keys.Right,
            Keys.RightShift, Keys.Enter, Keys.RightControl);
    }
}
