using Microsoft.Xna.Framework;
using System;

namespace BringTheBrotliDemo
{
    /// <summary>
    /// Manages which frame and which angle row is currently displayed for the
    /// Baker character.  All sprite-sheet knowledge (frame size, row layout) is
    /// kept here so Baker.cs stays thin.
    /// </summary>
    public class AnimationController
    {
        // ---------------------------------------------------------------
        // Configuration
        // ---------------------------------------------------------------

        /// <summary>Playback speed in frames-per-second.</summary>
        public const float AnimationFps = 10f;

        /// <summary>
        /// Maps a movement angle (degrees, 0 = East, counter-clockwise)
        /// to the sprite-sheet row that shows the correct character facing.
        ///
        /// Blender render convention used here:
        ///   Moving North (up-screen)  → character's back → row for Angle_180
        ///   Moving South (down-screen)→ character's front→ row for Angle_0
        ///   Moving East  (right)      → Angle_270
        ///   Moving West  (left)       → Angle_90
        ///   … diagonals map to 45 / 135 / 225 / 315
        ///
        /// The array index is the row in the spritesheet (same order as the
        /// JSON angleOrder list: [0, 45, 90, 135, 180, 225, 270, 315]).
        /// </summary>
        private static readonly int[] AngleOrder = { 0, 45, 90, 135, 180, 225, 270, 315 };

        // ---------------------------------------------------------------
        // Public properties (read-only for outside code)
        // ---------------------------------------------------------------

        public int FrameWidth       { get; private set; }
        public int FrameHeight      { get; private set; }
        public int FramesPerAngle   { get; private set; }
        public int TotalAngles      { get; private set; }

        /// <summary>The sprite-sheet angle (degrees) currently displayed.</summary>
        public int  CurrentAngle { get; private set; } = 0;

        /// <summary>Zero-based frame index within the angle row.</summary>
        public int  CurrentFrame { get; private set; } = 0;

        // ---------------------------------------------------------------
        // Private state
        // ---------------------------------------------------------------

        private float _frameTimer;          // seconds accumulated since last frame advance
        private bool  _isWalking;

        // ---------------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------------

        /// <summary>
        /// Create an AnimationController using the dimensions loaded from the
        /// spritesheet metadata JSON.
        /// </summary>
        public AnimationController(int frameWidth, int frameHeight,
                                   int framesPerAngle, int totalAngles)
        {
            FrameWidth      = frameWidth;
            FrameHeight     = frameHeight;
            FramesPerAngle  = framesPerAngle;
            TotalAngles     = totalAngles;
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>
        /// Advance the animation state each game tick.
        /// </summary>
        /// <param name="gameTime">Current game time (for delta-time).</param>
        /// <param name="movementDirection">
        ///   Normalised (or raw) 2-D movement vector in screen space.
        ///   (0,0) means idle; any non-zero value means walking.
        ///   X positive = right, Y positive = down (MonoGame screen space).
        /// </param>
        public void Update(GameTime gameTime, Vector2 movementDirection)
        {
            _isWalking = movementDirection != Vector2.Zero;

            // ---- Direction → angle row --------------------------------
            if (_isWalking)
            {
                int targetAngle = DirectionToAngle(movementDirection);
                CurrentAngle    = targetAngle;
            }

            // ---- Frame advancement ------------------------------------
            if (_isWalking)
            {
                _frameTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                float frameDuration = 1f / AnimationFps;

                if (_frameTimer >= frameDuration)
                {
                    _frameTimer -= frameDuration;
                    CurrentFrame = (CurrentFrame + 1) % FramesPerAngle;
                }
            }
            else
            {
                // Idle: hold on frame 0 of the last used angle.
                CurrentFrame = 0;
                _frameTimer  = 0f;
            }
        }

        /// <summary>
        /// Returns the Rectangle that clips the current frame from the spritesheet.
        /// Pass this directly to SpriteBatch.Draw as the sourceRectangle.
        /// </summary>
        public Rectangle GetSourceRect()
        {
            int row = AngleToRow(CurrentAngle);
            int x   = CurrentFrame * FrameWidth;
            int y   = row          * FrameHeight;
            return new Rectangle(x, y, FrameWidth, FrameHeight);
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Convert a 2-D movement direction to the nearest of the 8 sprite angles.
        ///
        /// Coordinate convention (MonoGame screen space):
        ///   +X = right,  +Y = down
        ///
        /// We map to "world compass headings" used when rendering:
        ///   Up-screen   (north) →  Angle_180  (back of Baker faces camera)
        ///   Down-screen (south) →  Angle_0
        ///   Right       (east)  →  Angle_270
        ///   Left        (west)  →  Angle_90
        /// </summary>
        private static int DirectionToAngle(Vector2 dir)
        {
            // atan2 in standard math: 0 = East, +CCW
            // In screen space Y is flipped, so we negate Y before atan2.
            double radians = Math.Atan2(-dir.Y, dir.X);   // standard math angle
            double degrees = radians * (180.0 / Math.PI); // -180 … +180

            // Normalise to 0..360
            if (degrees < 0) degrees += 360.0;

            // 'degrees' is now the standard math angle (0 = East, CCW).
            // Map to our sprite-sheet angle convention:
            //
            //   Standard 0°  (East)  → moving right  → Angle_270
            //   Standard 90° (North, up-screen) → moving up → Angle_180
            //   Standard 180°(West)  → moving left   → Angle_90
            //   Standard 270°(South, down-screen) → moving down → Angle_0
            //
            // Formula: spriteAngle = (270 - standardDegrees + 360) % 360
            double spriteAngle = (270.0 - degrees + 360.0) % 360.0;

            // Snap to nearest of the 8 defined angles.
            int best       = 0;
            double bestDiff = double.MaxValue;

            foreach (int candidate in AngleOrder)
            {
                double diff = Math.Abs(AngleDiff(spriteAngle, candidate));
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best     = candidate;
                }
            }
            return best;
        }

        /// <summary>Signed difference between two angles (degrees), result in -180..180.</summary>
        private static double AngleDiff(double a, double b)
        {
            double d = (a - b + 360.0) % 360.0;
            return d > 180.0 ? d - 360.0 : d;
        }

        /// <summary>Return the sprite-sheet row index for a given angle value.</summary>
        private static int AngleToRow(int angle)
        {
            for (int i = 0; i < AngleOrder.Length; i++)
                if (AngleOrder[i] == angle)
                    return i;
            return 0; // fallback
        }
    }
}
