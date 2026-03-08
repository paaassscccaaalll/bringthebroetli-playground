# Bring the Brötli — Demo

A 2.5D MonoGame demo where a Baker character walks, runs, and jumps on a locomotive.


S'wichtige isch in Run_demo.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Python 3 + `pip install Pillow pygame` (for asset tools only)

## Build & Run

```bash
cd Run_demo
dotnet run
```

### Controls

| Key | Action |
|-----|--------|
| WASD | Move |
| Shift | Run |
| Space | Jump |
| F1 | Debug overlay |
| Esc | Quit |

## Project Structure (`Run_demo/`)

### Game Code

| File | Role |
|------|------|
| `Game1.cs` | Main loop — loads content, update/draw, tooltip + debug overlay |
| `Baker.cs` | Player character — walk/run/jump state machine, foot-anchor collision |
| `Train.cs` | Loads locomotive sprite + level JSON, maps image→screen coordinates |
| `AnimationController.cs` | Drives walk (8×12 frames) and jump (8×6 frames) spritesheets |
| `CollisionSystem.cs` | Foot-anchor collision: surface boundary polygon, obstacles, jump barriers |

### Content Pipeline

Textures (`*.png`) go through MGCB (`Content/Content.mgcb`). JSON metadata files are copied as-is (configured in `.csproj`).

| File | Source |
|------|--------|
| `Baker_Walk_Spritesheet.png/.json` | `pack_spritesheet.py` |
| `Baker_Jump_Spritesheet.png/.json` | `pack_spritesheet.py` |
| `locomotive_bounds.json` | `level_editor.py` |
| `baker_bounds.json` | `define_baker_bounds.py` |

### Python Tools (`tools/`)

All tools are standalone scripts run from the `tools/` directory.

**`pack_spritesheet.py`** — Packs per-frame PNGs into Walk + Jump spritesheets.
```bash
python pack_spritesheet.py [path/to/Sprites]
```
Expects `Sprites/Walk/Angle_X/` and `Sprites/Jump/Angle_X/` folders. Outputs to `Content/`.

Done via Blender script...

**`level_editor.py`** — Visual editor for locomotive collision data.
```bash
python level_editor.py
```
Modes (keys 1–5): surface boundary, obstacles, action zones, anchor points, jump barriers. Outputs `locomotive_bounds.json`. Controls: left-click add, right-click delete, Ctrl+S save, Ctrl+Z undo, scroll to zoom.

**`define_baker_bounds.py`** — Marks the Baker's foot anchor on a sprite frame.
```bash
python define_baker_bounds.py
```
Outputs `baker_bounds.json`. TAB switches between foot-anchor and collision-box modes.

## Architecture Notes

- All collision uses a single **foot-anchor point** (not a bounding box). The sprite is drawn with the foot anchor as origin.
- The level editor works in **image-pixel space**. `Train.cs` scales everything to screen space at runtime (`pos * DrawScale + offset`).
- Jump barriers block walking but are passable while airborne.
