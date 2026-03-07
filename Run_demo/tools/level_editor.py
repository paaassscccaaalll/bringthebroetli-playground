#!/usr/bin/env python3
"""
level_editor.py — Interactive level editor for "Bring the Brötli"
=================================================================

Opens the locomotive sprite and provides a multi-mode editor for defining
collision boundaries, obstacles, action zones, and named anchor points.

Modes (press number keys to switch):
  [1] SURFACE BOUNDARY  — click to place polygon points for walkable area
  [2] OBSTACLE BOXES    — click+drag rectangles that block movement
  [3] ACTION ZONES      — click+drag rectangles for interactive areas
  [4] ANCHOR POINTS     — click to place named single-point markers

Controls:
  1/2/3/4       Switch mode
  LEFT CLICK    Place point (mode 1,4) or start drag (mode 2,3)
  RIGHT CLICK   Remove last point (mode 1) or delete hovered item (mode 2,3,4)
  CTRL+Z        Undo last action (any mode)
  CTRL+S        Save JSON
  S             Save JSON and exit
  ENTER         Save JSON and exit
  ESC           Quit without saving
  SCROLL WHEEL  Zoom in/out
  MIDDLE MOUSE  Pan the view (click and drag)

Requirements:
  pip install pygame

Usage:
  cd Run_demo/tools
  source .venv/bin/activate
  python level_editor.py

Output: ../Content/locomotive_bounds.json
"""

import json
import os
import sys
from typing import Optional

try:
    import pygame
    from pygame.locals import *
except ImportError:
    print("ERROR: Pygame is required.\n  pip install pygame")
    sys.exit(1)

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
SCRIPT_DIR  = os.path.dirname(os.path.abspath(__file__))
CONTENT_DIR = os.path.join(SCRIPT_DIR, "..", "Content")
IMAGE_PATH  = os.path.join(CONTENT_DIR, "locomotive.png")
OUTPUT_PATH = os.path.join(CONTENT_DIR, "locomotive_bounds.json")

# ---------------------------------------------------------------------------
# Colors
# ---------------------------------------------------------------------------
BG_COLOR         = (30, 30, 30)
SURFACE_COLOR    = (50, 255, 50)       # green — surface boundary
SURFACE_FILL     = (50, 255, 50, 30)
OBSTACLE_COLOR   = (255, 60, 60)       # red — obstacles
OBSTACLE_FILL    = (255, 60, 60, 40)
ZONE_COLOR       = (255, 220, 50)      # yellow — action zones
ZONE_FILL        = (255, 220, 50, 40)
ANCHOR_COLOR     = (80, 140, 255)      # blue — anchor points
BARRIER_COLOR    = (0, 220, 220)       # cyan — jump barriers
BARRIER_FILL     = (0, 220, 220, 40)
TEXT_COLOR        = (220, 220, 220)
LABEL_BG         = (0, 0, 0, 180)
MODE_COLORS      = {1: SURFACE_COLOR, 2: OBSTACLE_COLOR, 3: ZONE_COLOR, 4: ANCHOR_COLOR, 5: BARRIER_COLOR}
MODE_NAMES       = {1: "SURFACE BOUNDARY", 2: "OBSTACLE BOXES", 3: "ACTION ZONES", 4: "ANCHOR POINTS", 5: "JUMP BARRIERS"}

POINT_RADIUS     = 5
ANCHOR_RADIUS    = 7
MIN_RECT_SIZE    = 4   # minimum drag rectangle size to register

# Predefined action zone labels for quick selection.
ACTION_PRESETS   = ["load_coal", "load_water", "burn_coal", "pour_water"]

# ---------------------------------------------------------------------------
# Required items checklist  (shown at top-right of editor)
# Each entry: (type, label, description)
#   type = "surface", "anchor", "zone", "obstacle"
# ---------------------------------------------------------------------------
REQUIRED_ITEMS = [
    ("surface",  None,         "Surface boundary (≥3 pts)"),
    ("anchor",   "spawn",      "Anchor: spawn"),
    ("zone",     "load_coal",  "Zone: load_coal"),
    ("zone",     "load_water", "Zone: load_water"),
    ("zone",     "burn_coal",  "Zone: burn_coal"),
    ("zone",     "pour_water", "Zone: pour_water"),
]


