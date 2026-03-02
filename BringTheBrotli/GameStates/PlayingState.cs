using System;
using System.Collections.Generic;
using BringTheBrotli.Core;
using BringTheBrotli.Players;
using BringTheBrotli.Train;
using BringTheBrotli.UI;
using BringTheBrotli.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BringTheBrotli.GameStates
{
    /// <summary>
    /// The main gameplay state. Runs the train simulation, handles player movement and
    /// task interactions, draws the 2.5D world, and detects station arrivals and win/loss.
    /// Also manages random environmental events.
    /// </summary>
    public class PlayingState : IGameState
    {
        private readonly GameStateManager _stateManager;
        private readonly InputManager _input;
        private readonly TextRenderer _text;
        private readonly Player _player1;
        private readonly Player _player2;
        private readonly TrainSystem _train;
        private readonly TrackScroller _scroller;
        private readonly HUD _hud;

        // Factories for creating other game states
        private readonly Func<TrainSystem, TrackScroller, int, IGameState> _createVoteState;
        private readonly Func<TrainSystem, string, bool, IGameState> _createGameOverState;

        // --- 2.5D Rendering ---
        private readonly Camera _camera;
        private readonly BackgroundRenderer _background;
        private readonly TrainRenderer _trainRenderer;
        private readonly PlayerRenderer _playerRenderer;
        private readonly TaskStationManager _taskManager;
        private readonly ParticleSystem _particles;

        // --- Train cars ---
        private readonly List<TrainCar> _trainCars;
        private float _trainOriginX; // world-space X of the engine's left edge

        // --- Player characters ---
        private readonly PlayerCharacter _pc1;
        private readonly PlayerCharacter _pc2;

        // --- Task station offsets (relative to trainOriginX) ---
        private readonly List<(float offset, TaskStation station)> _stationOffsets = new();

        // --- Random Events ---
        private float _nextEventTimer;
        private float _activeEventTimer;
        private string? _activeEventType;
        private bool _hasImposter;

        private const float EventIntervalNoImposterMin = 15f;
        private const float EventIntervalNoImposterMax = 25f;
        private const float EventIntervalWithImposterMin = 45f;
        private const float EventIntervalWithImposterMax = 60f;
        private const float EventDurationSteamLeak = 10f;
        private const float EventDurationWindGust = 5f;
        private const float EventDurationCoalDamp = 10f;

        private static readonly Random _rng = new();

        // Car layout offsets from trainOriginX
        private const float EngineOffset = 0f;
        private const float EngineWidth = 180f;
        private const float Gap = 10f;
        private const float TenderOffset = EngineWidth + Gap;                  // 190
        private const float TenderWidth = 150f;
        private const float BoilerCarOffset = TenderOffset + TenderWidth + Gap; // 350
        private const float BoilerCarWidth = 200f;
        private const float PassengerCarOffset = BoilerCarOffset + BoilerCarWidth + Gap; // 560
        private const float PassengerCarWidth = 220f;
        private const float TotalTrainLength = PassengerCarOffset + PassengerCarWidth;   // 780

        public PlayingState(GameStateManager stateManager, InputManager input, TextRenderer text,
                             Player player1, Player player2, TrainSystem train, TrackScroller scroller, HUD hud,
                             Func<TrainSystem, TrackScroller, int, IGameState> createVoteState,
                             Func<TrainSystem, string, bool, IGameState> createGameOverState)
        {
            _stateManager = stateManager;
            _input = input;
            _text = text;
            _player1 = player1;
            _player2 = player2;
            _train = train;
            _scroller = scroller;
            _hud = hud;
            _createVoteState = createVoteState;
            _createGameOverState = createGameOverState;

            // --- Initialize 2.5D systems ---
            _camera = new Camera();
            _background = new BackgroundRenderer(text);
            _trainRenderer = new TrainRenderer(text);
            _playerRenderer = new PlayerRenderer(text);
            _taskManager = new TaskStationManager(text);
            _particles = new ParticleSystem();

            // --- Build train cars ---
            _trainCars = BuildTrainCars();

            // --- Build task stations and wire to train systems ---
            BuildTaskStations();

            // --- Player characters start on specific cars ---
            _pc1 = new PlayerCharacter(0, 100f);
            _pc2 = new PlayerCharacter(1, 300f);

            // Store references in Player objects
            _player1.Character = _pc1;
            _player2.Character = _pc2;
        }

        public void Enter()
        {
            _hasImposter = _player1.Role == PlayerRole.Imposter || _player2.Role == PlayerRole.Imposter;
            ScheduleNextEvent();
            _activeEventType = null;
            _activeEventTimer = 0f;

            // Set initial train world position
            _trainOriginX = Constants.CameraLeadOffset;

            // Position train cars
            UpdateTrainCarPositions();

            // Reset player positions relative to train
            _pc1.Reset(_trainOriginX + TenderOffset + TenderWidth / 2f);
            _pc2.Reset(_trainOriginX + BoilerCarOffset + BoilerCarWidth / 2f);

            _background.Reset();
            _particles.Clear();
            _hud.AddEvent("Journey begun! Baden -> Zurich");
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // --- Debug ---
            if (_input.IsKeyJustPressed(Keys.F1))
            {
                Console.WriteLine($"[DEBUG] Speed={_train.Speed:F1} Dist={_train.DistanceTraveled:F2} " +
                                  $"Time={_train.TimeElapsed:F1} Coal={_train.Firebox.CoalLevel:F1} " +
                                  $"Temp={_train.Firebox.Temperature:F1} Water={_train.Boiler.WaterLevel:F1} " +
                                  $"Pressure={_train.Boiler.SteamPressure:F1}");
            }

            if (_input.IsKeyJustPressed(Keys.Escape))
            {
                _hud.AddEvent("(Escape pressed - pausing not implemented yet)");
            }

            // --- Advance train origin based on train speed ---
            float trainAdvance = _train.Speed * 2f * dt;
            _trainOriginX += trainAdvance;
            UpdateTrainCarPositions();

            // --- Camera follows the engine ---
            _camera.WorldX = _trainOriginX - Constants.CameraLeadOffset;

            // --- Player movement (A/D for P1, Left/Right for P2) ---
            float p1Move = 0f;
            if (_input.IsKeyDown(Keys.A)) p1Move -= 1f;
            if (_input.IsKeyDown(Keys.D)) p1Move += 1f;

            float p2Move = 0f;
            if (_input.IsKeyDown(Keys.Left)) p2Move -= 1f;
            if (_input.IsKeyDown(Keys.Right)) p2Move += 1f;

            float trainLeft = _trainOriginX;
            float trainRight = _trainOriginX + TotalTrainLength;

            // Move players relative to the train, then advance with the train
            _pc1.Update(dt, p1Move, trainLeft, trainRight);
            _pc2.Update(dt, p2Move, trainLeft, trainRight);

            // Players ride along with the train
            _pc1.WorldX += trainAdvance;
            _pc2.WorldX += trainAdvance;

            // Re-clamp after train advancement
            _pc1.WorldX = Math.Clamp(_pc1.WorldX, trainLeft + 10f, trainRight - 10f);
            _pc2.WorldX = Math.Clamp(_pc2.WorldX, trainLeft + 10f, trainRight - 10f);

            // --- Task station interactions ---
            bool p1Holding = _input.IsKeyDown(Keys.Space);
            bool p2Holding = _input.IsKeyDown(Keys.Enter);
            _taskManager.Update(dt, _pc1, _pc2, p1Holding, p2Holding, msg => _hud.AddEvent(msg));

            // Sync Player action state for compatibility
            _player1.IsPerformingAction = _pc1.IsPerformingTask;
            _player2.IsPerformingAction = _pc2.IsPerformingTask;

            // --- Braking: Player can use brakes station, or P2 Down as emergency ---
            if (_input.IsKeyDown(Keys.Down))
                _train.IsBraking = true;
            else
                _train.IsBraking = false;

            // --- Update train simulation ---
            string? trainEvent = _train.Update(dt);
            if (trainEvent != null)
                _hud.AddEvent(trainEvent);

            // --- Update scroller & check station arrival ---
            bool stationReached = _scroller.Update(_train.Speed, _train.DistanceTraveled, dt);
            if (stationReached)
            {
                int stationNumber = _scroller.NextStationIndex;
                _hud.AddEvent($"Arriving at station {stationNumber}!");
                _stateManager.SetState(_createVoteState(_train, _scroller, stationNumber));
                return;
            }

            // --- Random events ---
            UpdateRandomEvents(dt);

            // --- Background scroll ---
            _background.Update(_train.Speed, dt);

            // --- Player animation ---
            _playerRenderer.Update(dt);

            // --- Particle system ---
            // Chimney is on the RIGHT (front) of the engine after flipping
            float chimneyX = _trainOriginX + EngineWidth - 40f;
            float chimneyY = Constants.RoofTopY - 35f;
            bool steamLeak = _activeEventType == "SteamLeak";
            // Steam vent on the boiler car (left side after flip)
            float steamVentX = _trainOriginX + BoilerCarOffset + 40f;
            float steamVentY = Constants.RoofTopY - 5f;
            _particles.Update(dt, chimneyX, chimneyY, _train.Speed,
                              steamLeak, steamVentX, steamVentY);

            // --- HUD flash timer ---
            _hud.Update(dt);

            // --- Check win/loss ---
            if (_train.GameOver)
            {
                _stateManager.SetState(_createGameOverState(_train, _train.GameOverReason, _train.CitizensWin));
                return;
            }

            // --- Log critical states ---
            if (_train.Boiler.WaterLevel < 15f && _train.Boiler.WaterLevel > 0.5f)
            {
                if (_train.Boiler.WaterLevel + _train.Boiler.WaterLevel * dt * 0.1f >= 15f)
                    _hud.AddEvent("WARNING: Water level critical!");
            }
            if (_train.Firebox.CoalLevel < 15f && _train.Firebox.CoalLevel > 0.5f)
            {
                if (_train.Firebox.CoalLevel + _train.Firebox.CoalLevel * dt * 0.1f >= 15f)
                    _hud.AddEvent("WARNING: Coal level critical!");
            }
        }

        public void Draw(SpriteBatch sb)
        {
            // --- Crossy-Road-style voxel rendering (back-to-front) ---

            // 1. Sky gradient + clouds
            _background.DrawSky(sb);

            // 2. Distant parallax hills
            _background.DrawHills(sb);

            // 3. Continuous ground plane (grass, dirt, track — fills behind + under + in front of train)
            _background.DrawGround(sb);

            // 4. Voxel train: top faces (visible from above)
            _trainRenderer.DrawRoofLayer(sb, _camera, _trainCars);

            // 5. Details ON the top face (chimney, dome, cab, cargo — voxel sub-blocks)
            _trainRenderer.DrawLocomotiveDetails(sb, _camera, _trainCars[0]);
            _trainRenderer.DrawCargoDetails(sb, _camera, _trainCars[1]);

            // 6. Task station markers on the top face
            _taskManager.DrawMarkers(sb, _camera, _pc1, _pc2);

            // 7. Player characters standing on the top face
            _playerRenderer.Draw(sb, _camera, _pc1, _pc2);

            // 8. Interaction prompts above players
            _taskManager.DrawPrompts(sb, _camera, _pc1, _pc2);

            // 9. Particles (smoke, steam) above the train
            _particles.Draw(sb, _camera);

            // 10. Voxel train: front faces + side faces + wheels (occludes player lower body)
            _trainRenderer.DrawWallLayer(sb, _camera, _trainCars);

            // 11. HUD overlay
            _hud.Draw(sb, _train, _scroller, _pc1, _pc2, _trainOriginX, TotalTrainLength, _taskManager.Stations);
        }

        public void Exit()
        {
            _train.Firebox.CoalDrainMultiplier = 1f;
            _train.Boiler.PressureDrainMultiplier = 1f;
            _train.SpeedPenalty = 0f;
        }

        // ---- Private Methods ----

        private List<TrainCar> BuildTrainCars()
        {
            return new List<TrainCar>
            {
                new("Engine", EngineWidth, new Color(100, 100, 110), texture: TextureAtlas.TrainEngine),
                new("Tender", TenderWidth, new Color(80, 80, 90), texture: TextureAtlas.TrainTender),
                new("BoilerCar", BoilerCarWidth, new Color(120, 90, 70), texture: TextureAtlas.TrainBoilerCar),
                new("PassengerCar", PassengerCarWidth, new Color(130, 50, 50), texture: TextureAtlas.TrainPassengerCar)
            };
        }

        private void BuildTaskStations()
        {
            // Brakes (on Engine)
            var brakes = new TaskStation("Brakes", 0f, 0.5f, Color.Yellow, cooldown: 2f);
            brakes.OnComplete = () => { _train.IsBraking = true; };
            _stationOffsets.Add((EngineOffset + 20f, brakes));
            _taskManager.AddStation(brakes);

            // Shovel Coal (on Coal Tender)
            var shovelCoal = new TaskStation("Shovel Coal", 0f, 1.5f, Color.Orange, cooldown: 3f);
            shovelCoal.OnComplete = () => _train.Firebox.ShovelCoal();
            _stationOffsets.Add((TenderOffset + 30f, shovelCoal));
            _taskManager.AddStation(shovelCoal);

            // Coal Storage Check (on Coal Tender)
            var coalCheck = new TaskStation("Coal Check", 0f, 0.3f, new Color(180, 140, 60), cooldown: 5f);
            coalCheck.OnComplete = () => _hud.AddEvent($"Coal reserve: {_train.Firebox.CoalLevel:F0}%");
            _stationOffsets.Add((TenderOffset + 100f, coalCheck));
            _taskManager.AddStation(coalCheck);

            // Fill Boiler (on Boiler Car)
            var fillBoiler = new TaskStation("Fill Boiler", 0f, 1.5f, Color.Cyan, cooldown: 3f);
            fillBoiler.OnComplete = () => _train.Boiler.FillWater();
            _stationOffsets.Add((BoilerCarOffset + 50f, fillBoiler));
            _taskManager.AddStation(fillBoiler);

            // Pressure Gauge (on Boiler Car)
            var gauge = new TaskStation("Pressure Gauge", 0f, 0.5f, new Color(180, 180, 220), cooldown: 8f);
            gauge.OnComplete = () => _hud.AddEvent($"Auto-vent threshold: {_train.Boiler.AutoVentThreshold:F0}%");
            _stationOffsets.Add((BoilerCarOffset + 100f, gauge));
            _taskManager.AddStation(gauge);

            // Steam Vent (on Boiler Car)
            var steamVent = new TaskStation("Steam Vent", 0f, 0.8f, new Color(200, 200, 255), cooldown: 2f);
            steamVent.OnComplete = () => _train.Boiler.ManualVent();
            _stationOffsets.Add((BoilerCarOffset + 160f, steamVent));
            _taskManager.AddStation(steamVent);
        }

        private void UpdateTrainCarPositions()
        {
            _trainCars[0].WorldX = _trainOriginX + EngineOffset;
            _trainCars[1].WorldX = _trainOriginX + TenderOffset;
            _trainCars[2].WorldX = _trainOriginX + BoilerCarOffset;
            _trainCars[3].WorldX = _trainOriginX + PassengerCarOffset;

            foreach (var (offset, station) in _stationOffsets)
            {
                station.WorldX = _trainOriginX + offset;
            }
        }

        // ---- Random Events ----

        private void ScheduleNextEvent()
        {
            float min = _hasImposter ? EventIntervalWithImposterMin : EventIntervalNoImposterMin;
            float max = _hasImposter ? EventIntervalWithImposterMax : EventIntervalNoImposterMax;
            _nextEventTimer = min + (float)_rng.NextDouble() * (max - min);
        }

        private void UpdateRandomEvents(float dt)
        {
            if (_activeEventType == null)
            {
                _nextEventTimer -= dt;
                if (_nextEventTimer <= 0)
                {
                    TriggerRandomEvent();
                    ScheduleNextEvent();
                }
            }
            else
            {
                _activeEventTimer -= dt;
                if (_activeEventTimer <= 0)
                    EndActiveEvent();
            }
        }

        private void TriggerRandomEvent()
        {
            int choice = _rng.Next(3);
            switch (choice)
            {
                case 0:
                    _activeEventType = "SteamLeak";
                    _activeEventTimer = EventDurationSteamLeak;
                    _train.Boiler.PressureDrainMultiplier = 2f;
                    _hud.AddEvent("Steam leak detected! Pressure draining fast!");
                    break;
                case 1:
                    _activeEventType = "WindGust";
                    _activeEventTimer = EventDurationWindGust;
                    _train.SpeedPenalty = 10f;
                    _hud.AddEvent("Strong headwind! Speed reduced!");
                    break;
                case 2:
                    _activeEventType = "CoalDamp";
                    _activeEventTimer = EventDurationCoalDamp;
                    _train.Firebox.CoalDrainMultiplier = 2f;
                    _hud.AddEvent("Wet coal in the firebox! Coal draining fast!");
                    break;
            }
        }

        private void EndActiveEvent()
        {
            switch (_activeEventType)
            {
                case "SteamLeak":
                    _train.Boiler.PressureDrainMultiplier = 1f;
                    _hud.AddEvent("Steam leak repaired.");
                    break;
                case "WindGust":
                    _train.SpeedPenalty = 0f;
                    _hud.AddEvent("Headwind subsided.");
                    break;
                case "CoalDamp":
                    _train.Firebox.CoalDrainMultiplier = 1f;
                    _hud.AddEvent("Coal dried out.");
                    break;
            }
            _activeEventType = null;
        }
    }
}
