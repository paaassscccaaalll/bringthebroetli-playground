# Bring the Brötli — Architecture

---

## Folder Structure

```
Run_demo/
├── Game1.cs                         
├── Program.cs                       # Entry point
│
├── Core/                            
│   ├── GameState.cs                 # All mutable game state
│   ├── GameRules.cs                 # Continuous physics + instant actions
│   ├── GamePhase.cs                 # enum: Gameplay, GameOver
│   ├── GameConstants.cs             # Named constants
│   ├── PlayerInventory.cs           # Per-player carried resources
│   ├── ResourceType.cs              # enum: Coal, Water, Steam
│   └── ZoneLabels.cs                # Compile-time zone label constants
│
├── Players/                         # Character controllers + input
│   ├── PlayerCharacter.cs           # Movement, jump, collision, zone detection
│   └── PlayerInput.cs               # Key binding configuration
│
├── Minigames/                       # Extensible minigame system
│   ├── IMinigame.cs                 # Interface — the extension point
│   ├── MinigameResult.cs            # Result value type
│   ├── MinigameRegistry.cs          # Zone label → factory map
│   ├── MinigameManager.cs           # Active minigame lifecycle
│   └── PlaceholderMinigame.cs       # Placeholder minigame implementation
│
├── World/                           # Train + collision
│   ├── Train.cs                     # Sprite + level data loading
│   └── CollisionSystem.cs           # Foot-anchor collision + action zones
│
├── UI/                              # All HUD/UI rendering
│   ├── HUD.cs                                        
│
├── Rendering/                       # Visual utilities
│   ├── AnimationController.cs       # 8-directional spritesheet animation
│   ├── DebugOverlay.cs              # F1 debug visualization
│   ├── DepthSorter.cs               # Painter's algorithm depth sorting
│   ├── DrawHelpers.cs               # Pixel/line/rect drawing utilities
│   └── IDepthSortable.cs            # Interface for depth-sorted world objects
│
└── Content/                         # Assets
    ├── Content.mgcb
    ├── locomotive_bounds.json        # Level editor output (surface, zones, etc.)
    ├── baker_bounds.json             # Character foot anchor
    ├── Baker_Walk_Spritesheet.json   # Walk animation metadata
    ├── Baker_Jump_Spritesheet.json   # Jump animation metadata
    └── DefaultFont.spritefont
```

All C# files share the `BringTheBrotliDemo` namespace.
Folders are for organization only — no sub-namespaces.

---

## Core Class Responsibilities

### Game1.cs — Orchestrator
Thin MonoGame Game subclass. Creates all subsystems in LoadContent.
Routes Update/Draw to the correct subsystem based on GamePhase.
Contains no game logic, no UI logic, no rendering logic beyond delegation.
Handles the distinction between minigame zones and instant interaction zones:
- Minigame zones (load_coal, load_water, vent_steam) → MinigameManager
- Instant zones (burn_coal, pour_water) → GameRules directly

### Core/GameState.cs — Pure Game State
Float resources (Coal, Water, Steam), Strikes, StrikeDanger accumulator,
TimeRemaining, TrainProgress, CurrentPhase, and per-player Inventories.
Provides Reset() to return to initial values.
Fully unit-testable in isolation without any MonoGame assemblies.

### Core/GameRules.cs — Pure Game Logic
**UpdateContinuous(state, dt)**: Furnace converts Coal+Water → Steam at a rate.
Strike danger accumulates when coal burns without water.
Steam drives TrainProgress. Coal and Steam decay over time.

**ProcessBurnCoal(state, playerIndex)**: Instant action. Deposits player's
carried coal into the train's furnace (global Coal).

**ProcessPourWater(state, playerIndex)**: Instant action. Deposits player's
carried water into the train's boiler (global Water).

**CheckWinConditions(state)**: Returns GameResult.

### Core/PlayerInventory.cs — Per-Player Resources
Dictionary-backed inventory keyed by `ResourceType`. Provides `Get(type)`,
`Set(type, amount)`, and `Add(type, delta, max)`. Automatically supports
new resource types without adding fields.

