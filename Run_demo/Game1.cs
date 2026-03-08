using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BringTheBrotliDemo
{
    public class Game1 : Game
    {
        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch = null!;
        private GameState _gameState = null!;
        private Train _train = null!;
        private CollisionSystem _collision = null!;
        private PlayerCharacter[] _players = null!;
        private MinigameManager _minigameManager = null!;
        private HUD _hud = null!;
        private DebugOverlay _debugOverlay = null!;
        private SpriteFont _font = null!;
        private Texture2D _pixel = null!;
        private bool _debugMode;
        private bool _f1WasDown;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = ScreenWidth,
                PreferredBackBufferHeight = ScreenHeight,
                IsFullScreen = false
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
        }

        protected override void LoadContent()
        {
            Window.Title = "Bring the Br\u00f6tli";
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            _font = Content.Load<SpriteFont>("DefaultFont");
            _gameState = new GameState();
            _train = new Train();
            _train.LoadContent(Content, ScreenWidth, ScreenHeight);
            _collision = new CollisionSystem();
            _train.PopulateCollision(_collision);
            var walkMeta = LoadMeta("Content/Baker_Walk_Spritesheet.json");
            var walkTex = Content.Load<Texture2D>("Baker_Walk_Spritesheet");
            var jumpMeta = LoadMeta("Content/Baker_Jump_Spritesheet.json");
            var jumpTex = Content.Load<Texture2D>("Baker_Jump_Spritesheet");
            _players = new PlayerCharacter[2];
            for (int i = 0; i < 2; i++)
            {
                var anim = new AnimationController(
                    walkTex, walkMeta.FrameWidth, walkMeta.FrameHeight, walkMeta.FramesPerAngle,
                    jumpTex, jumpMeta.FrameWidth, jumpMeta.FrameHeight, jumpMeta.FramesPerAngle);
                var input = i == 0 ? PlayerInput.Player1 : PlayerInput.Player2;
                _players[i] = new PlayerCharacter(i, input, anim, "Content/baker_bounds.json");
                if (i == 1)
                    _players[i].Tint = new Color(180, 200, 255);
            }

            Vector2? spawn = _train.GetAnchorPoint("spawn");
            if (spawn.HasValue)
            {
                _players[0].Position = spawn.Value;
                _players[1].Position = spawn.Value + new Vector2(40, 0);
            }
            var registry = new MinigameRegistry();
            registry.Register("load_coal", b => new PlaceholderMinigame("Load Coal", ResourceType.Coal, 2, b));
            registry.Register("load_water", b => new PlaceholderMinigame("Load Water", ResourceType.Water, 2, b));
            registry.Register("vent_steam", b => new PlaceholderMinigame("Vent Steam", ResourceType.Steam, -3, b));
            _minigameManager = new MinigameManager(registry, _collision);
            _hud = new HUD(_gameState);
            _debugOverlay = new DebugOverlay(_collision);
        }

        protected override void Update(GameTime gameTime)
        {
            var kb = Keyboard.GetState();
            if (kb.IsKeyDown(Keys.Escape)) Exit();
            bool f1Down = kb.IsKeyDown(Keys.F1);
            if (f1Down && !_f1WasDown) _debugMode = !_debugMode;
            _f1WasDown = f1Down;
            if (_gameState.CurrentPhase == GamePhase.Gameplay)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                foreach (var player in _players)
                {
                    if (!_minigameManager.IsPlayerInMinigame(player.PlayerIndex))
                    {
                        player.Update(gameTime, kb, _collision);
                        if (player.InteractPressed && player.CurrentZoneLabel != null)
                        {
                            string zone = player.CurrentZoneLabel;
                            if (string.Equals(zone, "burn_coal", StringComparison.OrdinalIgnoreCase))
                                GameRules.ProcessBurnCoal(_gameState, player.PlayerIndex);
                            else if (string.Equals(zone, "pour_water", StringComparison.OrdinalIgnoreCase))
                                GameRules.ProcessPourWater(_gameState, player.PlayerIndex);
                            else
                                _minigameManager.TryStartMinigame(
                                    player.PlayerIndex, zone, _gameState);
                        }
                    }
                }
                if (_players[0].JumpState == JumpState.Grounded &&
                    _players[1].JumpState == JumpState.Grounded)
                {
                    CollisionSystem.ResolvePlayerCollision(
                        ref _players[0].Position, ref _players[1].Position,
                        GameConstants.PlayerCollisionRadius);
                }
                _minigameManager.Update(gameTime, _gameState);
                GameRules.UpdateContinuous(_gameState, dt);
                if (GameRules.CheckWinConditions(_gameState) != GameResult.InProgress)
                    _gameState.CurrentPhase = GamePhase.GameOver;
            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(40, 40, 50));
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            _train.Draw(_spriteBatch);
            foreach (var player in _players)
                player.Draw(_spriteBatch);
            _hud.DrawTooltips(_spriteBatch, _font, _pixel, _players);
            _hud.Draw(_spriteBatch, _font, _pixel);
            _minigameManager.Draw(_spriteBatch, _font, _pixel);
            if (_gameState.Strikes >= GameConstants.MaxStrikes)
                _spriteBatch.Draw(_pixel,
                    new Rectangle(0, 0, ScreenWidth, ScreenHeight),
                    new Color(255, 0, 0, 40));
            if (_debugMode)
                _debugOverlay.Draw(_spriteBatch, _pixel, _font, _players);
            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private static SpritesheetMeta LoadMeta(string path)
        {
            string full = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            string json = File.ReadAllText(full);
            return JsonSerializer.Deserialize<SpritesheetMeta>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private struct SpritesheetMeta
        {
            public int FrameWidth { get; set; }
            public int FrameHeight { get; set; }
            public int FramesPerAngle { get; set; }
        }
    }
}
