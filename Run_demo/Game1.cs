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
    ///  • Set up the window (1280 × 720).
    ///  • Load the spritesheet texture and JSON metadata.
    ///  • Create the Baker and AnimationController.
    ///  • Run the standard Update/Draw loop.
    /// </summary>
    public class Game1 : Game
    {
        // ---------------------------------------------------------------
        // Constants
        // ---------------------------------------------------------------

        private const int ScreenWidth  = 1920;
        private const int ScreenHeight = 1080;

        // ---- 2.5D isometric floor settings ----
        // Tile footprint in world-space (square), then projected with a
        // Y-compression of 0.5 to simulate a ~45° downward camera tilt.
        private const int   WorldTileSize  = 64;          // square world tile
        private const float IsoYScale      = 0.5f;        // Y compression for 45° tilt
        private const int   TileScreenW    = WorldTileSize;                     // 64 px wide
        private const int   TileScreenH    = (int)(WorldTileSize * IsoYScale);  // 32 px tall

        // Colours for the two-tone floor.
        private static readonly Color FloorColourA = new Color(72, 100, 72);   // muted green
        private static readonly Color FloorColourB = new Color(60,  86, 60);   // darker green

        // ---------------------------------------------------------------
        // MonoGame infrastructure
        // ---------------------------------------------------------------

        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch = null!;

        // ---------------------------------------------------------------
        // Game objects
        // ---------------------------------------------------------------

        private Baker       _baker      = null!;
        private Texture2D   _floorTex   = null!;   // 1x1 solid colour used as tiled floor

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

            // ---- Load spritesheet metadata (JSON) --------------------
            // The JSON file is deployed to the Content folder as a plain
            // file (no MGCB processing — see Content.mgcb comments).
            SpritesheetMeta meta = LoadMetadata("Content/Baker_Spritesheet.json");

            // ---- Load spritesheet texture ----------------------------
            // Loaded via the standard content pipeline (Baker_Spritesheet.png
            // must be listed in Content.mgcb with Importer=TextureImporter).
            Texture2D sheet = Content.Load<Texture2D>("Baker_Spritesheet");

            // ---- Build AnimationController ---------------------------
            var animCtrl = new AnimationController(
                frameWidth:     meta.FrameWidth,
                frameHeight:    meta.FrameHeight,
                framesPerAngle: meta.FramesPerAngle,
                totalAngles:    meta.TotalAngles
            );

            // ---- Create Baker ----------------------------------------
            _baker = new Baker(sheet, animCtrl, ScreenWidth, ScreenHeight);

            // ---- Create a solid 1x1 texture for the floor ------------
            _floorTex = new Texture2D(GraphicsDevice, 1, 1);
            _floorTex.SetData(new[] { Color.White });
        }

        // ---------------------------------------------------------------
        // Update
        // ---------------------------------------------------------------

        protected override void Update(GameTime gameTime)
        {
            // Allow Alt+F4 / Back button to quit.
            KeyboardState kb = Keyboard.GetState();
            if (kb.IsKeyDown(Keys.Escape))
                Exit();

            _baker.Update(gameTime, kb);

            base.Update(gameTime);
        }

        // ---------------------------------------------------------------
        // Draw
        // ---------------------------------------------------------------

        protected override void Draw(GameTime gameTime)
        {
            // Clear to a dark sky / background colour.
            GraphicsDevice.Clear(new Color(40, 40, 50));

            _spriteBatch.Begin(
                sortMode:        SpriteSortMode.Deferred,
                blendState:      BlendState.AlphaBlend,
                samplerState:    SamplerState.PointClamp,       // crisp pixel art
                depthStencilState: null,
                rasterizerState:   null
            );

            // ---- Draw scrolling tiled floor --------------------------
            DrawFloor();

            // ---- Draw Baker character --------------------------------
            _baker.Draw(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Draws a 2.5D isometric-style checkerboard floor.
        ///
        /// Each world tile (64 × 64) is projected as a screen rectangle
        /// of 64 × 32, simulating a 45° downward camera tilt.  The grid
        /// scrolls opposite to the Baker's world position so the character
        /// appears to walk across an infinite plane.
        /// </summary>
        private void DrawFloor()
        {
            int sw = GraphicsDevice.Viewport.Width;
            int sh = GraphicsDevice.Viewport.Height;

            // World offset projected into screen space.
            float offsetX = (-_baker.Position.X) % TileScreenW;
            float offsetY = (-_baker.Position.Y * IsoYScale) % TileScreenH;

            // Keep offsets negative so the first tile is always off-screen.
            if (offsetX > 0) offsetX -= TileScreenW;
            if (offsetY > 0) offsetY -= TileScreenH;

            int tilesX = (sw / TileScreenW) + 3;
            int tilesY = (sh / TileScreenH) + 3;

            // Compute the world-space tile index of the top-left visible
            // tile so the checker pattern stays consistent when scrolling.
            int baseTX = (int)Math.Floor(_baker.Position.X / WorldTileSize);
            int baseTY = (int)Math.Floor(_baker.Position.Y / WorldTileSize);

            for (int ty = 0; ty < tilesY; ty++)
            {
                for (int tx = 0; tx < tilesX; tx++)
                {
                    // Checker based on world tile index — not screen tile.
                    bool checker = ((baseTX + tx) + (baseTY + ty)) % 2 == 0;
                    Color col = checker ? FloorColourA : FloorColourB;

                    var dest = new Rectangle(
                        (int)(offsetX + tx * TileScreenW),
                        (int)(offsetY + ty * TileScreenH),
                        TileScreenW,
                        TileScreenH
                    );

                    _spriteBatch.Draw(_floorTex, dest, col);
                }
            }
        }

        /// <summary>
        /// Reads Baker_Spritesheet.json from the given relative path and
        /// deserialises it into a <see cref="SpritesheetMeta"/> struct.
        /// The file is plain JSON (not MGCB-processed) and sits in the
        /// output Content/ folder alongside the compiled texture.
        /// </summary>
        private static SpritesheetMeta LoadMetadata(string relativePath)
        {
            // Resolve path relative to the executable directory.
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(basePath, relativePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException(
                    $"Spritesheet metadata not found at:\n  {fullPath}\n" +
                    "Make sure Baker_Spritesheet.json is set to 'Copy to Output' " +
                    "in the Content.mgcb file (as a plain file, not a processed asset).");

            string json = File.ReadAllText(fullPath);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var meta    = JsonSerializer.Deserialize<SpritesheetMeta>(json, options);
            return meta;
        }

        // ---------------------------------------------------------------
        // Nested metadata DTO
        // ---------------------------------------------------------------

        /// <summary>
        /// Plain-old-data type that mirrors the JSON produced by pack_spritesheet.py.
        /// System.Text.Json deserialises directly into this struct.
        /// </summary>
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