### Players/PlayerCharacter.cs — Character Controller
Refactored from Baker.cs. Takes a PlayerInput configuration instead of hardcoded keys.
Preserves the exact same jump physics, state machine, and foot-anchor collision system.
Supports per-player tinting for visual distinction. Tracks InteractPressed for zone triggers.

### Players/PlayerInput.cs — Input Configuration
Defines key bindings per player via a simple value class. Static factory properties
for Player1 (WASD/Space/E) and Player2 (Arrows/Enter/RCtrl). Extensible to gamepad
or network input by adding new presets.

### World/Train.cs — Train World
Loads the locomotive sprite and level-editor JSON (locomotive_bounds.json).
Maps all coordinates from image-pixel space to screen space using DrawScale + DrawPosition.
Exposes surface boundary, obstacles, action zones, anchor points, and jump barriers.

### World/CollisionSystem.cs — Collision
Foot-anchor point collision. Constrains movement to the surface boundary polygon,
pushes out of obstacles and jump barriers, detects action zone occupancy.
Separate resolution for grounded vs airborne movement. Axis-separation for wall sliding.
Also handles player-to-player collision via circle push-apart.

### Minigames/IMinigame.cs — Minigame Interface
The primary extensibility point. Defines the contract every minigame must implement:
Start, Update, Draw, IsComplete, Result, OverlayBounds. This is what new developers
implement to add gameplay content.

### Minigames/MinigameManager.cs — Minigame Lifecycle
Manages per-player active minigames. Each player can have their own independent
minigame running simultaneously. On trigger: looks up zone bounds, computes overlay
position, creates the minigame via MinigameRegistry. While a player is in a minigame,
their movement is frozen but the other player continues playing normally.
On completion: applies the MinigameResult to the player's inventory (Coal/Water)
or to global state (Steam for venting). Venting steam to 0 triggers a Strike.

### Minigames/MinigameRegistry.cs — Factory Map
Simple dictionary mapping action zone labels to minigame factory functions.
Uses case-insensitive string comparison. The only place that needs modification
when adding a new minigame (one line of registration).

### Rendering/AnimationController.cs — Spritesheet Animation
Manages 8-directional walk and jump animation from spritesheets. Handles angle
detection from movement direction, frame cycling at configurable FPS, and source
rectangle computation. Supports direct frame control for jump phases.

### Rendering/DebugOverlay.cs — Debug Visualization
F1-toggle overlay showing collision boundaries, obstacles, jump barriers,
per-player foot anchors, predicted landing positions, and jump state text.