# ===========================================================================
# Data model
# ===========================================================================
class EditorState:
    def __init__(self, image_w: int, image_h: int):
        self.image_w = image_w
        self.image_h = image_h
        self.surface_points: list[tuple[int, int]] = []
        self.obstacles: list[dict] = []       # {"label": str, "bounds": (x,y,w,h)}
        self.action_zones: list[dict] = []    # {"label": str, "bounds": (x,y,w,h)}
        self.anchor_points: list[dict] = []   # {"label": str, "pos": (x,y)}
        self.jump_barriers: list[tuple] = []  # (x, y, w, h)
        self.undo_stack: list[tuple] = []     # (action_type, data) for undo

    def push_undo(self, action_type: str, data):
        self.undo_stack.append((action_type, data))

    def undo(self) -> bool:
        if not self.undo_stack:
            return False
        action_type, data = self.undo_stack.pop()
        if action_type == "add_surface_point":
            if self.surface_points:
                self.surface_points.pop()
        elif action_type == "add_obstacle":
            if self.obstacles:
                self.obstacles.pop()
        elif action_type == "add_zone":
            if self.action_zones:
                self.action_zones.pop()
        elif action_type == "add_anchor":
            if self.anchor_points:
                self.anchor_points.pop()
        elif action_type == "add_barrier":
            if self.jump_barriers:
                self.jump_barriers.pop()
        elif action_type == "remove_surface_point":
            idx, pt = data
            self.surface_points.insert(idx, pt)
        elif action_type == "remove_obstacle":
            idx, obj = data
            self.obstacles.insert(idx, obj)
        elif action_type == "remove_zone":
            idx, obj = data
            self.action_zones.insert(idx, obj)
        elif action_type == "remove_anchor":
            idx, obj = data
            self.anchor_points.insert(idx, obj)
        elif action_type == "remove_barrier":
            idx, obj = data
            self.jump_barriers.insert(idx, obj)
        return True

    def to_json(self) -> dict:
        data: dict = {
            "imageSize": {"width": self.image_w, "height": self.image_h},
            "surfaceBoundary": [{"x": p[0], "y": p[1]} for p in self.surface_points],
            "obstacles": [
                {"label": o["label"], "bounds": {"x": o["bounds"][0], "y": o["bounds"][1],
                                                  "width": o["bounds"][2], "height": o["bounds"][3]}}
                for o in self.obstacles
            ],
            "actionZones": [
                {"label": z["label"], "bounds": {"x": z["bounds"][0], "y": z["bounds"][1],
                                                  "width": z["bounds"][2], "height": z["bounds"][3]}}
                for z in self.action_zones
            ],
            "anchorPoints": [
                {"label": a["label"], "position": {"x": a["pos"][0], "y": a["pos"][1]}}
                for a in self.anchor_points
            ],
            "jumpBarriers": [
                {"x": b[0], "y": b[1], "width": b[2], "height": b[3]}
                for b in self.jump_barriers
            ],
        }
        return data

    def load_existing(self, path: str):
        """Load previously saved JSON if it exists, so user can edit incrementally."""
        if not os.path.isfile(path):
            return
        try:
            with open(path, "r", encoding="utf-8") as f:
                data = json.load(f)
            for p in data.get("surfaceBoundary", []):
                self.surface_points.append((p["x"], p["y"]))
            for o in data.get("obstacles", []):
                b = o["bounds"]
                self.obstacles.append({"label": o["label"],
                                       "bounds": (b["x"], b["y"], b["width"], b["height"])})
            for z in data.get("actionZones", []):
                b = z["bounds"]
                self.action_zones.append({"label": z["label"],
                                          "bounds": (b["x"], b["y"], b["width"], b["height"])})
            for a in data.get("anchorPoints", []):
                p = a["position"]
                self.anchor_points.append({"label": a["label"], "pos": (p["x"], p["y"])})
            for b in data.get("jumpBarriers", []):
                self.jump_barriers.append((b["x"], b["y"], b["width"], b["height"]))
            print(f"Loaded existing data from {path}")
            print(f"  {len(self.surface_points)} surface points, "
                  f"{len(self.obstacles)} obstacles, "
                  f"{len(self.action_zones)} zones, "
                  f"{len(self.anchor_points)} anchors, "
                  f"{len(self.jump_barriers)} barriers")
        except Exception as e:
            print(f"Warning: could not load existing JSON: {e}")


