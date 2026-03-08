using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotliDemo
{
    public class DebugOverlay
    {
        private readonly CollisionSystem _collision;

        public DebugOverlay(CollisionSystem collision)
        {
            _collision = collision;
        }

        public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFont font,
                         PlayerCharacter[] players)
        {
            var boundary = _collision.SurfaceBoundary;
            if (boundary.Length >= 3)
            {
                for (int i = 0; i < boundary.Length; i++)
                {
                    int j = (i + 1) % boundary.Length;
                    DrawHelpers.DrawLine(sb, pixel, boundary[i], boundary[j], Color.Lime, 1);
                }
            }

            for (int i = 0; i < _collision.Obstacles.Length; i++)
                DrawHelpers.DrawRectOutline(sb, pixel, _collision.Obstacles[i].Bounds, Color.Yellow, 1);

            for (int i = 0; i < _collision.JumpBarriers.Length; i++)
                DrawHelpers.DrawRectOutline(sb, pixel, _collision.JumpBarriers[i], Color.Cyan, 2);

            foreach (var player in players)
            {
                Color dotColor = player.PlayerIndex == 0 ? Color.Red : Color.Blue;

                sb.Draw(pixel,
                    new Rectangle((int)player.Position.X - 2, (int)player.Position.Y - 2, 4, 4),
                    dotColor);

                if (player.JumpState == JumpState.Rising || player.JumpState == JumpState.Falling)
                {
                    Vector2 lp = player.PredictedLanding;
                    sb.Draw(pixel,
                        new Rectangle((int)lp.X - 3, (int)lp.Y - 3, 6, 6),
                        Color.Magenta);
                }

                if (player.JumpState != JumpState.Grounded && player.JumpState != JumpState.Landing)
                {
                    sb.Draw(pixel,
                        new Rectangle((int)player.Position.X - 8, (int)player.Position.Y - 1, 16, 2),
                        new Color(0, 0, 0, 100));
                }
            }

            string text = $"P1: {players[0].JumpState}  H:{players[0].CurrentJumpHeight:F1}";
            if (players.Length > 1)
                text += $"  |  P2: {players[1].JumpState}  H:{players[1].CurrentJumpHeight:F1}";
            sb.DrawString(font, text, new Vector2(10, 10), Color.White);
        }
    }
}