### Rendering/IDepthSortable.cs + DepthSorter.cs — Depth Sorting
Interface (`IDepthSortable`) requiring a `DepthY` float and a `Draw` method.
`DepthSorter.DrawSorted()` sorts any collection of world objects by foot-anchor
Y position (painter's algorithm) so closer objects draw on top. PlayerCharacter
implements IDepthSortable; future items/NPCs do the same.

### Core/ZoneLabels.cs — Zone Label Constants
Static class with `const string` for every action zone and anchor label
used in the level editor JSON. Eliminates raw string duplication across
Game1 and minigame registration.

---

## Key Interfaces

### IMinigame

```csharp
public interface IMinigame
{
    void Start();
    void Update(GameTime gameTime);
    void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel);
    bool IsComplete { get; }
    MinigameResult Result { get; }
    Rectangle OverlayBounds { get; }
}
```

### MinigameResult

```csharp
public struct MinigameResult
{
    public ResourceType ResourceType;
    public int ResourceDelta;
    public MinigameResult(ResourceType type, int delta);
}
```

### GameState

```csharp
public class GameState
{
    public const int PlayerCount = 2;
    public float Coal, Water, Steam;
    public int Strikes;
    public float StrikeDanger;
    public float TimeRemaining, TrainProgress;
    public GamePhase CurrentPhase;
    public PlayerInventory[] Inventories;
    public void Reset();
}
```

### PlayerInventory (Pure C#)

```csharp
public class PlayerInventory
{
    public int Get(ResourceType type);
    public void Set(ResourceType type, int amount);
    public void Add(ResourceType type, int delta, int max);
    public void Reset();
}
```

### GameRules

```csharp
public static class GameRules
{
    public static void UpdateContinuous(GameState state, float dt);
    public static void ProcessBurnCoal(GameState state, int playerIndex);
    public static void ProcessPourWater(GameState state, int playerIndex);
    public static GameResult CheckWinConditions(GameState state);
}
```

### PlayerInput

```csharp
public class PlayerInput
{
    public Keys Up, Down, Left, Right, Run, Jump, Interact;
    public static PlayerInput Player1 { get; }
    public static PlayerInput Player2 { get; }
}
```

---

## How to Add a New Minigame (3 Steps)

### Step 1: Implement IMinigame

Create a new file in `Minigames/`, e.g. `CoalShovelMinigame.cs`:

```csharp
public class CoalShovelMinigame : IMinigame
{
    private readonly Rectangle _overlayBounds;
    private bool _complete;

    public CoalShovelMinigame(Rectangle overlayBounds)
    {
        _overlayBounds = overlayBounds;
    }

    public bool IsComplete => _complete;
    public MinigameResult Result { get; private set; }
    public Rectangle OverlayBounds => _overlayBounds;

    public void Start()
    {
        _complete = false;
    }

    public void Update(GameTime gameTime)
    {
        // Your minigame logic here
        // When done:
        _complete = true;
        Result = new MinigameResult(ResourceType.Coal, coalGained);
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        // Draw your minigame UI within _overlayBounds
    }
}
```

### Step 2: Register in Game1.LoadContent

Add one line to the registry setup in `Game1.LoadContent()`:

```csharp
registry.Register(ZoneLabels.LoadCoal, b => new CoalShovelMinigame(b));
```

### Step 3: Done

No changes needed to Game1.Update, Game1.Draw, MinigameManager, HUD, or any other file.
The MinigameManager automatically handles lifecycle and result application.

---

## Two-Player Input Design

Both players share the same keyboard with non-overlapping key regions:

| Action    | Player 1 (left)  | Player 2 (right)  |
|-----------|-------------------|--------------------|
| Move      | W / A / S / D     | ↑ / ← / ↓ / →     |
| Run       | Left Shift        | Right Shift        |
| Jump      | Space             | Enter              |
| Interact  | E                 | Right Control      |

Implemented via `PlayerInput` configuration class. Both players use
identical `PlayerCharacter` instances differentiated only by their
`PlayerInput` preset and an optional color tint.

Adding gamepad, network, or AI input means creating a new `PlayerInput`
preset — no changes to PlayerCharacter or Game1.

Character visuals are fully decoupled: the `AnimationController`
takes spritesheet textures as constructor arguments. To add a new
character skin, provide different walk/jump spritesheets and a new
baker_bounds.json with the foot anchor. Zero code changes needed.

### Steam → Velocity → Progress

```
Every frame:
  velocity = Steam * TrainSpeedPerSteam
  TrainProgress += velocity * dt
  Steam -= SteamDecayRate * dt  (cooling)
  Coal -= CoalDecayRate * dt    (decay)
```

### Action Zone Types

| Zone Label    | Type     | Effect                                |
|---------------|----------|---------------------------------------|
| load_coal     | Minigame | Player receives coal in inventory     |
| load_water    | Minigame | Player receives water in inventory    |
| vent_steam    | Minigame | Removes steam from train              |
| burn_coal     | Instant  | Deposits carried coal into furnace    |
| pour_water    | Instant  | Deposits carried water into boiler    |

## Coding Conventions

- **Namespace**: `BringTheBrotliDemo` for all files (folders for organization only)
- **Naming**: PascalCase for public, _camelCase for private fields
- **Constants**: Named via `GameConstants` — no magic numbers in logic
- **Dependencies**: GameState and GameRules must never reference MonoGame
- **Composition over inheritance**: Except IMinigame implementations

---