# ===========================================================================
# Text input helper (blocking, Pygame-based)
# ===========================================================================
def text_input_dialog(screen: pygame.Surface, font: pygame.font.Font,
                      prompt: str, presets: list[str] | None = None) -> str | None:
    """Show a small text input box at the bottom of the screen.
    Returns the entered string or None if cancelled."""
    sw, sh = screen.get_size()
    box_h  = 80 if not presets else 100
    input_text = ""
    clock = pygame.time.Clock()

    while True:
        for event in pygame.event.get():
            if event.type == QUIT:
                return None
            if event.type == KEYDOWN:
                if event.key == K_ESCAPE:
                    return None
                if event.key == K_RETURN:
                    return input_text.strip() if input_text.strip() else None
                if event.key == K_BACKSPACE:
                    input_text = input_text[:-1]
                elif event.unicode and event.unicode.isprintable():
                    input_text += event.unicode

                # Number keys for presets.
                if presets:
                    idx = event.key - K_1  # K_1 = 49
                    if 0 <= idx < len(presets):
                        return presets[idx]

        # Draw dialog overlay.
        overlay = pygame.Surface((sw, box_h), pygame.SRCALPHA)
        overlay.fill((0, 0, 0, 200))
        screen.blit(overlay, (0, sh - box_h))

        prompt_surf = font.render(prompt, True, TEXT_COLOR)
        screen.blit(prompt_surf, (10, sh - box_h + 8))

        if presets:
            preset_str = "  ".join(f"[{i+1}] {p}" for i, p in enumerate(presets))
            ps = font.render(preset_str, True, (180, 180, 100))
            screen.blit(ps, (10, sh - box_h + 30))

        input_surf = font.render(f"> {input_text}_", True, (255, 255, 255))
        screen.blit(input_surf, (10, sh - box_h + (55 if presets else 35)))

        pygame.display.flip()
        clock.tick(30)


