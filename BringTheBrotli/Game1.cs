using BringTheBrotli.Core;
using BringTheBrotli.GameStates;
using BringTheBrotli.Players;
using BringTheBrotli.Train;
using BringTheBrotli.UI;
using BringTheBrotli.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotli
{
    /// <summary>
    /// Main game class. Initializes all systems and delegates to GameStateManager.
    /// Kept intentionally thin — logic lives in game states and systems.
    /// </summary>
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch = null!;

        // Core systems
        private InputManager _input = null!;
        private GameStateManager _stateManager = null!;
        private TextRenderer _text = null!;

        // Game objects (shared across states)
        private Player _player1 = null!;
        private Player _player2 = null!;
        private TrainSystem _train = null!;
        private TrackScroller _scroller = null!;
        private VoteManager _votes = null!;
        private HUD _hud = null!;
        private VoteScreen _voteScreen = null!;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set resolution to 1280x720 windowed
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.IsFullScreen = false;
        }

        protected override void Initialize()
        {
            Window.Title = "Bring the Br\u00f6tli \u2014 Prototype v0.1";

            _input = new InputManager();
            _stateManager = new GameStateManager();

            _player1 = new Player(0);
            _player2 = new Player(1);
            _train = new TrainSystem();
            _scroller = new TrackScroller();
            _votes = new VoteManager();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load the sprite font compiled from Content/DefaultFont.spritefont
            SpriteFont font;
            try
            {
                font = Content.Load<SpriteFont>("DefaultFont");
            }
            catch
            {
                // Fallback: create a minimal font-like setup.
                // If the font fails to load (e.g. no build tools), we still need to run.
                // We'll create a dummy — this should not normally happen with a proper MGCB build.
                System.Console.WriteLine("[WARN] Could not load DefaultFont. Text will not render properly.");
                font = null!;
            }

            _text = new TextRenderer(font, GraphicsDevice);
            _hud = new HUD(_text);
            _voteScreen = new VoteScreen(_text);

            // Load all sprite textures
            TextureAtlas.LoadAll(Content);

            // Start at the main menu
            _stateManager.SetState(CreateMainMenuState());
        }

        protected override void Update(GameTime gameTime)
        {
            _input.Update();
            _stateManager.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                               SamplerState.PointClamp, null, null);

            _stateManager.Draw(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        // ── State Factory Methods ──
        // These create fresh state instances with all shared dependencies injected.

        private IGameState CreateMainMenuState()
        {
            return new MainMenuState(_stateManager, _input, _text, _player1, _player2,
                () => CreatePlayingState());
        }

        private IGameState CreatePlayingState()
        {
            // Reset systems for a fresh run
            _train.Reset();
            _scroller.Reset();
            _player1.ResetAction();
            _player2.ResetAction();
            _hud.ClearEvents();

            return new PlayingState(_stateManager, _input, _text, _player1, _player2,
                _train, _scroller, _hud,
                (train, scroller, stationNum) => CreateStationVoteState(train, scroller, stationNum),
                (train, reason, win) => CreateGameOverState(train, reason, win));
        }

        private IGameState CreateStationVoteState(TrainSystem train, TrackScroller scroller, int stationNumber)
        {
            return new StationVoteState(_stateManager, _input, _text, _player1, _player2,
                _votes, _voteScreen, train, scroller, _hud, stationNumber,
                () => CreateResumePlayingState(),
                (t, reason, win) => CreateGameOverState(t, reason, win));
        }

        /// <summary>
        /// Creates a PlayingState that RESUMES the current train/scroller state
        /// (does NOT reset systems — continues from where we left off).
        /// </summary>
        private IGameState CreateResumePlayingState()
        {
            _player1.ResetAction();
            _player2.ResetAction();

            return new PlayingState(_stateManager, _input, _text, _player1, _player2,
                _train, _scroller, _hud,
                (train, scroller, stationNum) => CreateStationVoteState(train, scroller, stationNum),
                (train, reason, win) => CreateGameOverState(train, reason, win));
        }

        private IGameState CreateGameOverState(TrainSystem train, string reason, bool citizensWin)
        {
            return new GameOverState(_stateManager, _input, _text, train, reason, citizensWin,
                () => CreateMainMenuState());
        }
    }
}
