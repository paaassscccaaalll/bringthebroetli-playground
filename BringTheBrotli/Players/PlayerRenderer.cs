using System;
using BringTheBrotli.Core;
using BringTheBrotli.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotli.Players
{
    /// <summary>
    /// Draws animated player characters on the train roof using spritesheet textures.
    /// Player 1 uses player1.png, Player 2 uses player2.png.
    /// 4 frames per sheet (32x48 each): idle, walk-left, idle, walk-right.
    /// Depth sorted so the player further right renders first.
    /// </summary>
    public class PlayerRenderer
    {
        private readonly TextRenderer _text;

        // Animation timing
        private float _animTimer;
        private const float FrameDuration = 0.15f; // seconds per frame

        public PlayerRenderer(TextRenderer text)
        {
            _text = text;
        }

        /// <summary>Advance animation timer.</summary>
        public void Update(float dt)
        {
            _animTimer += dt;
        }

        /// <summary>
        /// Draw both player characters. Sorts by WorldX so the player further right
        /// renders first for correct layering.
        /// </summary>
        public void Draw(SpriteBatch sb, Camera camera, PlayerCharacter p1, PlayerCharacter p2)
        {
            if (p1.WorldX > p2.WorldX)
            {
                DrawCharacter(sb, camera, p1);
                DrawCharacter(sb, camera, p2);
            }
            else
            {
                DrawCharacter(sb, camera, p2);
                DrawCharacter(sb, camera, p1);
            }
        }

        private void DrawCharacter(SpriteBatch sb, Camera camera, PlayerCharacter pc)
        {
            float screenX = camera.WorldToScreenX(pc.WorldX);
            float feetY = pc.FeetY;
            bool isP1 = pc.PlayerIndex == 0;

            var texture = isP1 ? TextureAtlas.Player1Sheet : TextureAtlas.Player2Sheet;

            // Determine animation frame
            int frame;
            if (MathF.Abs(pc.VelocityX) > 10f)
            {
                int animIndex = (int)(_animTimer / FrameDuration) % TextureAtlas.PlayerFrameCount;
                frame = animIndex;
            }
            else
            {
                frame = 0;
            }

            var sourceRect = TextureAtlas.GetPlayerFrame(frame);

            // Scale: 1.2x gives ~38x58 which fits on the roof
            int drawW = (int)(TextureAtlas.PlayerFrameWidth * 1.2f);
            int drawH = (int)(TextureAtlas.PlayerFrameHeight * 1.2f);

            // Position: anchor at feet
            int dx = (int)(screenX - drawW / 2f);
            int dy = (int)(feetY - drawH);

            // Flip based on facing direction
            SpriteEffects flip = pc.FacingDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // ── Drop shadow on the top face ──
            int shadowW = drawW - 4;
            int shadowH = 6;
            int shadowX = dx + 3;
            int shadowY = (int)(feetY - 2);
            _text.DrawRect(sb, new Rectangle(shadowX + 3, shadowY, shadowW - 6, shadowH),
                new Color(0, 0, 0, 40));
            _text.DrawRect(sb, new Rectangle(shadowX, shadowY + 2, shadowW, shadowH - 4),
                new Color(0, 0, 0, 25));

            // ── Player sprite ──
            sb.Draw(texture,
                new Rectangle(dx, dy, drawW, drawH),
                sourceRect,
                Color.White,
                0f, Vector2.Zero, flip, 0f);

            // ── Voxel-style bold outline around player (2px dark border) ──
            var olc = new Color(25, 25, 30, 200);
            _text.DrawRect(sb, new Rectangle(dx, dy, drawW, 2), olc);             // top
            _text.DrawRect(sb, new Rectangle(dx, dy + drawH - 2, drawW, 2), olc); // bottom
            _text.DrawRect(sb, new Rectangle(dx, dy, 2, drawH), olc);             // left
            _text.DrawRect(sb, new Rectangle(dx + drawW - 2, dy, 2, drawH), olc); // right

            // Player number label above head
            string label = isP1 ? "P1" : "P2";
            Color labelColor = isP1 ? Color.CornflowerBlue : Color.Salmon;
            // Label with shadow
            _text.DrawString(sb, label, new Vector2(dx + drawW / 2 - 7, dy - 15), new Color(0, 0, 0, 150), 0.7f);
            _text.DrawString(sb, label, new Vector2(dx + drawW / 2 - 8, dy - 16), labelColor, 0.7f);
        }
    }
}
