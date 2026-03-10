using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotliDemo
{
    /// <summary>
    /// Loads the locomotive sprite and level-editor JSON, maps all
    /// coordinates from image-pixel space to screen space, and exposes
    /// the data for <see cref="CollisionSystem"/> and rendering.
    ///
    /// Coordinate flow
    /// ───────────────
    ///  1. The Python level editor records everything in image-pixel
    ///     space (origin = top-left of locomotive.png).
    ///  2. At runtime the train is drawn at <see cref="DrawPosition"/>
    ///     with <see cref="DrawScale"/>.  Every JSON coordinate is
    ///     mapped:  screenPos = imagePos * DrawScale + DrawPosition.
    /// </summary>
    public class Train
    {
        // ---------------------------------------------------------------
        // Constants
        // ---------------------------------------------------------------

        /// <summary>
        /// Draw scale for the locomotive sprite.
        /// Raw image 4607×640 → ~1843×256 at 0.4×.
        /// </summary>
        public const float DrawScale = 0.4f;

        // ---------------------------------------------------------------
        // Fields
        // ---------------------------------------------------------------

        private Texture2D _texture = null!;

        /// <summary>Top-left screen position of the drawn train.</summary>
        public Vector2 DrawPosition { get; private set; }

        /// <summary>Screen-space bounding box of the drawn train.</summary>
        public Rectangle Bounds { get; private set; }

        // ---------------------------------------------------------------
        // Level data (screen space)
        // ---------------------------------------------------------------

        /// <summary>Surface boundary polygon (screen space).</summary>
        public Vector2[] SurfaceBoundary { get; private set; } = Array.Empty<Vector2>();

        /// <summary>Obstacle rectangles (screen space).</summary>
        public CollisionSystem.ObstacleRect[] Obstacles { get; private set; }
            = Array.Empty<CollisionSystem.ObstacleRect>();

        /// <summary>Action zone rectangles (screen space).</summary>
        public CollisionSystem.ActionZone[] ActionZones { get; private set; }
            = Array.Empty<CollisionSystem.ActionZone>();

        /// <summary>Named anchor points (screen space).</summary>
        public (string Label, Vector2 Position)[] AnchorPoints { get; private set; }
            = Array.Empty<(string, Vector2)>();

        /// <summary>Jump barrier rectangles (screen space).</summary>
        public Rectangle[] JumpBarriers { get; private set; }
            = Array.Empty<Rectangle>();

        // ---------------------------------------------------------------
        // Load
        // ---------------------------------------------------------------

        public void LoadContent(
            Microsoft.Xna.Framework.Content.ContentManager content,
            int screenWidth, int screenHeight)
        {
            _texture = content.Load<Texture2D>("locomotive");

            int scaledW = (int)(_texture.Width  * DrawScale);
            int scaledH = (int)(_texture.Height * DrawScale);

            int drawX = (screenWidth  - scaledW) / 2;
            int drawY = (screenHeight - scaledH) * 2 / 3;
            DrawPosition = new Vector2(drawX, drawY);
            Bounds = new Rectangle(drawX, drawY, scaledW, scaledH);

            // ---- Load level-editor JSON --------------------------------
            var raw = LoadBoundsJson("Content/locomotive_bounds.json");

            // Surface boundary.
            if (raw.SurfaceBoundary != null)
            {
                SurfaceBoundary = new Vector2[raw.SurfaceBoundary.Length];
                for (int i = 0; i < raw.SurfaceBoundary.Length; i++)
                    SurfaceBoundary[i] = ToScreenPoint(raw.SurfaceBoundary[i]);
            }

            // Obstacles.
            if (raw.Obstacles != null)
            {
                Obstacles = new CollisionSystem.ObstacleRect[raw.Obstacles.Length];
                for (int i = 0; i < raw.Obstacles.Length; i++)
                {
                    Obstacles[i] = new CollisionSystem.ObstacleRect
                    {
                        Label = raw.Obstacles[i].Label ?? "",
                        Bounds = ToScreenRect(raw.Obstacles[i].Bounds)
                    };
                }
            }

            // Action zones.
            if (raw.ActionZones != null)
            {
                ActionZones = new CollisionSystem.ActionZone[raw.ActionZones.Length];
                for (int i = 0; i < raw.ActionZones.Length; i++)
                {
                    ActionZones[i] = new CollisionSystem.ActionZone
                    {
                        Label = raw.ActionZones[i].Label ?? "",
                        Bounds = ToScreenRect(raw.ActionZones[i].Bounds)
                    };
                }
            }

            // Anchor points.
            if (raw.AnchorPoints != null)
            {
                AnchorPoints = new (string, Vector2)[raw.AnchorPoints.Length];
                for (int i = 0; i < raw.AnchorPoints.Length; i++)
                {
                    AnchorPoints[i] = (
                        raw.AnchorPoints[i].Label ?? "",
                        ToScreenPoint(raw.AnchorPoints[i].Position));
                }
            }

            // Jump barriers.
            if (raw.JumpBarriers != null)
            {
                JumpBarriers = new Rectangle[raw.JumpBarriers.Length];
                for (int i = 0; i < raw.JumpBarriers.Length; i++)
                    JumpBarriers[i] = ToScreenRect(raw.JumpBarriers[i]);
            }
        }

        /// <summary>
        /// Populate a <see cref="CollisionSystem"/> with the loaded level data.
        /// Call once after LoadContent.
        /// </summary>
        public void PopulateCollision(CollisionSystem collision)
        {
            collision.SurfaceBoundary = SurfaceBoundary;
            collision.Obstacles       = Obstacles;
            collision.ActionZones     = ActionZones;
            collision.JumpBarriers    = JumpBarriers;
        }

        /// <summary>
        /// Get a named anchor point's screen position.
        /// Returns null if the label is not found.
        /// </summary>
        public Vector2? GetAnchorPoint(string label)
        {
            for (int i = 0; i < AnchorPoints.Length; i++)
            {
                if (string.Equals(AnchorPoints[i].Label, label,
                        StringComparison.OrdinalIgnoreCase))
                    return AnchorPoints[i].Position;
            }
            return null;
        }

        // ---------------------------------------------------------------
        // Coordinate mapping (image-pixel → screen)
        // ---------------------------------------------------------------

        private Vector2 ToScreenPoint(PointData p) =>
            new Vector2(p.X * DrawScale + DrawPosition.X, p.Y * DrawScale + DrawPosition.Y);

        private Rectangle ToScreenRect(RectData r) =>
            new Rectangle(
                (int)(r.X * DrawScale + DrawPosition.X),
                (int)(r.Y * DrawScale + DrawPosition.Y),
                (int)(r.Width  * DrawScale),
                (int)(r.Height * DrawScale));



        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(
                texture:         _texture,
                position:        DrawPosition,
                sourceRectangle: null,
                color:           Color.White,
                rotation:        0f,
                origin:          Vector2.Zero,
                scale:           DrawScale,
                effects:         SpriteEffects.None,
                layerDepth:      0f);
        }

        // ---------------------------------------------------------------
        // JSON loading
        // ---------------------------------------------------------------

        private static LevelData LoadBoundsJson(string relativePath)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(basePath, relativePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException(
                    $"Train boundary data not found at:\n  {fullPath}\n" +
                    "Run  tools/level_editor.py  first.");

            string json = File.ReadAllText(fullPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<LevelData>(json, options);
        }

        // ---------------------------------------------------------------
        // DTOs for JSON deserialisation
        // ---------------------------------------------------------------

        private struct PointData
        {
            [JsonPropertyName("x")] public float X { get; set; }
            [JsonPropertyName("y")] public float Y { get; set; }
        }

        private struct RectData
        {
            [JsonPropertyName("x")]      public float X { get; set; }
            [JsonPropertyName("y")]      public float Y { get; set; }
            [JsonPropertyName("width")]  public float Width { get; set; }
            [JsonPropertyName("height")] public float Height { get; set; }
        }

        private struct ImageSizeData
        {
            [JsonPropertyName("width")]  public int Width { get; set; }
            [JsonPropertyName("height")] public int Height { get; set; }
        }

        private struct ObstacleData
        {
            [JsonPropertyName("label")]  public string Label { get; set; }
            [JsonPropertyName("bounds")] public RectData Bounds { get; set; }
        }

        private struct ActionZoneData
        {
            [JsonPropertyName("label")]  public string Label { get; set; }
            [JsonPropertyName("bounds")] public RectData Bounds { get; set; }
        }

        private struct AnchorPointData
        {
            [JsonPropertyName("label")]    public string Label { get; set; }
            [JsonPropertyName("position")] public PointData Position { get; set; }
        }

        private struct LevelData
        {
            [JsonPropertyName("imageSize")]
            public ImageSizeData ImageSize { get; set; }

            [JsonPropertyName("surfaceBoundary")]
            public PointData[] SurfaceBoundary { get; set; }

            [JsonPropertyName("obstacles")]
            public ObstacleData[] Obstacles { get; set; }

            [JsonPropertyName("actionZones")]
            public ActionZoneData[] ActionZones { get; set; }

            [JsonPropertyName("anchorPoints")]
            public AnchorPointData[] AnchorPoints { get; set; }

            [JsonPropertyName("jumpBarriers")]
            public RectData[] JumpBarriers { get; set; }
        }
    }
}
