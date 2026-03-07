using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BringTheBrotliDemo
{
    public enum JumpState { Grounded, Rising, Falling, Landing }

    /// <summary>
    /// Represents the playable Baker character with full jump physics.
    /// </summary>
    public class Baker
    {
        // ---------------------------------------------------------------
        // Constants
        // ---------------------------------------------------------------

        public const float WalkSpeed = 2.5f * 60f;   // ~150 px/s
        public const float RunSpeed  = 5.5f * 60f;   // ~330 px/s

        public const float WalkAnimFps = 8f;
        public const float RunAnimFps  = 14f;

        public const float DrawScale = 0.2f;

        // Jump physics constants
        public const float JumpVelocity = -6f;   // negative = upward
        public const float Gravity      = 0.35f;
        public const float MaxFallSpeed = 12f;

        // Landing animation duration
        private const float LandingDuration = 0.15f;

        // ---------------------------------------------------------------
        // Fields
        // ---------------------------------------------------------------

        /// <summary>Screen-space position of the Baker's foot anchor.</summary>
        public Vector2 Position;

        public bool IsRunning { get; private set; }

        public string? CurrentZoneLabel { get; private set; }

        /// <summary>Current jump state.</summary>
        public JumpState JumpState { get; private set; } = JumpState.Grounded;

        /// <summary>Current visual jump height (positive = higher).</summary>
        public float CurrentJumpHeight { get; private set; }

        /// <summary>Predicted landing foot-anchor position.</summary>
        public Vector2 PredictedLanding { get; private set; }

        private readonly AnimationController _anim;

        // Foot anchor within a single frame (pixel coords in the walk frame).
        private Vector2 _footAnchor;

        // Jump physics state
        private float _verticalVelocity;
        private Vector2 _jumpMomentum;  // captured horizontal velocity at jump start
        private float _landingTimer;

        // For detecting key press (not hold)
        private bool _spaceWasDown;

        // ---------------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------------

        public Baker(AnimationController anim, int screenWidth, int screenHeight)
        {
            _anim = anim;

            LoadBakerBounds(
                "Content/baker_bounds.json",
                anim.WalkFrameWidth, anim.WalkFrameHeight);

            Position = Vector2.Zero;
        }

        // ---------------------------------------------------------------
        // Public properties
        // ---------------------------------------------------------------

        public AnimationController Animation => _anim;

        // ---------------------------------------------------------------
        // Update
        // ---------------------------------------------------------------

        public void Update(GameTime gameTime, KeyboardState kb, CollisionSystem collision)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // ---- Walk / run ------------------------------------------
            IsRunning = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);
            float speed = IsRunning ? RunSpeed : WalkSpeed;
            _anim.TargetFps = IsRunning ? RunAnimFps : WalkAnimFps;

            // ---- Build movement vector from WASD --------------------
            Vector2 rawDir = Vector2.Zero;
            if (kb.IsKeyDown(Keys.A)) rawDir.X -= 1f;
            if (kb.IsKeyDown(Keys.D)) rawDir.X += 1f;
            if (kb.IsKeyDown(Keys.W)) rawDir.Y -= 1f;
            if (kb.IsKeyDown(Keys.S)) rawDir.Y += 1f;

            if (rawDir != Vector2.Zero)
                rawDir = Vector2.Normalize(rawDir);

            // ---- Jump state machine ---------------------------------
            bool spaceDown = kb.IsKeyDown(Keys.Space);
            bool spacePressed = spaceDown && !_spaceWasDown;
            _spaceWasDown = spaceDown;

            switch (JumpState)
            {
                case JumpState.Grounded:
                {
                    // Normal ground movement
                    Vector2 desired = Position + rawDir * speed * dt;
                    Position = collision.ResolveMovement(Position, desired);

                    // Try to jump
                    if (spacePressed)
                    {
                        Vector2 momentum = rawDir * speed;
                        Vector2 landingPos = PredictLandingPosition(Position, momentum);

                        if (collision.IsLandingValid(landingPos))
                        {
                            JumpState = JumpState.Rising;
                            _verticalVelocity = JumpVelocity;
                            CurrentJumpHeight = 0f;
                            _jumpMomentum = momentum;
                            PredictedLanding = landingPos;
                            _anim.SetAnimation(AnimationType.Jump);
                        }
                    }

                    // Animation
                    _anim.Update(gameTime, rawDir);
                    break;
                }

                case JumpState.Rising:
                {
                    // Apply gravity
                    _verticalVelocity += Gravity;
                    CurrentJumpHeight -= _verticalVelocity;  // negative vel = go up

                    // Horizontal: use captured momentum, no air control
                    Vector2 desired = Position + _jumpMomentum * dt;
                    Position = collision.ResolveMovementAirborne(Position, desired);

                    // Transition to falling
                    if (_verticalVelocity >= 0f)
                    {
                        JumpState = JumpState.Falling;
                    }

                    // Jump animation: Rising = frames 0-2
                    float risingProgress = Math.Clamp(
                        1f - (CurrentJumpHeight / GetPeakHeight()), 0f, 1f);
                    int frame = (int)(risingProgress * 2.99f);  // 0, 1, 2
                    _anim.SetFrame(frame);

                    // Keep facing direction from momentum
                    if (_jumpMomentum != Vector2.Zero)
                        _anim.Update(gameTime, Vector2.Normalize(_jumpMomentum));
                    else
                        _anim.Update(gameTime, Vector2.Zero);
                    break;
                }

                case JumpState.Falling:
                {
                    // Apply gravity
                    _verticalVelocity += Gravity;
                    if (_verticalVelocity > MaxFallSpeed)
                        _verticalVelocity = MaxFallSpeed;

                    CurrentJumpHeight -= _verticalVelocity;

                    // Horizontal: use captured momentum
                    Vector2 desired = Position + _jumpMomentum * dt;
                    Position = collision.ResolveMovementAirborne(Position, desired);

                    // Land when height reaches/passes 0
                    if (CurrentJumpHeight <= 0f)
                    {
                        CurrentJumpHeight = 0f;
                        JumpState = JumpState.Grounded;
                        _anim.SetAnimation(AnimationType.Walk);
                    }
                    else
                    {
                        // Falling animation: frames 3-4
                        float fallProgress = Math.Clamp(
                            CurrentJumpHeight / GetPeakHeight(), 0f, 1f);
                        int frame = 3 + (int)((1f - fallProgress) * 1.99f); // 3 or 4
                        _anim.SetFrame(frame);
                    }

                    if (_jumpMomentum != Vector2.Zero)
                        _anim.Update(gameTime, Vector2.Normalize(_jumpMomentum));
                    else
                        _anim.Update(gameTime, Vector2.Zero);
                    break;
                }

                case JumpState.Landing:
                {
                    _landingTimer -= dt;
                    if (_landingTimer <= 0f)
                    {
                        JumpState = JumpState.Grounded;
                        _anim.SetAnimation(AnimationType.Walk);
                    }

                    // No movement during landing
                    _anim.SetFrame(5);
                    _anim.Update(gameTime, Vector2.Zero);
                    break;
                }
            }

            // ---- Detect action zone ---------------------------------
            CurrentZoneLabel = collision.GetActiveZone(Position);
        }

        // ---------------------------------------------------------------
        // Jump helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Calculate peak height of the jump arc (for animation mapping).
        /// Peak = v² / (2g)
        /// </summary>
        private static float GetPeakHeight()
        {
            return (JumpVelocity * JumpVelocity) / (2f * Gravity);
        }

        /// <summary>
        /// Predict where the foot anchor will be when the jump arc returns
        /// to ground level (jumpHeight = 0).
        /// </summary>
        public static Vector2 PredictLandingPosition(Vector2 startPos, Vector2 momentum)
        {
            // Total airtime: 2 * |JumpVelocity| / Gravity
            float airtime = 2f * Math.Abs(JumpVelocity) / Gravity;
            // Horizontal displacement during airtime
            // momentum is in px/s, airtime is in "physics frames"
            // Each frame is ~1/60s, so convert: time in seconds = airtime / 60
            float airtimeSeconds = airtime / 60f;
            return startPos + momentum * airtimeSeconds;
        }

        // ---------------------------------------------------------------
        // Draw
        // ---------------------------------------------------------------

        public void Draw(SpriteBatch spriteBatch)
        {
            Rectangle src = _anim.GetSourceRect();

            // During a jump, offset the sprite upward by currentJumpHeight
            Vector2 drawPos = Position;
            drawPos.Y -= CurrentJumpHeight;

            spriteBatch.Draw(
                texture:           _anim.ActiveTexture,
                position:          drawPos,
                sourceRectangle:   src,
                color:             Color.White,
                rotation:          0f,
                origin:            _footAnchor,
                scale:             DrawScale,
                effects:           SpriteEffects.None,
                layerDepth:        0f
            );
        }

        // ---------------------------------------------------------------
        // JSON loading
        // ---------------------------------------------------------------

        private void LoadBakerBounds(string relativePath, int frameW, int frameH)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(basePath, relativePath);

            if (!File.Exists(fullPath))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Baker] baker_bounds.json not found, using defaults.");
                _footAnchor = new Vector2(frameW / 2f, frameH);
                return;
            }

            string json = File.ReadAllText(fullPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data    = JsonSerializer.Deserialize<BakerBoundsData>(json, options);

            _footAnchor = new Vector2(data.FootAnchor.X, data.FootAnchor.Y);
        }

        // ---------------------------------------------------------------
        // DTO for JSON
        // ---------------------------------------------------------------

        private struct PointData
        {
            [JsonPropertyName("x")] public int X { get; set; }
            [JsonPropertyName("y")] public int Y { get; set; }
        }

        private struct BakerBoundsData
        {
            [JsonPropertyName("footAnchor")] public PointData FootAnchor { get; set; }
        }
    }
}
