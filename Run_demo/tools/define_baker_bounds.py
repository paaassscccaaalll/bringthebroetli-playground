#!/usr/bin/env python3
"""
define_baker_bounds.py — Mark the Baker's foot anchor + collision box
=============================================================================

Opens one frame from the Baker spritesheet and lets you:
  1. Click the exact position of the character's feet (foot anchor).
  2. Draw a collision rectangle around the character's footprint.

Both are saved to baker_bounds.json so the MonoGame game uses them for
draw origin and polygon collision respectively.

Modes
-----
  The tool starts in FOOT ANCHOR mode (press TAB to switch).

  FOOT ANCHOR mode:
    LEFT CLICK    – set the foot anchor (replaces previous)
    RIGHT CLICK   – clear the foot anchor

  COLLISION BOX mode:
    LEFT CLICK    – first click sets one corner, second click sets the
                    opposite corner of the collision rectangle
    RIGHT CLICK   – clear the box

  Common:
    TAB           – toggle between the two modes
    LEFT / RIGHT  – cycle animation frames
    UP / DOWN     – cycle angle rows
    ENTER         – save to JSON and quit
    ESC           – quit without saving

Requirements
------------
  pip install pygame

Usage
-----
  cd Run_demo/tools
  .venv/bin/python define_baker_bounds.py

Output: ../Content/baker_bounds.json
"""

import json
import os
import sys

try:
    import pygame
    from pygame.locals import (
        QUIT, KEYDOWN, MOUSEBUTTONDOWN,
        K_RETURN, K_KP_ENTER, K_ESCAPE,
        K_LEFT, K_RIGHT, K_UP, K_DOWN, K_TAB,
    )
except ImportError:
    print("ERROR: Pygame is required.  Install it with:\n  pip install pygame")
    sys.exit(1)

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
SCRIPT_DIR  = os.path.dirname(os.path.abspath(__file__))
CONTENT_DIR = os.path.join(SCRIPT_DIR, "..", "Content")
IMAGE_PATH  = os.path.join(CONTENT_DIR, "Baker_Spritesheet.png")
META_PATH   = os.path.join(CONTENT_DIR, "Baker_Spritesheet.json")
OUTPUT_PATH = os.path.join(CONTENT_DIR, "baker_bounds.json")

# ---------------------------------------------------------------------------
# Visual settings
# ---------------------------------------------------------------------------
BG_COLOR      = (30, 30, 30)
GRID_COLOR    = (60, 60, 60)
CROSS_COLOR   = (255, 50, 50)
BOX_COLOR     = (50, 255, 100)     # green for collision box
BOX_FILL      = (50, 255, 100, 60)
TEXT_COLOR    = (220, 220, 220)
FRAME_BORDER  = (100, 200, 255)
MODE_FOOT     = 0
MODE_BOX      = 1
ZOOM          = 1.5      # display zoom so you can see detail


