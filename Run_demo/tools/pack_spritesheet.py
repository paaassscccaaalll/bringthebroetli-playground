"""
pack_spritesheet.py — Baker Spritesheet Packer
================================================
Reads 8 angle folders from a Sprites/ directory, packs all 12 walk-cycle
frames per angle into a single unified spritesheet PNG plus a JSON metadata
file, and saves them directly into the Run_demo/Content/ folder so the
game picks them up automatically on the next `dotnet run`.

Usage:
    python pack_spritesheet.py [path/to/Sprites] [--output path/to/output]

If no argument is given, the script uses the default Sprites path (see
DEFAULT_SPRITES_DIR below) and outputs into ../Content/ relative to
this script.

Requirements:
    pip install Pillow
"""

import json
import os
import sys
from PIL import Image

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

ANGLE_ORDER:  list[int]  = [0, 45, 90, 135, 180, 225, 270, 315]
FRAMES_PER_ANGLE: int    = 12
OUTPUT_PNG:   str        = "Baker_Spritesheet.png"
OUTPUT_JSON:  str        = "Baker_Spritesheet.json"

# Frame filename template inside each angle folder.
# Example: Walk_A0_F001.png  (angle encoded without leading zeros in folder name)
FILENAME_TEMPLATE = "Walk_A{angle}_F{frame:03d}.png"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def angle_folder_name(angle: int) -> str:
    """Return the folder name for a given angle, e.g. 'Angle_45'."""
    return f"Angle_{angle}"


def load_frame(sprites_dir: str, angle: int, frame: int) -> Image.Image:
    """Load a single frame PNG for the given angle (1-indexed frame)."""
    folder = os.path.join(sprites_dir, angle_folder_name(angle))
    filename = FILENAME_TEMPLATE.format(angle=angle, frame=frame)
    path = os.path.join(folder, filename)
    if not os.path.isfile(path):
        raise FileNotFoundError(
            f"Expected frame file not found:\n  {path}\n"
            f"Check that your renders follow the naming convention "
            f"'Walk_A<angle>_F<NNN>.png' (3-digit zero-padded frame number)."
        )
    return Image.open(path).convert("RGBA")


# ---------------------------------------------------------------------------
# Main packing logic
# ---------------------------------------------------------------------------

# Default Sprites source folder.
# On Windows: the OneDrive renders folder.
# On Linux:   the local Sprites/ folder next to this script.
# Override with the first CLI argument.
_WINDOWS_SPRITES = r"C:\Users\pphol\OneDrive - ETH Zurich\Game Lab\visuals\Sprites"
_LOCAL_SPRITES   = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Sprites")

if os.path.isdir(_LOCAL_SPRITES):
    DEFAULT_SPRITES_DIR = _LOCAL_SPRITES
elif os.path.isdir(_WINDOWS_SPRITES):
    DEFAULT_SPRITES_DIR = _WINDOWS_SPRITES
else:
    DEFAULT_SPRITES_DIR = _LOCAL_SPRITES   # will fail with a clear error later

# Default output directory: Run_demo/Content/ (one level up from tools/).
DEFAULT_OUTPUT_DIR  = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", "Content"))


