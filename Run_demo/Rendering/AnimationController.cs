using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace BringTheBrotliDemo
{
    public enum AnimationType { Walk, Jump }

    /// <summary>
    /// Manages which frame and which angle row is currently displayed for the
    /// Baker character.  Supports two spritesheets (Walk and Jump) with
    /// potentially different frame counts.
    /// </summary>
    public class AnimationController
    {
        // ---------------------------------------------------------------
        // Configuration
        // ---------------------------------------------------------------

        public const float DefaultAnimationFps = 10f;

        public float TargetFps { get; set; } = DefaultAnimationFps;

        private static readonly int[] AngleOrder = { 0, 45, 90, 135, 180, 225, 270, 315 };

        // ---------------------------------------------------------------
        // Spritesheet data (walk + jump)
        // ---------------------------------------------------------------

        private readonly Texture2D _walkTexture;
        private readonly Texture2D _jumpTexture;

        private readonly int _walkFrameWidth,  _walkFrameHeight,  _walkFramesPerAngle;
        private readonly int _jumpFrameWidth,  _jumpFrameHeight,  _jumpFramesPerAngle;

        // ---------------------------------------------------------------
        // Public properties
        // ---------------------------------------------------------------

        /// <summary>Frame dimensions for the currently active animation.</summary>
        public int FrameWidth  => _activeType == AnimationType.Walk ? _walkFrameWidth  : _jumpFrameWidth;
        public int FrameHeight => _activeType == AnimationType.Walk ? _walkFrameHeight : _jumpFrameHeight;
        public int FramesPerAngle => _activeType == AnimationType.Walk ? _walkFramesPerAngle : _jumpFramesPerAngle;

        /// <summary>Walk frame dimensions (used by Baker for foot-anchor loading).</summary>
        public int WalkFrameWidth  => _walkFrameWidth;
        public int WalkFrameHeight => _walkFrameHeight;

        public int  CurrentAngle { get; private set; } = 0;
        public int  CurrentFrame { get; private set; } = 0;

        /// <summary>The currently active animation type.</summary>
        public AnimationType ActiveType => _activeType;

        /// <summary>The texture for the currently active animation.</summary>
        public Texture2D ActiveTexture => _activeType == AnimationType.Walk ? _walkTexture : _jumpTexture;

        // ---------------------------------------------------------------
        // Private state
        // ---------------------------------------------------------------

        private float _frameTimer;
        private bool  _isWalking;
        private AnimationType _activeType = AnimationType.Walk;

        // ---------------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------------

        public AnimationController(
            Texture2D walkTexture, int walkFrameW, int walkFrameH, int walkFramesPerAngle,
            Texture2D jumpTexture, int jumpFrameW, int jumpFrameH, int jumpFramesPerAngle)
        {
            _walkTexture        = walkTexture;
            _walkFrameWidth     = walkFrameW;
            _walkFrameHeight    = walkFrameH;
            _walkFramesPerAngle = walkFramesPerAngle;

            _jumpTexture        = jumpTexture;
            _jumpFrameWidth     = jumpFrameW;
            _jumpFrameHeight    = jumpFrameH;
            _jumpFramesPerAngle = jumpFramesPerAngle;
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>
        /// Switch the active animation type.
        /// Resets the frame counter when changing type.
        /// </summary>
        public void SetAnimation(AnimationType type)
        {
            if (_activeType == type) return;
            _activeType = type;
            CurrentFrame = 0;
            _frameTimer  = 0f;
        }

        /// <summary>
        /// Set a specific frame directly (used by Baker for jump phases).
        /// </summary>
        public void SetFrame(int frame)
        {
            CurrentFrame = Math.Clamp(frame, 0, FramesPerAngle - 1);
        }

        /// <summary>
        /// Advance the animation state each game tick.
        /// </summary>
        public void Update(GameTime gameTime, Vector2 movementDirection)
        {
            _isWalking = movementDirection != Vector2.Zero;

            if (_isWalking)
            {
                int targetAngle = DirectionToAngle(movementDirection);
                CurrentAngle    = targetAngle;
            }

            if (_activeType == AnimationType.Walk)
            {
                if (_isWalking)
                {
                    _frameTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    float frameDuration = 1f / TargetFps;
                    if (_frameTimer >= frameDuration)
                    {
                        _frameTimer -= frameDuration;
                        CurrentFrame = (CurrentFrame + 1) % _walkFramesPerAngle;
                    }
                }
                else
                {
                    CurrentFrame = 0;
                    _frameTimer  = 0f;
                }
            }
            // Jump animation frames are driven by Baker.cs via SetFrame().
        }

        /// <summary>
        /// Returns the source rectangle for the current frame from the active spritesheet.
        /// </summary>
        public Rectangle GetSourceRect()
        {
            int row = AngleToRow(CurrentAngle);
            int x   = CurrentFrame * FrameWidth;
            int y   = row          * FrameHeight;
            return new Rectangle(x, y, FrameWidth, FrameHeight);
        }

        private static int DirectionToAngle(Vector2 dir)
        {
            double radians = Math.Atan2(-dir.Y, dir.X);
            double degrees = radians * (180.0 / Math.PI);
            if (degrees < 0) degrees += 360.0;

            double spriteAngle = (270.0 - degrees + 360.0) % 360.0;

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

        private static double AngleDiff(double a, double b)
        {
            double d = (a - b + 360.0) % 360.0;
            return d > 180.0 ? d - 360.0 : d;
        }

        private static int AngleToRow(int angle)
        {
            for (int i = 0; i < AngleOrder.Length; i++)
                if (AngleOrder[i] == angle)
                    return i;
            return 0;
        }
    }
}
