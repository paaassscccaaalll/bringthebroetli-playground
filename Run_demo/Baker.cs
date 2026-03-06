using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BringTheBrotliDemo
{
    /// <summary>
    /// Represents the playable Baker character.
    ///
    /// Responsibilities
    /// ────────────────
    ///  • Read WASD keyboard input and compute a movement vector.
    ///  • Move the Baker's world position at a fixed speed.
    ///  • Delegate animation state changes to AnimationController.
    ///  • Draw the correct sprite-sheet frame, centered on the screen.
    /// </summary>
    public class Baker
    {
        // ---------------------------------------------------------------
        // Constants
        // ---------------------------------------------------------------

        /// <summary>Movement speed in world-units per second.</summary>
        public const float MoveSpeed = 200f;

        /// <summary>
        /// Y-axis compression factor matching the 2.5D camera tilt (45°).
        /// Applied to the floor; the Baker position moves at full speed in
        /// world space but the floor scrolls with this compression.
        /// </summary>
        public const float IsoYScale = 0.5f;

        // ---------------------------------------------------------------
        // Fields
        // ---------------------------------------------------------------

        /// <summary>World-space position of the Baker's centre.</summary>
        public Vector2 Position;

        private readonly AnimationController _anim;
        private readonly Texture2D           _spriteSheet;

        // Screen dimensions — used so the Baker is always centred.
        private readonly int _screenWidth;
        private readonly int _screenHeight;

        // ---------------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------------

        /// <param name="spriteSheet">The loaded Baker spritesheet texture.</param>
        /// <param name="anim">Pre-constructed AnimationController for this Baker.</param>
        /// <param name="screenWidth">Viewport width in pixels.</param>
        /// <param name="screenHeight">Viewport height in pixels.</param>
        public Baker(Texture2D spriteSheet, AnimationController anim,
                     int screenWidth, int screenHeight)
        {
            _spriteSheet  = spriteSheet;
            _anim         = anim;
            _screenWidth  = screenWidth;
            _screenHeight = screenHeight;

            // Start the Baker in the centre of the (conceptual) world.
            Position = Vector2.Zero;
        }

        // ---------------------------------------------------------------
        // Public properties
        // ---------------------------------------------------------------

        /// <summary>Expose the controller so Game1 can inspect debug info.</summary>
        public AnimationController Animation => _anim;

        // ---------------------------------------------------------------
        // Update
        // ---------------------------------------------------------------

        /// <summary>
        /// Process input, move the Baker, and advance the animation.
        /// Call once per game tick from Game1.Update.
        /// </summary>
        public void Update(GameTime gameTime, KeyboardState kb)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // ---- Build raw movement vector from WASD -----------------
            Vector2 rawDir = Vector2.Zero;

            if (kb.IsKeyDown(Keys.W)) rawDir.Y -= 1f;  // up-screen = north
            if (kb.IsKeyDown(Keys.S)) rawDir.Y += 1f;  // down
            if (kb.IsKeyDown(Keys.A)) rawDir.X -= 1f;  // left = west
            if (kb.IsKeyDown(Keys.D)) rawDir.X += 1f;  // right = east

            // Normalise diagonals so speed is consistent in all directions.
            if (rawDir != Vector2.Zero)
                rawDir = Vector2.Normalize(rawDir);

            // ---- Move the world position -----------------------------
            Position += rawDir * MoveSpeed * dt;

            // ---- Update animation state ------------------------------
            _anim.Update(gameTime, rawDir);
        }

        // ---------------------------------------------------------------
        // Draw
        // ---------------------------------------------------------------

        /// <summary>
        /// Draw the Baker centred on the screen.
        ///
        /// The Baker's world position scrolls the background but the
        /// character sprite always sits at the screen centre — this gives
        /// the classic 2D RPG feel without a separate camera class.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            Rectangle src = _anim.GetSourceRect();

            // The character is drawn centred on screen.
            // Place the sprite so its bottom-centre aligns with the screen
            // centre (feet on the ground line) for a natural 2.5D look.
            Vector2 screenCentre = new Vector2(
                _screenWidth  / 2f,
                _screenHeight / 2f
            );

            // Origin at bottom-centre of the frame.
            Vector2 origin = new Vector2(src.Width / 2f, src.Height);

            // Slight vertical offset so the feet sit on the ground line.
            Vector2 drawPos = new Vector2(
                screenCentre.X,
                screenCentre.Y + src.Height * 0.15f   // nudge down a bit
            );

            spriteBatch.Draw(
                texture:           _spriteSheet,
                position:          drawPos,
                sourceRectangle:   src,
                color:             Color.White,
                rotation:          0f,
                origin:            origin,
                scale:             1f,
                effects:           SpriteEffects.None,
                layerDepth:        0f
            );
        }
    }
}