def pack_spritesheet(sprites_dir: str, output_dir: str | None = None) -> None:
    sprites_dir = os.path.abspath(sprites_dir)
    if output_dir is None:
        output_dir = DEFAULT_OUTPUT_DIR
    output_dir = os.path.abspath(output_dir)
    os.makedirs(output_dir, exist_ok=True)

    print(f"Sprites directory : {sprites_dir}")
    print(f"Output directory  : {output_dir}")
    print()

    # -----------------------------------------------------------------------
    # Step 1 — probe one frame to determine the canonical frame size.
    # -----------------------------------------------------------------------
    probe = load_frame(sprites_dir, ANGLE_ORDER[0], 1)
    frame_w, frame_h = probe.size
    print(f"Detected frame size : {frame_w} x {frame_h} px")

    total_angles    = len(ANGLE_ORDER)
    sheet_w         = frame_w * FRAMES_PER_ANGLE
    sheet_h         = frame_h * total_angles

    print(f"Spritesheet size    : {sheet_w} x {sheet_h} px  "
          f"({total_angles} rows × {FRAMES_PER_ANGLE} cols)")
    print()

    # -----------------------------------------------------------------------
    # Step 2 — allocate the final canvas and paste every frame.
    # -----------------------------------------------------------------------
    sheet = Image.new("RGBA", (sheet_w, sheet_h), (0, 0, 0, 0))

    for row_idx, angle in enumerate(ANGLE_ORDER):
        print(f"  Packing angle {angle:>3}°  (row {row_idx}) ...", end="  ")
        for col_idx in range(FRAMES_PER_ANGLE):
            frame_number = col_idx + 1          # frames are 1-indexed
            frame_img    = load_frame(sprites_dir, angle, frame_number)

            # Validate that every frame matches the probed size.
            if frame_img.size != (frame_w, frame_h):
                raise ValueError(
                    f"Frame size mismatch at angle={angle}, frame={frame_number}: "
                    f"expected {frame_w}×{frame_h}, got {frame_img.size}."
                )

            x = col_idx  * frame_w
            y = row_idx  * frame_h
            sheet.paste(frame_img, (x, y))

        print("done")

    # -----------------------------------------------------------------------
    # Step 3 — save the spritesheet PNG.
    # -----------------------------------------------------------------------
    png_path = os.path.join(output_dir, OUTPUT_PNG)
    sheet.save(png_path, "PNG")
    print(f"\nSaved spritesheet → {png_path}")

    # -----------------------------------------------------------------------
    # Step 4 — save the JSON metadata file.
    # -----------------------------------------------------------------------
    metadata = {
        "frameWidth":     frame_w,
        "frameHeight":    frame_h,
        "totalAngles":    total_angles,
        "framesPerAngle": FRAMES_PER_ANGLE,
        "angleOrder":     ANGLE_ORDER,
        # Convenience mapping: angle → row index (for the renderer)
        "angleToRow":     {str(angle): idx for idx, angle in enumerate(ANGLE_ORDER)},
    }

    json_path = os.path.join(output_dir, OUTPUT_JSON)
    with open(json_path, "w", encoding="utf-8") as f:
        json.dump(metadata, f, indent=2)
    print(f"Saved metadata    → {json_path}")

    # -----------------------------------------------------------------------
    # Summary
    # -----------------------------------------------------------------------
    print()
    print("=" * 60)
    print("  Spritesheet packing complete!")
    print(f"  Final dimensions : {sheet_w} × {sheet_h} px")
    print(f"  Frame size       : {frame_w} × {frame_h} px")
    print(f"  Angles packed    : {total_angles}")
    print(f"  Frames / angle   : {FRAMES_PER_ANGLE}")
    print("=" * 60)


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Pack Baker walk-cycle sprites into a spritesheet.")
    parser.add_argument(
        "sprites",
        nargs="?",
        default=DEFAULT_SPRITES_DIR,
        help=f"Path to the Sprites/ folder (default: {DEFAULT_SPRITES_DIR})",
    )
    parser.add_argument(
        "--output", "-o",
        default=DEFAULT_OUTPUT_DIR,
        help=f"Output directory for PNG + JSON (default: {DEFAULT_OUTPUT_DIR})",
    )
    args = parser.parse_args()

    if not os.path.isdir(args.sprites):
        print(f"[ERROR] Sprites directory not found: {args.sprites}")
        print("")
        print("Usage: python pack_spritesheet.py [path/to/Sprites] [--output path]")
        print(f"Default sprites path: {DEFAULT_SPRITES_DIR}")
        sys.exit(1)

    pack_spritesheet(args.sprites, args.output)
