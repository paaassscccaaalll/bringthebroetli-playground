using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotli
{
    /// <summary>
    /// Central registry for all sprite textures. Loads all textures once at startup
    /// and provides source rectangles for spritesheets.
    /// </summary>
    public static class TextureAtlas
    {
        // ── Train car textures ──
        public static Texture2D TrainEngine { get; private set; } = null!;
        public static Texture2D TrainTender { get; private set; } = null!;
        public static Texture2D TrainBoilerCar { get; private set; } = null!;
        public static Texture2D TrainPassengerCar { get; private set; } = null!;

        // ── Player spritesheets (4 frames each: 32x48 per frame) ──
        public static Texture2D Player1Sheet { get; private set; } = null!;
        public static Texture2D Player2Sheet { get; private set; } = null!;

        public const int PlayerFrameWidth = 32;
        public const int PlayerFrameHeight = 48;
        public const int PlayerFrameCount = 4;

        // ── Backgrounds ──
        public static Texture2D BackgroundHills { get; private set; } = null!;
        public static Texture2D BackgroundMidground { get; private set; } = null!;

        // ── Track ──
        public static Texture2D TrackTile { get; private set; } = null!;
        public const int TrackTileWidth = 64;
        public const int TrackTileHeight = 40;

        // ── Task station spritesheet (4 icons: 40x32 each) ──
        public static Texture2D TaskStationSheet { get; private set; } = null!;
        public const int StationIconWidth = 40;
        public const int StationIconHeight = 32;

        // ── Particles ──
        public static Texture2D ParticleSmoke { get; private set; } = null!;
        public static Texture2D ParticleSteam { get; private set; } = null!;
        public const int ParticleFrameSize = 16;
        public const int ParticleFrameCount = 6;

        // ── UI ──
        public static Texture2D UIGaugeFrame { get; private set; } = null!;
        public static Texture2D HUDPanel { get; private set; } = null!;

        /// <summary>Load all textures from the content pipeline.</summary>
        public static void LoadAll(ContentManager content)
        {
            TrainEngine = content.Load<Texture2D>("Textures/train_engine");
            TrainTender = content.Load<Texture2D>("Textures/train_tender");
            TrainBoilerCar = content.Load<Texture2D>("Textures/train_boiler_car");
            TrainPassengerCar = content.Load<Texture2D>("Textures/train_passenger_car");

            Player1Sheet = content.Load<Texture2D>("Textures/player1");
            Player2Sheet = content.Load<Texture2D>("Textures/player2");

            BackgroundHills = content.Load<Texture2D>("Textures/background_hills");
            BackgroundMidground = content.Load<Texture2D>("Textures/background_midground");

            TrackTile = content.Load<Texture2D>("Textures/track_tile");
            TaskStationSheet = content.Load<Texture2D>("Textures/task_station_spritesheet");

            ParticleSmoke = content.Load<Texture2D>("Textures/particle_smoke");
            ParticleSteam = content.Load<Texture2D>("Textures/particle_steam");

            UIGaugeFrame = content.Load<Texture2D>("Textures/ui_gauge_frame");
            HUDPanel = content.Load<Texture2D>("Textures/hud_panel");
        }

        // ── Spritesheet helpers ──

        /// <summary>Get the source rectangle for a player animation frame (0-3).</summary>
        public static Rectangle GetPlayerFrame(int frameIndex)
        {
            int fi = frameIndex % PlayerFrameCount;
            return new Rectangle(fi * PlayerFrameWidth, 0, PlayerFrameWidth, PlayerFrameHeight);
        }

        /// <summary>Get the source rectangle for a particle frame (0-5).</summary>
        public static Rectangle GetParticleFrame(int frameIndex)
        {
            int fi = frameIndex % ParticleFrameCount;
            return new Rectangle(fi * ParticleFrameSize, 0, ParticleFrameSize, ParticleFrameSize);
        }

        /// <summary>
        /// Get the source rectangle for a task station icon.
        /// 0=Shovel, 1=Valve, 2=Gauge, 3=Brake
        /// </summary>
        public static Rectangle GetStationIcon(int iconIndex)
        {
            int ii = iconIndex % 4;
            return new Rectangle(ii * StationIconWidth, 0, StationIconWidth, StationIconHeight);
        }
    }
}
