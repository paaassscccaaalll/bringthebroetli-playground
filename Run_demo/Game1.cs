using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BringTheBrotliDemo
{
    /// <summary>
    /// Main game class for the "Bring the Brötli" demo.
    ///
    /// Responsibilities
    /// ────────────────
    ///  • Set up the window (1920 × 1080).
    ///  • Load the spritesheet, train, and collision system.
    ///  • Run Update / Draw loop.
    ///  • Render action-zone tooltip above Baker.
    /// </summary>
    public class Game1 : Game
    {
        // ---------------------------------------------------------------
        // Constants
        // ---------------------------------------------------------------

        private const int ScreenWidth  = 1920;
        private const int ScreenHeight = 1080;

        // ---------------------------------------------------------------
        // MonoGame infrastructure
        // ---------------------------------------------------------------

        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch = null!;

        // ---------------------------------------------------------------
        // Game objects
        // ---------------------------------------------------------------

        private Baker           _baker     = null!;
        private Train           _train     = null!;
        private CollisionSystem _collision = null!;
        private SpriteFont      _font      = null!;

        // Tooltip rendering.
        private Texture2D _pixel = null!;   // 1×1 white texture for drawing rects

        // Debug overlay
        private bool _debugMode = false;
        private bool _f1WasDown = false;

        // ---------------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------------

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth  = ScreenWidth,
                PreferredBackBufferHeight = ScreenHeight,
                IsFullScreen              = false
            };

            Content.RootDirectory = "Content";
            IsMouseVisible        = true;
            Window.AllowUserResizing = true;
        }

        // ---------------------------------------------------------------
        // Initialize
        // ---------------------------------------------------------------

        protected override void Initialize()
        {
            Window.Title = "Bring the Br\u00f6tli \u2014 Demo";
            base.Initialize();
        }

        // ---------------------------------------------------------------
        // LoadContent
        // ---------------------------------------------------------------

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // 1×1 white pixel for rectangle drawing.
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // ---- Load font (for tooltip text) -------------------------
            _font = Content.Load<SpriteFont>("DefaultFont");

            // ---- Load walk spritesheet metadata + texture -------------
            SpritesheetMeta walkMeta = LoadMetadata("Content/Baker_Walk_Spritesheet.json");
            Texture2D walkSheet = Content.Load<Texture2D>("Baker_Walk_Spritesheet");

            // ---- Load jump spritesheet metadata + texture -------------
            SpritesheetMeta jumpMeta = LoadMetadata("Content/Baker_Jump_Spritesheet.json");
            Texture2D jumpSheet = Content.Load<Texture2D>("Baker_Jump_Spritesheet");

            var animCtrl = new AnimationController(
                walkTexture: walkSheet,
                walkFrameW: walkMeta.FrameWidth,
                walkFrameH: walkMeta.FrameHeight,
                walkFramesPerAngle: walkMeta.FramesPerAngle,
                jumpTexture: jumpSheet,
                jumpFrameW: jumpMeta.FrameWidth,
                jumpFrameH: jumpMeta.FrameHeight,
                jumpFramesPerAngle: jumpMeta.FramesPerAngle
            );

            // ---- Create Baker ----------------------------------------
            _baker = new Baker(animCtrl, ScreenWidth, ScreenHeight);

            // ---- Load Train ------------------------------------------
            _train = new Train();
            _train.LoadContent(Content, ScreenWidth, ScreenHeight);

            // ---- Create and populate CollisionSystem -----------------
            _collision = new CollisionSystem();
            _train.PopulateCollision(_collision);

            // ---- Place Baker at "spawn" anchor, or polygon centroid --
            Vector2? spawn = _train.GetAnchorPoint("spawn");
            if (spawn.HasValue)
            {
                _baker.Position = spawn.Value;
            }
            else if (_train.SurfaceBoundary.Length > 0)
            {
                Vector2 centroid = Vector2.Zero;
                foreach (var p in _train.SurfaceBoundary)
                    centroid += p;
                centroid /= _train.SurfaceBoundary.Length;
                _baker.Position = centroid;
            }
        }

        // ---------------------------------------------------------------
        // Update
        // ---------------------------------------------------------------

        protected override void Update(GameTime gameTime)
        {
            KeyboardState kb = Keyboard.GetState();
            if (kb.IsKeyDown(Keys.Escape))
                Exit();

            // Toggle debug overlay with F1
            bool f1Down = kb.IsKeyDown(Keys.F1);
            if (f1Down && !_f1WasDown)
                _debugMode = !_debugMode;
            _f1WasDown = f1Down;

            _baker.Update(gameTime, kb, _collision);

            base.Update(gameTime);
        }

        // ---------------------------------------------------------------
        // Draw
        // ---------------------------------------------------------------

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(40, 40, 50));

            _spriteBatch.Begin(
                sortMode:          SpriteSortMode.Deferred,
                blendState:        BlendState.AlphaBlend,
                samplerState:      SamplerState.PointClamp,
                depthStencilState: null,
                rasterizerState:   null
            );

            // Layer 1: Train
            _train.Draw(_spriteBatch);

            // Layer 2: Baker
            _baker.Draw(_spriteBatch);

            // Layer 3: Tooltip (if Baker is in an action zone)
            DrawTooltip(_spriteBatch);

            // Layer 4: Debug overlay (F1 toggle)
            if (_debugMode)
                DrawDebugOverlay(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        // ---------------------------------------------------------------
        // Tooltip rendering
        // ---------------------------------------------------------------

        private void DrawTooltip(SpriteBatch sb)
        {
            string? label = _baker.CurrentZoneLabel;
            if (label == null)
                return;

            // Measure text.
            Vector2 textSize = _font.MeasureString(label);
            int padX = 8, padY = 4;

            // Position tooltip above the Baker's head.
            float tooltipX = _baker.Position.X - textSize.X / 2f - padX;
            float tooltipY = _baker.Position.Y - 90f;  // above sprite

            // Background rectangle.
            Rectangle bg = new Rectangle(
                (int)tooltipX, (int)tooltipY,
                (int)textSize.X + padX * 2,
                (int)textSize.Y + padY * 2);

            sb.Draw(_pixel, bg, new Color(255, 255, 255, 220));

            // Border (draw 4 thin rectangles).
            Color border = new Color(60, 60, 60);
            sb.Draw(_pixel, new Rectangle(bg.X, bg.Y, bg.Width, 1), border);
            sb.Draw(_pixel, new Rectangle(bg.X, bg.Bottom - 1, bg.Width, 1), border);
            sb.Draw(_pixel, new Rectangle(bg.X, bg.Y, 1, bg.Height), border);
            sb.Draw(_pixel, new Rectangle(bg.Right - 1, bg.Y, 1, bg.Height), border);

            // Text.
            sb.DrawString(_font, label,
                new Vector2(bg.X + padX, bg.Y + padY),
                new Color(40, 40, 40));
        }

        // ---------------------------------------------------------------
        // Debug overlay
        // ---------------------------------------------------------------

        private void DrawDebugOverlay(SpriteBatch sb)
        {
            // ---- 1. Surface boundary (green lines) --------------------
            var boundary = _collision.SurfaceBoundary;
            if (boundary.Length >= 3)
            {
                for (int i = 0; i < boundary.Length; i++)
                {
                    int j = (i + 1) % boundary.Length;
                    DrawLine(sb, boundary[i], boundary[j], Color.Lime, 1);
                }
            }

            // ---- 2. Obstacles (yellow outlines) -----------------------
            for (int i = 0; i < _collision.Obstacles.Length; i++)
                DrawRectOutline(sb, _collision.Obstacles[i].Bounds, Color.Yellow, 1);

            // ---- 3. Jump barriers (cyan outlines) ---------------------
            for (int i = 0; i < _collision.JumpBarriers.Length; i++)
                DrawRectOutline(sb, _collision.JumpBarriers[i], Color.Cyan, 2);

            // ---- 4. Foot anchor dot (red, 4×4) -----------------------
            sb.Draw(_pixel,
                new Rectangle(
                    (int)_baker.Position.X - 2,
                    (int)_baker.Position.Y - 2, 4, 4),
                Color.Red);

            // ---- 5. Predicted landing (magenta, only during jump) -----
            if (_baker.JumpState == JumpState.Rising ||
                _baker.JumpState == JumpState.Falling)
            {
                Vector2 lp = _baker.PredictedLanding;
                sb.Draw(_pixel,
                    new Rectangle((int)lp.X - 3, (int)lp.Y - 3, 6, 6),
                    Color.Magenta);
            }

            // ---- 6. Jump shadow (foot anchor on ground during jump) ---
            if (_baker.JumpState != JumpState.Grounded &&
                _baker.JumpState != JumpState.Landing)
            {
                sb.Draw(_pixel,
                    new Rectangle(
                        (int)_baker.Position.X - 8,
                        (int)_baker.Position.Y - 1, 16, 2),
                    new Color(0, 0, 0, 100));
            }

            // ---- 7. JumpState text (top-left) -------------------------
            string stateText = $"Jump: {_baker.JumpState}  Height: {_baker.CurrentJumpHeight:F1}";
            sb.DrawString(_font, stateText, new Vector2(10, 10), Color.White);
        }

        /// <summary>Draw a 1px-wide line between two points.</summary>
        private void DrawLine(SpriteBatch sb, Vector2 a, Vector2 b, Color color, int thickness)
        {
            Vector2 delta = b - a;
            float length = delta.Length();
            if (length < 0.5f) return;

            float angle = (float)Math.Atan2(delta.Y, delta.X);
            sb.Draw(_pixel,
                a,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(length, thickness),
                SpriteEffects.None,
                0f);
        }

        /// <summary>Draw a rectangle outline.</summary>
        private void DrawRectOutline(SpriteBatch sb, Rectangle rect, Color color, int thickness)
        {
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            sb.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        private static SpritesheetMeta LoadMetadata(string relativePath)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(basePath, relativePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException(
                    $"Spritesheet metadata not found at:\n  {fullPath}\n" +
                    "Make sure the JSON file is set to 'Copy to Output' " +
                    "in the .csproj file.");

            string json = File.ReadAllText(fullPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<SpritesheetMeta>(json, options);
        }

        // ---------------------------------------------------------------
        // Nested metadata DTO
        // ---------------------------------------------------------------

        private struct SpritesheetMeta
        {
            public int   FrameWidth      { get; set; }
            public int   FrameHeight     { get; set; }
            public int   TotalAngles     { get; set; }
            public int   FramesPerAngle  { get; set; }
            public int[] AngleOrder      { get; set; }
        }
    }
}
