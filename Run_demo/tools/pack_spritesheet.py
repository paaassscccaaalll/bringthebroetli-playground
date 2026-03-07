"""
pack_spritesheet.py — Baker Spritesheet Packer
================================================
Packs separate spritesheets for each animation type (Walk, Jump, etc.).

Each spritesheet has 8 rows (one per angle) × N frames per row.
Outputs:
  Content/Baker_Walk_Spritesheet.png  + .json  (8 × 12)
  Content/Baker_Jump_Spritesheet.png  + .json  (8 × 6)

Usage:
    python pack_spritesheet.py [path/to/Sprites]

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

ANGLE_ORDER: list[int] = [0, 45, 90, 135, 180, 225, 270, 315]

# Each animation to pack: (subfolder, prefix, frames_per_angle, output_base)
ANIMATIONS = [
    {
        "subfolder":   "Walk",
        "prefix":      "Walk",
        "frames":      12,
        "out_png":     "Baker_Walk_Spritesheet.png",
        "out_json":    "Baker_Walk_Spritesheet.json",
    },
    {
        "subfolder":   "Jump",
        "prefix":      "Jump",
        "frames":      6,
        "out_png":     "Baker_Jump_Spritesheet.png",
        "out_json":    "Baker_Jump_Spritesheet.json",
    },
]

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def angle_folder_name(angle: int) -> str:
    return f"Angle_{angle}"


def load_frame(base_dir: str, subfolder: str, prefix: str,
               angle: int, frame: int) -> Image.Image:
    """Load a single frame PNG.  frame is 1-indexed."""
    folder = os.path.join(base_dir, subfolder, angle_folder_name(angle))
    filename = f"{prefix}_A{angle}_F{frame:03d}.png"
    path = os.path.join(folder, filename)
    if not os.path.isfile(path):
        raise FileNotFoundError(
            f"Expected frame not found:\n  {path}\n"
            f"Naming convention: {prefix}_A<angle>_F<NNN>.png"
        )
    return Image.open(path).convert("RGBA")


# ---------------------------------------------------------------------------
# Default paths
# ---------------------------------------------------------------------------

_WINDOWS_SPRITES = r"C:\Users\pphol\OneDrive - ETH Zurich\Game Lab\visuals\Sprites"
_LOCAL_SPRITES   = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Sprites")

if os.path.isdir(_LOCAL_SPRITES):
    DEFAULT_SPRITES_DIR = _LOCAL_SPRITES
elif os.path.isdir(_WINDOWS_SPRITES):
    DEFAULT_SPRITES_DIR = _WINDOWS_SPRITES
else:
    DEFAULT_SPRITES_DIR = _LOCAL_SPRITES

DEFAULT_OUTPUT_DIR = os.path.normpath(
    os.path.join(os.path.dirname(__file__), "..", "Content"))


# ---------------------------------------------------------------------------
# Packing logic
# ---------------------------------------------------------------------------

def pack_one(sprites_dir: str, output_dir: str, anim: dict) -> None:
    """Pack a single animation into its own spritesheet."""
    subfolder      = anim["subfolder"]
    prefix         = anim["prefix"]
    frames_per_ang = anim["frames"]
    out_png        = anim["out_png"]
    out_json       = anim["out_json"]

    print(f"\n{'='*60}")
    print(f"  Packing: {subfolder}  ({frames_per_ang} frames × {len(ANGLE_ORDER)} angles)")
    print(f"{'='*60}")

    # Probe first frame for dimensions.
    probe = load_frame(sprites_dir, subfolder, prefix, ANGLE_ORDER[0], 1)
    frame_w, frame_h = probe.size
    print(f"  Frame size  : {frame_w} × {frame_h} px")

    total_angles = len(ANGLE_ORDER)
    sheet_w = frame_w * frames_per_ang
    sheet_h = frame_h * total_angles
    print(f"  Sheet size  : {sheet_w} × {sheet_h} px")

    sheet = Image.new("RGBA", (sheet_w, sheet_h), (0, 0, 0, 0))

    for row_idx, angle in enumerate(ANGLE_ORDER):
        print(f"    Angle {angle:>3}°  (row {row_idx}) ...", end="  ")
        for col_idx in range(frames_per_ang):
            frame_img = load_frame(sprites_dir, subfolder, prefix,
                                   angle, col_idx + 1)
            if frame_img.size != (frame_w, frame_h):
                raise ValueError(
                    f"Size mismatch at {subfolder}/angle={angle}, "
                    f"frame={col_idx+1}: expected {frame_w}×{frame_h}, "
                    f"got {frame_img.size}")
            sheet.paste(frame_img, (col_idx * frame_w, row_idx * frame_h))
        print("done")

    # Save PNG.
    png_path = os.path.join(output_dir, out_png)
    sheet.save(png_path, "PNG")
    print(f"  Saved PNG   → {png_path}")

    # Save JSON metadata.
    metadata = {
        "frameWidth":     frame_w,
        "frameHeight":    frame_h,
        "totalAngles":    total_angles,
        "framesPerAngle": frames_per_ang,
        "angleOrder":     ANGLE_ORDER,
    }
    json_path = os.path.join(output_dir, out_json)
    with open(json_path, "w", encoding="utf-8") as f:
        json.dump(metadata, f, indent=2)
    print(f"  Saved JSON  → {json_path}")
    print(f"  Final: {sheet_w} × {sheet_h} px  "
          f"({total_angles} rows × {frames_per_ang} cols)")


def pack_all(sprites_dir: str, output_dir: str | None = None) -> None:
    sprites_dir = os.path.abspath(sprites_dir)
    if output_dir is None:
        output_dir = DEFAULT_OUTPUT_DIR
    output_dir = os.path.abspath(output_dir)
    os.makedirs(output_dir, exist_ok=True)

    print(f"Sprites directory : {sprites_dir}")
    print(f"Output directory  : {output_dir}")

    for anim in ANIMATIONS:
        anim_path = os.path.join(sprites_dir, anim["subfolder"])
        if not os.path.isdir(anim_path):
            print(f"\n  [SKIP] {anim['subfolder']}/ not found at {anim_path}")
            continue
        pack_one(sprites_dir, output_dir, anim)

    print(f"\n{'='*60}")
    print("  All spritesheets packed!")
    print(f"{'='*60}")


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(
        description="Pack Baker animation sprites into spritesheets.")
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
        sys.exit(1)

    pack_all(args.sprites, args.output)