# ===========================================================================
# Main editor
# ===========================================================================
def main() -> None:
    if not os.path.isfile(IMAGE_PATH):
        print(f"ERROR: locomotive.png not found at:\n  {IMAGE_PATH}")
        sys.exit(1)

    pygame.init()

    # We need a display surface before convert_alpha(), so load the
    # image in two steps: first get its size, create the window, then convert.
    raw_image_raw = pygame.image.load(IMAGE_PATH)
    img_w, img_h = raw_image_raw.get_size()

    # Window setup — fit the image with some padding.
    info = pygame.display.Info()
    max_w = min(info.current_w - 100, 1800)
    max_h = min(info.current_h - 150, 900)

    # Compute initial zoom to fit image in window.
    zoom = min(max_w / img_w, max_h / img_h, 1.0)
    min_zoom = zoom * 0.3
    max_zoom = 3.0

    win_w = max_w
    win_h = max_h + 50   # status bar

    screen = pygame.display.set_mode((win_w, win_h), RESIZABLE)
    pygame.display.set_caption("Brötli Level Editor — 1:Surface  2:Obstacles  3:Zones  4:Anchors")

    # Now that a display exists, convert for fast alpha blitting.
    raw_image = raw_image_raw.convert_alpha()
    font = pygame.font.SysFont("monospace", 14)
    small_font = pygame.font.SysFont("monospace", 12)
    clock = pygame.time.Clock()

    # Pan offset (in screen pixels). Image is drawn at pan_offset * zoom.
    pan_x = (win_w - img_w * zoom) / 2
    pan_y = (win_h - 50 - img_h * zoom) / 2

    # Editor state.
    state = EditorState(img_w, img_h)
    state.load_existing(OUTPUT_PATH)

    mode = 1   # current editing mode

    # Drag state for rectangle modes (2, 3).
    dragging = False
    drag_start: tuple[int, int] | None = None  # image coords
    drag_end: tuple[int, int] | None = None

    # Middle-mouse pan state.
    panning = False
    pan_start_mouse = (0, 0)
    pan_start_offset = (0.0, 0.0)

    running = True
    saved = False

    def screen_to_image(sx: int, sy: int) -> tuple[int, int]:
        """Convert screen pixel to image pixel coords."""
        ix = int((sx - pan_x) / zoom)
        iy = int((sy - pan_y) / zoom)
        return ix, iy

    def image_to_screen(ix: float, iy: float) -> tuple[float, float]:
        """Convert image pixel to screen pixel coords."""
        return ix * zoom + pan_x, iy * zoom + pan_y

    def is_in_image(ix: int, iy: int) -> bool:
        return 0 <= ix < img_w and 0 <= iy < img_h

    def rect_from_bounds(b: tuple[int,int,int,int]) -> pygame.Rect:
        """Convert (x,y,w,h) image-space rect to screen-space pygame Rect."""
        sx, sy = image_to_screen(b[0], b[1])
        return pygame.Rect(int(sx), int(sy), int(b[2] * zoom), int(b[3] * zoom))

    def point_in_rect(px: int, py: int, b: tuple[int,int,int,int]) -> bool:
        return b[0] <= px <= b[0]+b[2] and b[1] <= py <= b[1]+b[3]

    def point_near(px: int, py: int, ax: int, ay: int, radius: int = 10) -> bool:
        return abs(px - ax) <= radius and abs(py - ay) <= radius

    while running:
        for event in pygame.event.get():
            if event.type == QUIT:
                running = False

            elif event.type == KEYDOWN:
                mods = pygame.key.get_mods()

                if event.key == K_ESCAPE:
                    running = False

                # Ctrl+Z — undo
                elif event.key == K_z and (mods & KMOD_CTRL):
                    state.undo()

                # Ctrl+S — save
                elif event.key == K_s and (mods & KMOD_CTRL):
                    save_json(state, OUTPUT_PATH)
                    saved = True

                # S (no ctrl) — save and exit
                elif event.key == K_s and not (mods & KMOD_CTRL):
                    save_json(state, OUTPUT_PATH)
                    saved = True
                    running = False

                # ENTER — save and exit
                elif event.key in (K_RETURN, K_KP_ENTER):
                    save_json(state, OUTPUT_PATH)
                    saved = True
                    running = False

                # Mode switching
                elif event.key == K_1:
                    mode = 1; dragging = False
                elif event.key == K_2:
                    mode = 2; dragging = False
                elif event.key == K_3:
                    mode = 3; dragging = False
                elif event.key == K_4:
                    mode = 4; dragging = False
                elif event.key == K_5:
                    mode = 5; dragging = False

            elif event.type == MOUSEBUTTONDOWN:
                mx, my = event.pos
                ix, iy = screen_to_image(mx, my)

                # Middle mouse — start panning
                if event.button == 2:
                    panning = True
                    pan_start_mouse = (mx, my)
                    pan_start_offset = (pan_x, pan_y)

                elif event.button == 1:  # LEFT CLICK
                    if not is_in_image(ix, iy):
                        continue

                    if mode == 1:  # Surface boundary — add point
                        state.surface_points.append((ix, iy))
                        state.push_undo("add_surface_point", (ix, iy))

                    elif mode in (2, 3, 5):  # Obstacle / zone / barrier — start drag
                        dragging = True
                        drag_start = (ix, iy)
                        drag_end = (ix, iy)

                    elif mode == 4:  # Anchor point
                        label = text_input_dialog(screen, font,
                                                  "Enter anchor label (ESC=cancel):")
                        if label:
                            state.anchor_points.append({"label": label, "pos": (ix, iy)})
                            state.push_undo("add_anchor", None)

                elif event.button == 3:  # RIGHT CLICK — remove
                    if mode == 1:
                        if state.surface_points:
                            removed = state.surface_points.pop()
                            state.push_undo("remove_surface_point",
                                            (len(state.surface_points), removed))
                    elif mode == 2:
                        # Remove obstacle under cursor.
                        for i in range(len(state.obstacles) - 1, -1, -1):
                            if point_in_rect(ix, iy, state.obstacles[i]["bounds"]):
                                removed = state.obstacles.pop(i)
                                state.push_undo("remove_obstacle", (i, removed))
                                break
                    elif mode == 3:
                        for i in range(len(state.action_zones) - 1, -1, -1):
                            if point_in_rect(ix, iy, state.action_zones[i]["bounds"]):
                                removed = state.action_zones.pop(i)
                                state.push_undo("remove_zone", (i, removed))
                                break
                    elif mode == 4:
                        for i in range(len(state.anchor_points) - 1, -1, -1):
                            ap = state.anchor_points[i]["pos"]
                            if point_near(ix, iy, ap[0], ap[1], int(12 / zoom)):
                                removed = state.anchor_points.pop(i)
                                state.push_undo("remove_anchor", (i, removed))
                                break
                    elif mode == 5:
                        for i in range(len(state.jump_barriers) - 1, -1, -1):
                            if point_in_rect(ix, iy, state.jump_barriers[i]):
                                removed = state.jump_barriers.pop(i)
                                state.push_undo("remove_barrier", (i, removed))
                                break

                # Scroll wheel — zoom
                elif event.button == 4:  # scroll up
                    old_zoom = zoom
                    zoom = min(zoom * 1.15, max_zoom)
                    # Zoom toward cursor.
                    pan_x = mx - (mx - pan_x) * (zoom / old_zoom)
                    pan_y = my - (my - pan_y) * (zoom / old_zoom)
                elif event.button == 5:  # scroll down
                    old_zoom = zoom
                    zoom = max(zoom / 1.15, min_zoom)
                    pan_x = mx - (mx - pan_x) * (zoom / old_zoom)
                    pan_y = my - (my - pan_y) * (zoom / old_zoom)

            elif event.type == MOUSEBUTTONUP:
                if event.button == 2:
                    panning = False

                if event.button == 1 and dragging:
                    dragging = False
                    if drag_start and drag_end:
                        x1 = min(drag_start[0], drag_end[0])
                        y1 = min(drag_start[1], drag_end[1])
                        x2 = max(drag_start[0], drag_end[0])
                        y2 = max(drag_start[1], drag_end[1])
                        w = x2 - x1
                        h = y2 - y1
                        if w >= MIN_RECT_SIZE and h >= MIN_RECT_SIZE:
                            if mode == 2:
                                label = text_input_dialog(screen, font,
                                                          "Enter obstacle label (ESC=cancel):")
                                if label:
                                    state.obstacles.append({"label": label, "bounds": (x1,y1,w,h)})
                                    state.push_undo("add_obstacle", None)
                            elif mode == 3:
                                label = text_input_dialog(screen, font,
                                    "Enter action zone label (number for preset, or type custom):",
                                    presets=ACTION_PRESETS)
                                if label:
                                    state.action_zones.append({"label": label, "bounds": (x1,y1,w,h)})
                                    state.push_undo("add_zone", None)
                            elif mode == 5:
                                state.jump_barriers.append((x1, y1, w, h))
                                state.push_undo("add_barrier", None)
                    drag_start = None
                    drag_end = None

            elif event.type == MOUSEMOTION:
                mx, my = event.pos
                if panning:
                    pan_x = pan_start_offset[0] + (mx - pan_start_mouse[0])
                    pan_y = pan_start_offset[1] + (my - pan_start_mouse[1])
                if dragging:
                    drag_end = screen_to_image(mx, my)

        # ==================================================================
        # DRAW
        # ==================================================================
        screen.fill(BG_COLOR)

        # ---- Checkerboard behind image to show transparency ----
        checker = 16
        disp_w = int(img_w * zoom)
        disp_h = int(img_h * zoom)
        ix0 = max(0, int(-pan_x / zoom))
        iy0 = max(0, int(-pan_y / zoom))
        for cy in range(iy0, img_h, checker):
            for cx in range(ix0, img_w, checker):
                sx, sy = image_to_screen(cx, cy)
                if sx > win_w or sy > win_h - 50:
                    break
                shade = 45 if ((cx // checker) + (cy // checker)) % 2 == 0 else 55
                cw = min(checker, img_w - cx)
                ch = min(checker, img_h - cy)
                pygame.draw.rect(screen, (shade, shade, shade),
                                 (int(sx), int(sy), int(cw * zoom) + 1, int(ch * zoom) + 1))

        # ---- Draw scaled image ----
        scaled_img = pygame.transform.scale(raw_image, (disp_w, disp_h))
        screen.blit(scaled_img, (int(pan_x), int(pan_y)))

        # ---- Draw surface boundary polygon ----
        if len(state.surface_points) >= 3:
            screen_pts = [image_to_screen(p[0], p[1]) for p in state.surface_points]
            # Fill.
            fill_surf = pygame.Surface((win_w, win_h), pygame.SRCALPHA)
            pygame.draw.polygon(fill_surf, SURFACE_FILL, [(int(x), int(y)) for x, y in screen_pts])
            screen.blit(fill_surf, (0, 0))
            # Outline.
            pygame.draw.polygon(screen, SURFACE_COLOR,
                                [(int(x), int(y)) for x, y in screen_pts], 2)
        elif len(state.surface_points) >= 2:
            screen_pts = [image_to_screen(p[0], p[1]) for p in state.surface_points]
            pygame.draw.lines(screen, SURFACE_COLOR, False,
                              [(int(x), int(y)) for x, y in screen_pts], 2)

        for p in state.surface_points:
            sx, sy = image_to_screen(p[0], p[1])
            pygame.draw.circle(screen, SURFACE_COLOR, (int(sx), int(sy)), POINT_RADIUS)

        # ---- Draw obstacles ----
        for o in state.obstacles:
            r = rect_from_bounds(o["bounds"])
            fill_s = pygame.Surface((r.w, r.h), pygame.SRCALPHA)
            fill_s.fill(OBSTACLE_FILL)
            screen.blit(fill_s, r.topleft)
            pygame.draw.rect(screen, OBSTACLE_COLOR, r, 2)
            lbl = small_font.render(o["label"], True, OBSTACLE_COLOR)
            screen.blit(lbl, (r.x + 3, r.y + 2))

        # ---- Draw action zones ----
        for z in state.action_zones:
            r = rect_from_bounds(z["bounds"])
            fill_s = pygame.Surface((r.w, r.h), pygame.SRCALPHA)
            fill_s.fill(ZONE_FILL)
            screen.blit(fill_s, r.topleft)
            pygame.draw.rect(screen, ZONE_COLOR, r, 2)
            lbl = small_font.render(z["label"], True, ZONE_COLOR)
            screen.blit(lbl, (r.x + 3, r.y + 2))

        # ---- Draw jump barriers ----
        for b in state.jump_barriers:
            r = rect_from_bounds(b)
            fill_s = pygame.Surface((max(r.w, 1), max(r.h, 1)), pygame.SRCALPHA)
            fill_s.fill(BARRIER_FILL)
            screen.blit(fill_s, r.topleft)
            pygame.draw.rect(screen, BARRIER_COLOR, r, 2)

        # ---- Draw anchor points ----
        for a in state.anchor_points:
            sx, sy = image_to_screen(a["pos"][0], a["pos"][1])
            pygame.draw.circle(screen, ANCHOR_COLOR, (int(sx), int(sy)), ANCHOR_RADIUS)
            pygame.draw.circle(screen, (255, 255, 255), (int(sx), int(sy)), ANCHOR_RADIUS, 1)
            lbl = small_font.render(a["label"], True, ANCHOR_COLOR)
            screen.blit(lbl, (int(sx) + ANCHOR_RADIUS + 4, int(sy) - 6))

        # ---- Draw drag preview ----
        if dragging and drag_start and drag_end:
            x1 = min(drag_start[0], drag_end[0])
            y1 = min(drag_start[1], drag_end[1])
            x2 = max(drag_start[0], drag_end[0])
            y2 = max(drag_start[1], drag_end[1])
            sx1, sy1 = image_to_screen(x1, y1)
            sx2, sy2 = image_to_screen(x2, y2)
            color = MODE_COLORS.get(mode, (200, 200, 200))
            pygame.draw.rect(screen, color,
                             (int(sx1), int(sy1), int(sx2 - sx1), int(sy2 - sy1)), 2)

        # ---- Checklist panel (top-right) ----
        panel_w = 220
        panel_x = win_w - panel_w - 10
        panel_y = 10
        line_h = 18
        panel_h = 8 + line_h * (len(REQUIRED_ITEMS) + 1)  # +1 for header

        panel_bg = pygame.Surface((panel_w, panel_h), pygame.SRCALPHA)
        panel_bg.fill((0, 0, 0, 180))
        screen.blit(panel_bg, (panel_x, panel_y))

        hdr = small_font.render("Required items", True, (180, 180, 180))
        screen.blit(hdr, (panel_x + 6, panel_y + 4))

        for ri, (rtype, rlabel, rdesc) in enumerate(REQUIRED_ITEMS):
            done = False
            if rtype == "surface":
                done = len(state.surface_points) >= 3
            elif rtype == "anchor":
                done = any(a["label"] == rlabel for a in state.anchor_points)
            elif rtype == "zone":
                done = any(z["label"] == rlabel for z in state.action_zones)
            elif rtype == "obstacle":
                done = any(o["label"] == rlabel for o in state.obstacles)

            icon = "\u2713" if done else "\u2022"  # checkmark or bullet
            color = (80, 220, 80) if done else (180, 100, 100)
            txt = small_font.render(f" {icon} {rdesc}", True, color)
            screen.blit(txt, (panel_x + 4, panel_y + 4 + line_h * (ri + 1)))

        # ---- Status bar ----
        bar_y = win_h - 46
        overlay = pygame.Surface((win_w, 46), pygame.SRCALPHA)
        overlay.fill((0, 0, 0, 200))
        screen.blit(overlay, (0, bar_y))

        mode_color = MODE_COLORS.get(mode, TEXT_COLOR)
        mode_str = f"Mode [{mode}]: {MODE_NAMES[mode]}"
        mode_surf = font.render(mode_str, True, mode_color)
        screen.blit(mode_surf, (10, bar_y + 4))

        counts = (f"Surface:{len(state.surface_points)}  "
                  f"Obstacles:{len(state.obstacles)}  "
                  f"Zones:{len(state.action_zones)}  "
                  f"Anchors:{len(state.anchor_points)}  "
                  f"Barriers:{len(state.jump_barriers)}")
        cnt_surf = font.render(counts, True, TEXT_COLOR)
        screen.blit(cnt_surf, (10, bar_y + 24))

        # Hover coords.
        mx, my = pygame.mouse.get_pos()
        ix, iy = screen_to_image(mx, my)
        if is_in_image(ix, iy):
            coord_surf = font.render(f"({ix}, {iy})", True, TEXT_COLOR)
            screen.blit(coord_surf, (win_w - 140, bar_y + 4))

        # Zoom level.
        zoom_surf = font.render(f"Zoom: {zoom:.2f}x", True, TEXT_COLOR)
        screen.blit(zoom_surf, (win_w - 140, bar_y + 24))

        # Help hint.
        help_str = "CTRL+Z=undo  CTRL+S=save  S=save+exit  ESC=quit"
        help_surf = small_font.render(help_str, True, (120, 120, 120))
        screen.blit(help_surf, (win_w // 2 - help_surf.get_width() // 2, bar_y + 28))

        pygame.display.flip()
        clock.tick(60)

    pygame.quit()
    if not saved:
        print("Exited without saving.")


def save_json(state: EditorState, path: str) -> None:
    data = state.to_json()
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
    print(f"\n✓ Saved to: {path}")
    print(f"  Surface boundary: {len(state.surface_points)} points")
    print(f"  Obstacles:        {len(state.obstacles)}")
    print(f"  Action zones:     {len(state.action_zones)}")
    print(f"  Anchor points:    {len(state.anchor_points)}")
    print(f"  Jump barriers:    {len(state.jump_barriers)}")


if __name__ == "__main__":
    main()
