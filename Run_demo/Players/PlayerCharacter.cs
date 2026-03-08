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

    public class PlayerCharacter
    {
        public const float WalkSpeed = 2.5f * 60f;
        public const float RunSpeed = 5.5f * 60f;
        public const float WalkAnimFps = 8f;
        public const float RunAnimFps = 14f;
        public const float DrawScale = 0.2f;
        public const float JumpVelocity = -6f;
        public const float Gravity = 0.35f;
        public const float MaxFallSpeed = 12f;
        private const float LandingDuration = 0.15f;

        public Vector2 Position;
        public int PlayerIndex { get; }
        public bool IsRunning { get; private set; }
        public string? CurrentZoneLabel { get; private set; }
        public JumpState JumpState { get; private set; } = JumpState.Grounded;
        public float CurrentJumpHeight { get; private set; }
        public Vector2 PredictedLanding { get; private set; }
        public bool InteractPressed { get; private set; }
        public Color Tint { get; set; } = Color.White;

        private readonly AnimationController _anim;
        private readonly PlayerInput _input;
        private Vector2 _footAnchor;
        private float _verticalVelocity;
        private Vector2 _jumpMomentum;
        private float _landingTimer;
        private bool _jumpWasDown;
        private bool _interactWasDown;

        public PlayerCharacter(int playerIndex, PlayerInput input,
                               AnimationController anim, string boundsJsonPath)
        {
            PlayerIndex = playerIndex;
            _input = input;
            _anim = anim;
            LoadBounds(boundsJsonPath, anim.WalkFrameWidth, anim.WalkFrameHeight);
            Position = Vector2.Zero;
        }

        public AnimationController Animation => _anim;

        public void Update(GameTime gameTime, KeyboardState kb, CollisionSystem collision)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            IsRunning = kb.IsKeyDown(_input.Run);
            float speed = IsRunning ? RunSpeed : WalkSpeed;
            _anim.TargetFps = IsRunning ? RunAnimFps : WalkAnimFps;

            Vector2 rawDir = Vector2.Zero;
            if (kb.IsKeyDown(_input.Left)) rawDir.X -= 1f;
            if (kb.IsKeyDown(_input.Right)) rawDir.X += 1f;
            if (kb.IsKeyDown(_input.Up)) rawDir.Y -= 1f;
            if (kb.IsKeyDown(_input.Down)) rawDir.Y += 1f;
            if (rawDir != Vector2.Zero)
                rawDir = Vector2.Normalize(rawDir);

            bool jumpDown = kb.IsKeyDown(_input.Jump);
            bool jumpPressed = jumpDown && !_jumpWasDown;
            _jumpWasDown = jumpDown;

            bool interactDown = kb.IsKeyDown(_input.Interact);
            InteractPressed = interactDown && !_interactWasDown;
            _interactWasDown = interactDown;

            switch (JumpState)
            {
                case JumpState.Grounded:
                {
                    Vector2 desired = Position + rawDir * speed * dt;
                    Position = collision.ResolveMovement(Position, desired);

                    if (jumpPressed)
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
                    _anim.Update(gameTime, rawDir);
                    break;
                }

                case JumpState.Rising:
                {
                    _verticalVelocity += Gravity;
                    CurrentJumpHeight -= _verticalVelocity;

                    Vector2 desired = Position + _jumpMomentum * dt;
                    Position = collision.ResolveMovementAirborne(Position, desired);

                    if (_verticalVelocity >= 0f)
                        JumpState = JumpState.Falling;

                    float risingProgress = Math.Clamp(
                        1f - (CurrentJumpHeight / GetPeakHeight()), 0f, 1f);
                    _anim.SetFrame((int)(risingProgress * 2.99f));

                    Vector2 animDir = _jumpMomentum != Vector2.Zero
                        ? Vector2.Normalize(_jumpMomentum) : Vector2.Zero;
                    _anim.Update(gameTime, animDir);
                    break;
                }

                case JumpState.Falling:
                {
                    _verticalVelocity += Gravity;
                    if (_verticalVelocity > MaxFallSpeed)
                        _verticalVelocity = MaxFallSpeed;
                    CurrentJumpHeight -= _verticalVelocity;

                    Vector2 desired = Position + _jumpMomentum * dt;
                    Position = collision.ResolveMovementAirborne(Position, desired);

                    if (CurrentJumpHeight <= 0f)
                    {
                        CurrentJumpHeight = 0f;
                        JumpState = JumpState.Grounded;
                        _anim.SetAnimation(AnimationType.Walk);
                    }
                    else
                    {
                        float fallProgress = Math.Clamp(
                            CurrentJumpHeight / GetPeakHeight(), 0f, 1f);
                        _anim.SetFrame(3 + (int)((1f - fallProgress) * 1.99f));
                    }

                    Vector2 animDir = _jumpMomentum != Vector2.Zero
                        ? Vector2.Normalize(_jumpMomentum) : Vector2.Zero;
                    _anim.Update(gameTime, animDir);
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
                    _anim.SetFrame(5);
                    _anim.Update(gameTime, Vector2.Zero);
                    break;
                }
            }

            CurrentZoneLabel = collision.GetActiveZone(Position);
        }

        private static float GetPeakHeight()
        {
            return (JumpVelocity * JumpVelocity) / (2f * Gravity);
        }

        public static Vector2 PredictLandingPosition(Vector2 startPos, Vector2 momentum)
        {
            float airtime = 2f * Math.Abs(JumpVelocity) / Gravity;
            float airtimeSeconds = airtime / 60f;
            return startPos + momentum * airtimeSeconds;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Rectangle src = _anim.GetSourceRect();
            Vector2 drawPos = Position;
            drawPos.Y -= CurrentJumpHeight;

            spriteBatch.Draw(
                texture: _anim.ActiveTexture,
                position: drawPos,
                sourceRectangle: src,
                color: Tint,
                rotation: 0f,
                origin: _footAnchor,
                scale: DrawScale,
                effects: SpriteEffects.None,
                layerDepth: 0f);
        }

        private void LoadBounds(string relativePath, int frameW, int frameH)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(basePath, relativePath);

            if (!File.Exists(fullPath))
            {
                _footAnchor = new Vector2(frameW / 2f, frameH);
                return;
            }

            string json = File.ReadAllText(fullPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<BoundsData>(json, options);
            _footAnchor = new Vector2(data.FootAnchor.X, data.FootAnchor.Y);
        }

        private struct PointData
        {
            [JsonPropertyName("x")] public int X { get; set; }
            [JsonPropertyName("y")] public int Y { get; set; }
        }

        private struct BoundsData
        {
            [JsonPropertyName("footAnchor")] public PointData FootAnchor { get; set; }
        }
    }
}