def main() -> None:
    # ---- Load metadata -------------------------------------------------
    if not os.path.isfile(META_PATH):
        print(f"ERROR: Metadata not found at:\n  {META_PATH}")
        sys.exit(1)

    with open(META_PATH, "r") as f:
        meta = json.load(f)

    frame_w = meta["frameWidth"]       # 512
    frame_h = meta["frameHeight"]      # 512
    total_angles = meta["totalAngles"] # 8
    frames_per   = meta["framesPerAngle"]  # 12

    # ---- Load spritesheet -----------------------------------------------
    if not os.path.isfile(IMAGE_PATH):
        print(f"ERROR: Spritesheet not found at:\n  {IMAGE_PATH}")
        sys.exit(1)

    pygame.init()
    sheet = pygame.image.load(IMAGE_PATH)

    display_w = int(frame_w * ZOOM)
    display_h = int(frame_h * ZOOM)
    win_w = display_w + 20
    win_h = display_h + 80       # room for two status lines
    screen = pygame.display.set_mode((win_w, win_h))
    pygame.display.set_caption("Define Baker Bounds — TAB to switch mode, ENTER save")
    font = pygame.font.SysFont("monospace", 15)

    # State
    current_angle = 0    # row index
    current_frame = 0    # column index
    foot_point: tuple[int, int] | None = None   # in frame-local pixel coords

    # Collision box: two corners in frame-local pixel coords
    box_corner1: tuple[int, int] | None = None
    box_corner2: tuple[int, int] | None = None

    mode = MODE_FOOT  # current editing mode

    pad_x = (win_w - display_w) // 2
    pad_y = 10

    running = True
    while running:
        for event in pygame.event.get():
            if event.type == QUIT:
                running = False

            elif event.type == KEYDOWN:
                if event.key == K_ESCAPE:
                    running = False

                elif event.key in (K_RETURN, K_KP_ENTER):
                    save_json(foot_point, box_corner1, box_corner2, frame_w, frame_h)
                    running = False

                elif event.key == K_TAB:
                    mode = MODE_BOX if mode == MODE_FOOT else MODE_FOOT

                elif event.key == K_RIGHT:
                    current_frame = (current_frame + 1) % frames_per
                elif event.key == K_LEFT:
                    current_frame = (current_frame - 1) % frames_per
                elif event.key == K_DOWN:
                    current_angle = (current_angle + 1) % total_angles
                elif event.key == K_UP:
                    current_angle = (current_angle - 1) % total_angles

            elif event.type == MOUSEBUTTONDOWN:
                mx, my = event.pos
                fx = (mx - pad_x) / ZOOM
                fy = (my - pad_y) / ZOOM
                in_frame = 0 <= fx < frame_w and 0 <= fy < frame_h

                if event.button == 1 and in_frame:   # LEFT CLICK
                    pt = (int(round(fx)), int(round(fy)))
                    if mode == MODE_FOOT:
                        foot_point = pt
                    else:  # MODE_BOX
                        if box_corner1 is None:
                            box_corner1 = pt
                            box_corner2 = None
                        else:
                            box_corner2 = pt

                elif event.button == 3:  # RIGHT CLICK — clear
                    if mode == MODE_FOOT:
                        foot_point = None
                    else:
                        box_corner1 = None
                        box_corner2 = None

        # ---- Draw -------------------------------------------------------
        screen.fill(BG_COLOR)

        # Extract current frame from spritesheet.
        src_rect = pygame.Rect(
            current_frame * frame_w,
            current_angle * frame_h,
            frame_w, frame_h
        )
        frame_surf = sheet.subsurface(src_rect)
        scaled = pygame.transform.scale(frame_surf, (display_w, display_h))

        # Checkerboard background to show transparency.
        checker_size = 16
        for cy in range(0, display_h, checker_size):
            for cx in range(0, display_w, checker_size):
                shade = 45 if ((cx // checker_size) + (cy // checker_size)) % 2 == 0 else 55
                pygame.draw.rect(screen, (shade, shade, shade),
                                 (pad_x + cx, pad_y + cy, checker_size, checker_size))

        screen.blit(scaled, (pad_x, pad_y))

        # Frame border.
        pygame.draw.rect(screen, FRAME_BORDER, (pad_x, pad_y, display_w, display_h), 1)

        # Draw foot anchor (always visible).
        if foot_point is not None:
            sx = int(foot_point[0] * ZOOM) + pad_x
            sy = int(foot_point[1] * ZOOM) + pad_y
            # Crosshair
            pygame.draw.line(screen, CROSS_COLOR, (sx - 12, sy), (sx + 12, sy), 2)
            pygame.draw.line(screen, CROSS_COLOR, (sx, sy - 12), (sx, sy + 12), 2)
            pygame.draw.circle(screen, CROSS_COLOR, (sx, sy), 6, 2)

        # Draw collision box (always visible).
        if box_corner1 is not None:
            c1_sx = int(box_corner1[0] * ZOOM) + pad_x
            c1_sy = int(box_corner1[1] * ZOOM) + pad_y
            if box_corner2 is not None:
                c2_sx = int(box_corner2[0] * ZOOM) + pad_x
                c2_sy = int(box_corner2[1] * ZOOM) + pad_y
                rx = min(c1_sx, c2_sx)
                ry = min(c1_sy, c2_sy)
                rw = abs(c2_sx - c1_sx)
                rh = abs(c2_sy - c1_sy)
                # Semi-transparent fill.
                box_surf = pygame.Surface((rw, rh), pygame.SRCALPHA)
                box_surf.fill((50, 255, 100, 40))
                screen.blit(box_surf, (rx, ry))
                pygame.draw.rect(screen, BOX_COLOR, (rx, ry, rw, rh), 2)
            else:
                # Only first corner placed — draw a small marker.
                pygame.draw.circle(screen, BOX_COLOR, (c1_sx, c1_sy), 5, 2)

        # Status bar.
        angle_degs = meta.get("angleOrder", list(range(0, 360, 45)))[current_angle]
        mode_name = "FOOT ANCHOR" if mode == MODE_FOOT else "COLLISION BOX"
        status1 = (
            f"Mode: {mode_name} (TAB switch)  |  "
            f"Angle {angle_degs}\u00b0  Frame {current_frame}/{frames_per - 1}"
        )

        # Build collision box info string.
        if box_corner1 and box_corner2:
            bx1 = min(box_corner1[0], box_corner2[0])
            by1 = min(box_corner1[1], box_corner2[1])
            bx2 = max(box_corner1[0], box_corner2[0])
            by2 = max(box_corner1[1], box_corner2[1])
            box_str = f"Box: ({bx1},{by1})-({bx2},{by2})  {bx2-bx1}\u00d7{by2-by1}px"
        else:
            box_str = "Box: (not set)"

        status2 = (
            f"Foot: {foot_point if foot_point else '(not set)'}  |  "
            f"{box_str}  |  ENTER=save"
        )
        txt1 = font.render(status1, True, TEXT_COLOR)
        txt2 = font.render(status2, True, TEXT_COLOR)
        screen.blit(txt1, (8, win_h - 42))
        screen.blit(txt2, (8, win_h - 22))

        # Hover coords.
        mx, my = pygame.mouse.get_pos()
        fx = (mx - pad_x) / ZOOM
        fy = (my - pad_y) / ZOOM
        if 0 <= fx < frame_w and 0 <= fy < frame_h:
            coord_txt = font.render(f"({int(fx)}, {int(fy)})", True, TEXT_COLOR)
            screen.blit(coord_txt, (mx + 14, my - 6))

        pygame.display.flip()

    pygame.quit()


def save_json(
    foot_point: tuple[int, int] | None,
    box_corner1: tuple[int, int] | None,
    box_corner2: tuple[int, int] | None,
    frame_w: int,
    frame_h: int
) -> None:
    """Save the foot anchor and collision box to baker_bounds.json."""
    if foot_point is None:
        print("No foot anchor was set — nothing saved.")
        return

    data: dict = {
        "frameWidth": frame_w,
        "frameHeight": frame_h,
        "footAnchor": {
            "x": foot_point[0],
            "y": foot_point[1]
        },
    }

    # Collision box (optional but recommended).
    if box_corner1 is not None and box_corner2 is not None:
        bx1 = min(box_corner1[0], box_corner2[0])
        by1 = min(box_corner1[1], box_corner2[1])
        bx2 = max(box_corner1[0], box_corner2[0])
        by2 = max(box_corner1[1], box_corner2[1])
        data["collisionBox"] = {
            "x": bx1,
            "y": by1,
            "width": bx2 - bx1,
            "height": by2 - by1
        }

    os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)
    with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)

    print(f"\n✓ Saved to:\n  {OUTPUT_PATH}")
    print(f"  footAnchor = ({foot_point[0]}, {foot_point[1]})")
    if "collisionBox" in data:
        cb = data["collisionBox"]
        print(f"  collisionBox = ({cb['x']},{cb['y']}) {cb['width']}×{cb['height']}")
    else:
        print("  collisionBox = (not set — will use defaults)")
    print(f"  (frame size = {frame_w}×{frame_h})")


if __name__ == "__main__":
    main()
