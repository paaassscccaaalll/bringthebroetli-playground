#!/usr/bin/env python3
"""
Bring the Brötli – Voxel-Art Asset Generator
Generates all sprite PNGs for the game using Pillow.
Output directory: Content/Textures/
"""

import os
import sys

try:
    from PIL import Image, ImageDraw
except ImportError:
    print("Pillow not installed. Installing...")
    os.system(f"{sys.executable} -m pip install Pillow")
    from PIL import Image, ImageDraw

OUTPUT_DIR = os.path.join(os.path.dirname(__file__), "..", "Content", "Textures")
os.makedirs(OUTPUT_DIR, exist_ok=True)


# ─────────────────────── Color Palettes ───────────────────────

# Train
ENGINE_BODY = (80, 80, 100)
ENGINE_ROOF = (56, 56, 70)
ENGINE_HIGHLIGHT = (120, 120, 140)
ENGINE_DARK = (40, 40, 60)
CHIMNEY = (50, 50, 55)
CHIMNEY_CAP = (70, 70, 80)
CAB_COLOR = (60, 60, 80)
CAB_ROOF = (45, 45, 65)
WINDOW_GLASS = (150, 190, 220)
WINDOW_FRAME = (40, 40, 50)

TENDER_BODY = (70, 70, 85)
TENDER_ROOF = (50, 50, 60)
TENDER_COAL = (25, 25, 30)
TENDER_COAL_HIGHLIGHT = (45, 45, 50)

BOILER_BODY = (120, 90, 70)
BOILER_ROOF = (84, 63, 49)
BOILER_COPPER = (180, 120, 80)
BOILER_RIVET = (160, 100, 60)
BOILER_PIPE = (90, 90, 100)

PASSENGER_BODY = (130, 50, 50)
PASSENGER_ROOF = (91, 35, 35)
PASSENGER_TRIM = (170, 140, 60)
PASSENGER_WINDOW = (150, 190, 220, 200)

WHEEL_DARK = (30, 30, 30)
WHEEL_AXLE = (80, 80, 80)
COUPLING = (100, 100, 110)

# Player 1 (Blue)
P1_SKIN = (230, 195, 160)
P1_HAT = (50, 80, 180)
P1_TORSO = (60, 100, 200)
P1_LEGS = (40, 60, 140)
P1_BOOTS = (50, 40, 30)

# Player 2 (Red)
P2_SKIN = (210, 175, 140)
P2_HAT = (180, 50, 50)
P2_TORSO = (200, 60, 60)
P2_LEGS = (140, 40, 40)
P2_BOOTS = (50, 40, 30)

# Background
SKY_TOP = (135, 190, 235)
SKY_BOTTOM = (180, 215, 245)
HILL_FAR = (100, 130, 100)
HILL_NEAR = (80, 150, 60)
HILL_ACCENT = (70, 120, 55)
TERRAIN_BASE = (90, 160, 55)
TERRAIN_DARK = (70, 130, 45)

# Track
RAIL = (60, 60, 65)
TIE = (110, 80, 50)
GRAVEL = (110, 95, 75)
GROUND = (90, 70, 50)

# UI
GAUGE_FRAME = (50, 50, 60)
GAUGE_INNER = (30, 30, 40)
GAUGE_RIVET = (80, 80, 90)
HUD_BG = (15, 15, 25)
HUD_BORDER = (180, 150, 80)


# ─────────────────────── Helper Functions ───────────────────────

def draw_voxel_block(draw, x, y, w, h, top_color, front_color, depth=3):
    """Draw a 2.5D voxel block with a top face and front face."""
    # Front face
    draw.rectangle([x, y + depth, x + w - 1, y + h - 1], fill=front_color)
    # Top face (parallelogram approximated as offset rectangle)
    top_shade = tuple(min(c + 30, 255) for c in top_color)
    draw.rectangle([x, y, x + w - 1, y + depth - 1], fill=top_shade)
    # Left edge highlight
    for i in range(min(2, w)):
        draw.line([(x + i, y), (x + i, y + h - 1)],
                  fill=tuple(min(c + 15, 255) for c in front_color))
    # Right edge shadow
    for i in range(min(2, w)):
        draw.line([(x + w - 1 - i, y + depth), (x + w - 1 - i, y + h - 1)],
                  fill=tuple(max(c - 20, 0) for c in front_color))
    # Bottom edge
    draw.line([(x, y + h - 1), (x + w - 1, y + h - 1)],
              fill=tuple(max(c - 30, 0) for c in front_color))


def draw_wheel(draw, cx, cy, r=7):
    """Draw a small wheel."""
    draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=WHEEL_DARK)
    draw.ellipse([cx - r + 2, cy - r + 2, cx + r - 2, cy + r - 2], fill=WHEEL_AXLE)
    draw.ellipse([cx - 2, cy - 2, cx + 2, cy + 2], fill=WHEEL_DARK)


def darken(color, amount=30):
    return tuple(max(c - amount, 0) for c in color[:3])


def lighten(color, amount=30):
    return tuple(min(c + amount, 255) for c in color[:3])


# ─────────────────────── Asset Generators ───────────────────────

def generate_train_engine():
    """240x120: Side view of steam locomotive with chimney, boiler, cab."""
    w, h = 240, 120
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Main boiler body (cylindrical, use rounded rectangle)
    draw.rectangle([10, 40, 160, 85], fill=ENGINE_BODY)
    # Top highlight on boiler
    draw.rectangle([10, 38, 160, 42], fill=ENGINE_HIGHLIGHT)
    # Boiler bands
    for bx in [40, 80, 120]:
        draw.rectangle([bx, 40, bx + 4, 85], fill=ENGINE_DARK)

    # Smokebox front (left end)
    draw.rectangle([0, 35, 15, 90], fill=ENGINE_DARK)
    draw.rectangle([2, 38, 13, 42], fill=ENGINE_HIGHLIGHT)

    # Chimney
    draw.rectangle([15, 8, 35, 38], fill=CHIMNEY)
    draw.rectangle([12, 4, 38, 12], fill=CHIMNEY_CAP)
    # Chimney rim
    draw.rectangle([13, 10, 37, 12], fill=lighten(CHIMNEY_CAP, 20))

    # Steam dome
    draw.rectangle([70, 28, 90, 40], fill=lighten(ENGINE_BODY, 15))
    draw.rectangle([73, 24, 87, 30], fill=lighten(ENGINE_BODY, 25))

    # Safety valve
    draw.rectangle([100, 32, 108, 40], fill=BOILER_COPPER)

    # Cab (right side)
    draw.rectangle([160, 20, 230, 90], fill=CAB_COLOR)
    # Cab roof
    draw.rectangle([155, 16, 235, 24], fill=CAB_ROOF)
    draw.rectangle([155, 14, 235, 18], fill=lighten(CAB_ROOF, 20))
    # Cab window
    draw.rectangle([175, 32, 215, 58], fill=WINDOW_FRAME)
    draw.rectangle([178, 35, 212, 55], fill=WINDOW_GLASS)
    # Window cross
    draw.line([(195, 35), (195, 55)], fill=WINDOW_FRAME, width=2)
    draw.line([(178, 45), (212, 45)], fill=WINDOW_FRAME, width=2)
    # Cab door
    draw.rectangle([220, 30, 228, 85], fill=darken(CAB_COLOR, 15))

    # Running board / frame
    draw.rectangle([0, 86, 230, 92], fill=ENGINE_DARK)
    # Highlight strip
    draw.rectangle([0, 86, 230, 88], fill=ENGINE_HIGHLIGHT)

    # Cylinder (left, below boiler)
    draw.rectangle([5, 78, 35, 92], fill=darken(ENGINE_BODY, 10))

    # Cow catcher
    for i in range(4):
        draw.line([(0, 92 + i * 3), (8, 86)], fill=ENGINE_DARK, width=2)

    # Wheels
    draw_wheel(draw, 30, 102, 10)
    draw_wheel(draw, 65, 102, 10)
    draw_wheel(draw, 100, 102, 10)
    draw_wheel(draw, 135, 102, 10)
    draw_wheel(draw, 195, 102, 8)
    draw_wheel(draw, 215, 102, 8)

    # Connecting rod
    draw.line([(30, 102), (135, 102)], fill=COUPLING, width=2)

    # Firebox glow under boiler
    draw.rectangle([130, 86, 158, 92], fill=(180, 80, 40))

    return img


def generate_train_tender():
    """160x96: Coal tender car."""
    w, h = 160, 96
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Main body
    draw.rectangle([5, 20, 155, 68], fill=TENDER_BODY)
    # Side panel detail
    draw.rectangle([5, 20, 155, 24], fill=lighten(TENDER_BODY, 15))
    draw.rectangle([5, 64, 155, 68], fill=darken(TENDER_BODY, 15))
    # Vertical ribs
    for rx in range(15, 150, 20):
        draw.rectangle([rx, 24, rx + 2, 64], fill=darken(TENDER_BODY, 10))

    # Coal heap
    coal_points = [(10, 20), (30, 6), (60, 3), (100, 5), (130, 8), (150, 20)]
    # Fill area under coal
    for cx in range(10, 150):
        # Interpolate y from the polygon
        for ci in range(len(coal_points) - 1):
            x1, y1 = coal_points[ci]
            x2, y2 = coal_points[ci + 1]
            if x1 <= cx <= x2:
                t = (cx - x1) / max(x2 - x1, 1)
                cy = int(y1 + t * (y2 - y1))
                draw.line([(cx, cy), (cx, 20)], fill=TENDER_COAL)
                # Some coal highlights
                if cx % 7 == 0:
                    draw.point((cx, cy + 1), fill=TENDER_COAL_HIGHLIGHT)
                break

    # Brötli crate on top of the coal
    draw.rectangle([60, 0, 100, 14], fill=(200, 160, 80))
    draw.rectangle([62, 2, 98, 12], fill=(220, 180, 100))
    # Cross on crate
    draw.line([(70, 2), (70, 12)], fill=(180, 140, 60), width=1)
    draw.line([(90, 2), (90, 12)], fill=(180, 140, 60), width=1)
    # "B" label area
    draw.rectangle([75, 4, 85, 10], fill=(160, 50, 50))

    # Frame
    draw.rectangle([5, 68, 155, 74], fill=ENGINE_DARK)
    draw.rectangle([5, 68, 155, 70], fill=ENGINE_HIGHLIGHT)

    # Wheels
    draw_wheel(draw, 35, 82, 8)
    draw_wheel(draw, 65, 82, 8)
    draw_wheel(draw, 95, 82, 8)
    draw_wheel(draw, 125, 82, 8)

    # Coupling hooks
    draw.rectangle([0, 50, 6, 56], fill=COUPLING)
    draw.rectangle([154, 50, 160, 56], fill=COUPLING)

    return img


def generate_train_boiler_car():
    """200x96: The boiler/water car with pipes and gauges."""
    w, h = 200, 96
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Main body
    draw.rectangle([5, 18, 195, 68], fill=BOILER_BODY)
    # Top highlight
    draw.rectangle([5, 16, 195, 20], fill=lighten(BOILER_BODY, 20))
    # Bottom shadow
    draw.rectangle([5, 64, 195, 68], fill=darken(BOILER_BODY, 15))

    # Copper bands
    for bx in [30, 70, 110, 150]:
        draw.rectangle([bx, 18, bx + 5, 68], fill=BOILER_COPPER)
        # Rivets on bands
        for ry in [28, 45, 58]:
            draw.rectangle([bx + 1, ry, bx + 3, ry + 2], fill=BOILER_RIVET)

    # Pressure gauge (front)
    draw.ellipse([15, 28, 35, 48], fill=GAUGE_FRAME)
    draw.ellipse([18, 31, 32, 45], fill=(220, 220, 200))
    draw.line([(25, 38), (28, 32)], fill=(200, 50, 50), width=1)

    # Pipes along top
    draw.rectangle([40, 10, 160, 15], fill=BOILER_PIPE)
    draw.rectangle([40, 10, 160, 12], fill=lighten(BOILER_PIPE, 20))
    # Pipe connectors
    for px in [50, 90, 130]:
        draw.rectangle([px, 8, px + 8, 18], fill=darken(BOILER_PIPE, 10))

    # Water level indicator (glass tube)
    draw.rectangle([170, 24, 178, 60], fill=WINDOW_FRAME)
    draw.rectangle([172, 26, 176, 58], fill=(100, 180, 220, 180))
    # Water level line
    draw.rectangle([172, 40, 176, 58], fill=(60, 140, 200))

    # Steam valve on top
    draw.rectangle([95, 4, 105, 16], fill=BOILER_COPPER)
    draw.rectangle([92, 2, 108, 6], fill=lighten(BOILER_COPPER))

    # Frame
    draw.rectangle([5, 68, 195, 74], fill=ENGINE_DARK)
    draw.rectangle([5, 68, 195, 70], fill=ENGINE_HIGHLIGHT)

    # Wheels
    draw_wheel(draw, 35, 82, 8)
    draw_wheel(draw, 75, 82, 8)
    draw_wheel(draw, 125, 82, 8)
    draw_wheel(draw, 165, 82, 8)

    # Coupling hooks
    draw.rectangle([0, 50, 6, 56], fill=COUPLING)
    draw.rectangle([194, 50, 200, 56], fill=COUPLING)

    return img


def generate_train_passenger_car():
    """240x96: Passenger car with windows and trim."""
    w, h = 240, 96
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Main body
    draw.rectangle([5, 18, 235, 68], fill=PASSENGER_BODY)
    # Top moulding
    draw.rectangle([5, 16, 235, 20], fill=lighten(PASSENGER_BODY, 25))
    # Bottom moulding
    draw.rectangle([5, 64, 235, 68], fill=darken(PASSENGER_BODY, 15))
    # Gold trim strip
    draw.rectangle([5, 38, 235, 41], fill=PASSENGER_TRIM)

    # Windows
    win_y = 24
    win_h = 28
    win_w = 22
    for wx in range(18, 220, 32):
        # Frame
        draw.rectangle([wx - 1, win_y - 1, wx + win_w, win_y + win_h], fill=WINDOW_FRAME)
        # Glass
        draw.rectangle([wx, win_y, wx + win_w - 1, win_y + win_h - 1], fill=WINDOW_GLASS)
        # Cross divider
        cx = wx + win_w // 2
        cy = win_y + win_h // 2
        draw.line([(cx, win_y), (cx, win_y + win_h - 1)], fill=WINDOW_FRAME, width=1)
        # Curtain hint
        draw.rectangle([wx + 1, win_y + 1, wx + 4, win_y + win_h // 3], fill=(180, 150, 120, 100))

    # Roof line
    draw.rectangle([3, 14, 237, 17], fill=PASSENGER_ROOF)
    draw.rectangle([3, 12, 237, 15], fill=lighten(PASSENGER_ROOF, 15))

    # Door (middle)
    draw.rectangle([112, 22, 128, 66], fill=darken(PASSENGER_BODY, 20))
    draw.rectangle([114, 24, 126, 64], fill=darken(PASSENGER_BODY, 10))
    # Door handle
    draw.rectangle([123, 42, 125, 48], fill=PASSENGER_TRIM)

    # Frame
    draw.rectangle([5, 68, 235, 74], fill=ENGINE_DARK)
    draw.rectangle([5, 68, 235, 70], fill=ENGINE_HIGHLIGHT)

    # Wheels
    draw_wheel(draw, 35, 82, 8)
    draw_wheel(draw, 75, 82, 8)
    draw_wheel(draw, 165, 82, 8)
    draw_wheel(draw, 205, 82, 8)

    # Coupling hooks
    draw.rectangle([0, 50, 6, 56], fill=COUPLING)
    draw.rectangle([234, 50, 240, 56], fill=COUPLING)

    return img


def generate_player_spritesheet(skin, hat, torso, legs, boots, label_color):
    """
    128x48 spritesheet: 4 frames of 32x48 each.
    Frame 0: idle
    Frame 1: walk left foot
    Frame 2: idle (duplicate for animation)
    Frame 3: walk right foot
    """
    fw, fh = 32, 48
    img = Image.new("RGBA", (fw * 4, fh), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    def draw_character(ox, left_leg_dy=0, right_leg_dy=0, arm_angle=0):
        cx = ox + fw // 2  # center x
        # Boots
        draw.rectangle([cx - 6, 40 + left_leg_dy, cx - 1, 47], fill=boots)
        draw.rectangle([cx + 1, 40 + right_leg_dy, cx + 6, 47], fill=boots)
        # Legs
        draw.rectangle([cx - 5, 30 + left_leg_dy, cx - 1, 41 + left_leg_dy], fill=legs)
        draw.rectangle([cx + 1, 30 + right_leg_dy, cx + 6, 41 + right_leg_dy], fill=legs)
        # Torso
        draw.rectangle([cx - 7, 16, cx + 7, 31], fill=torso)
        # Belt
        draw.rectangle([cx - 7, 28, cx + 7, 30], fill=darken(torso, 20))
        # Torso highlight (lapels)
        draw.rectangle([cx - 2, 17, cx + 2, 26], fill=lighten(torso, 15))
        # Arms
        draw.rectangle([cx - 10, 17 - arm_angle, cx - 7, 30 + arm_angle], fill=torso)
        draw.rectangle([cx + 7, 17 + arm_angle, cx + 10, 30 - arm_angle], fill=torso)
        # Hands
        draw.rectangle([cx - 10, 29 + arm_angle, cx - 7, 32 + arm_angle], fill=skin)
        draw.rectangle([cx + 7, 29 - arm_angle, cx + 10, 32 - arm_angle], fill=skin)
        # Head (skin)
        draw.rectangle([cx - 6, 6, cx + 6, 17], fill=skin)
        # Eyes
        draw.rectangle([cx - 4, 10, cx - 2, 12], fill=(30, 30, 30))
        draw.rectangle([cx + 2, 10, cx + 4, 12], fill=(30, 30, 30))
        # Mouth
        draw.rectangle([cx - 2, 14, cx + 2, 15], fill=darken(skin, 40))
        # Hat
        draw.rectangle([cx - 7, 2, cx + 7, 7], fill=hat)
        draw.rectangle([cx - 9, 6, cx + 9, 8], fill=hat)  # brim
        # Hat band
        draw.rectangle([cx - 7, 5, cx + 7, 6], fill=darken(hat, 20))

    # Frame 0: Idle
    draw_character(0)
    # Frame 1: Walk, left forward
    draw_character(fw, left_leg_dy=-3, right_leg_dy=2, arm_angle=1)
    # Frame 2: Idle (repeat)
    draw_character(fw * 2)
    # Frame 3: Walk, right forward
    draw_character(fw * 3, left_leg_dy=2, right_leg_dy=-3, arm_angle=-1)

    return img


def generate_background_hills():
    """1600x200: Scrollable parallax hill silhouettes."""
    w, h = 1600, 200
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Sky gradient base (will blend with game sky)
    for y in range(h):
        t = y / h
        r = int(SKY_TOP[0] + t * (SKY_BOTTOM[0] - SKY_TOP[0]))
        g = int(SKY_TOP[1] + t * (SKY_BOTTOM[1] - SKY_TOP[1]))
        b = int(SKY_TOP[2] + t * (SKY_BOTTOM[2] - SKY_TOP[2]))
        draw.line([(0, y), (w - 1, y)], fill=(r, g, b))

    # Far mountain silhouettes
    import random
    random.seed(42)  # deterministic

    # Far mountains (dark, distant)
    far_points = [(0, 140)]
    x = 0
    while x < w:
        peak_h = random.randint(50, 120)
        peak_w = random.randint(80, 200)
        far_points.append((x + peak_w // 2, h - peak_h - 40))
        x += peak_w
        far_points.append((x, 140))
    far_points.append((w, 140))
    far_points.append((w, h))
    far_points.append((0, h))
    draw.polygon(far_points, fill=HILL_FAR)

    # Near hills (brighter green)
    near_points = [(0, 160)]
    x = 0
    while x < w:
        peak_h = random.randint(20, 60)
        peak_w = random.randint(100, 250)
        near_points.append((x + peak_w // 2, h - peak_h - 10))
        x += peak_w
        near_points.append((x, 160))
    near_points.append((w, 160))
    near_points.append((w, h))
    near_points.append((0, h))
    draw.polygon(near_points, fill=HILL_NEAR)

    # Some trees on near hills (simple pixel art trees)
    for tx in range(20, w, 60):
        ty = 155 + random.randint(-10, 10)
        # Trunk
        draw.rectangle([tx, ty, tx + 3, ty + 12], fill=(80, 60, 40))
        # Canopy (triangle-ish)
        for i in range(8):
            draw.rectangle([tx - 4 + i // 2, ty - i, tx + 7 - i // 2, ty - i + 1],
                           fill=(40 + random.randint(0, 20), 100 + random.randint(0, 30), 30))

    return img


def generate_background_midground():
    """1600x80: Closer terrain strip with detail."""
    w, h = 1600, 80
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Base terrain
    draw.rectangle([0, 0, w - 1, h - 1], fill=TERRAIN_BASE)

    import random
    random.seed(99)

    # Grass tufts
    for gx in range(0, w, 15):
        gy = random.randint(0, 10)
        draw.rectangle([gx, gy, gx + 8, gy + 5], fill=TERRAIN_DARK)

    # Fence posts
    for fx in range(0, w, 80):
        draw.rectangle([fx, 20, fx + 4, 60], fill=(110, 80, 50))
        draw.rectangle([fx - 1, 20, fx + 5, 24], fill=(120, 90, 55))

    # Fence wire
    draw.line([(0, 30), (w - 1, 30)], fill=(100, 90, 80), width=1)
    draw.line([(0, 45), (w - 1, 45)], fill=(100, 90, 80), width=1)

    # Occasional flowers
    for fx in range(10, w, 50):
        fy = 55 + random.randint(0, 15)
        colors = [(220, 60, 60), (60, 60, 220), (220, 200, 60), (220, 120, 200)]
        c = colors[random.randint(0, 3)]
        draw.rectangle([fx, fy, fx + 3, fy + 3], fill=c)
        draw.rectangle([fx + 1, fy + 3, fx + 2, fy + 7], fill=(50, 120, 40))

    return img


def generate_track_tile():
    """64x40: Repeating track segment with gravel, ties, rails."""
    w, h = 64, 40
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Gravel base
    draw.rectangle([0, 0, w - 1, h - 1], fill=GROUND)
    draw.rectangle([0, 0, w - 1, 14], fill=GRAVEL)

    import random
    random.seed(7)
    # Gravel speckles
    for _ in range(40):
        gx = random.randint(0, w - 2)
        gy = random.randint(0, 12)
        gc = random.randint(90, 120)
        draw.point((gx, gy), fill=(gc, gc - 15, gc - 25))

    # Ties
    for tx in [4, 24, 44]:
        draw.rectangle([tx, 4, tx + 14, 18], fill=TIE)
        draw.rectangle([tx, 4, tx + 14, 6], fill=lighten(TIE, 10))
        draw.rectangle([tx, 16, tx + 14, 18], fill=darken(TIE, 10))

    # Rails
    draw.rectangle([0, 7, w - 1, 10], fill=RAIL)
    draw.rectangle([0, 7, w - 1, 8], fill=lighten(RAIL, 15))
    draw.rectangle([0, 14, w - 1, 17], fill=RAIL)
    draw.rectangle([0, 14, w - 1, 15], fill=lighten(RAIL, 15))

    return img


def generate_task_station_spritesheet():
    """160x32: 4 task station icons, each 40x32.
    [0] Shovel, [1] Valve, [2] Gauge, [3] Brake
    """
    fw, fh = 40, 32
    img = Image.new("RGBA", (fw * 4, fh), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Frame 0: Shovel (coal)
    ox = 0
    # Handle
    draw.rectangle([ox + 18, 4, ox + 22, 26], fill=(140, 100, 60))
    # Blade
    draw.rectangle([ox + 12, 22, ox + 28, 30], fill=(120, 120, 130))
    draw.rectangle([ox + 14, 24, ox + 26, 28], fill=(100, 100, 110))
    # Coal bits
    draw.rectangle([ox + 15, 20, ox + 18, 23], fill=TENDER_COAL)
    draw.rectangle([ox + 22, 19, ox + 25, 22], fill=TENDER_COAL)

    # Frame 1: Valve/wheel
    ox = fw
    cx, cy = ox + 20, 16
    draw.ellipse([cx - 10, cy - 10, cx + 10, cy + 10], fill=(120, 120, 130))
    draw.ellipse([cx - 7, cy - 7, cx + 7, cy + 7], fill=(80, 80, 90))
    draw.ellipse([cx - 3, cy - 3, cx + 3, cy + 3], fill=(100, 100, 110))
    # Spokes
    for dx, dy in [(-8, 0), (8, 0), (0, -8), (0, 8)]:
        draw.line([(cx, cy), (cx + dx, cy + dy)], fill=(140, 140, 150), width=2)
    # Pipe below
    draw.rectangle([cx - 3, cy + 10, cx + 3, 30], fill=BOILER_PIPE)

    # Frame 2: Gauge
    ox = fw * 2
    cx, cy = ox + 20, 16
    # Gauge body
    draw.ellipse([cx - 12, cy - 12, cx + 12, cy + 12], fill=GAUGE_FRAME)
    draw.ellipse([cx - 9, cy - 9, cx + 9, cy + 9], fill=(220, 220, 200))
    # Needle
    draw.line([(cx, cy), (cx + 5, cy - 7)], fill=(200, 50, 50), width=2)
    # Tick marks
    for angle_x, angle_y in [(-7, -5), (0, -8), (7, -5)]:
        draw.rectangle([cx + angle_x - 1, cy + angle_y - 1, cx + angle_x + 1, cy + angle_y + 1],
                       fill=(40, 40, 40))

    # Frame 3: Brake lever
    ox = fw * 3
    # Base
    draw.rectangle([ox + 14, 24, ox + 26, 30], fill=(80, 80, 90))
    # Lever arm
    draw.rectangle([ox + 18, 4, ox + 22, 26], fill=(150, 50, 50))
    # Handle
    draw.ellipse([ox + 16, 2, ox + 24, 8], fill=(180, 60, 60))
    # Arrow indicator
    draw.polygon([(ox + 28, 14), (ox + 34, 18), (ox + 28, 22)], fill=(200, 200, 60))

    return img


def generate_particle_smoke():
    """96x16: 6 frames of 16x16 smoke puffs, varying opacity/size."""
    fw, fh = 16, 16
    frames = 6
    img = Image.new("RGBA", (fw * frames, fh), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    for i in range(frames):
        ox = i * fw
        cx, cy = ox + 8, 8
        # Size grows, opacity decreases
        r = 3 + i
        alpha = 200 - i * 30
        gray = 160 + i * 10
        color = (gray, gray, gray, max(alpha, 20))
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=color)
        # Inner lighter core
        ir = max(r - 2, 1)
        inner_a = max(alpha - 40, 10)
        draw.ellipse([cx - ir, cy - ir, cx + ir, cy + ir],
                     fill=(min(gray + 20, 255), min(gray + 20, 255), min(gray + 20, 255), inner_a))

    return img


def generate_particle_steam():
    """96x16: 6 frames of 16x16 steam wisps, white/blue tint."""
    fw, fh = 16, 16
    frames = 6
    img = Image.new("RGBA", (fw * frames, fh), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    for i in range(frames):
        ox = i * fw
        cx, cy = ox + 8, 8
        r = 2 + i
        alpha = 180 - i * 28
        color = (220, 230, 255, max(alpha, 15))
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=color)
        # Wispy extensions
        if i > 1:
            draw.ellipse([cx - r + 2, cy - r - 1, cx + r - 2, cy - r + 2],
                         fill=(230, 240, 255, max(alpha - 30, 10)))

    return img


def generate_ui_gauge_frame():
    """200x80: Styled gauge frame for HUD with riveted border."""
    w, h = 200, 80
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Outer frame
    draw.rectangle([0, 0, w - 1, h - 1], fill=GAUGE_FRAME)
    # Inner recessed area
    draw.rectangle([4, 4, w - 5, h - 5], fill=GAUGE_INNER)
    # Inner highlight (top-left)
    draw.line([(4, 4), (w - 5, 4)], fill=lighten(GAUGE_FRAME, 10))
    draw.line([(4, 4), (4, h - 5)], fill=lighten(GAUGE_FRAME, 10))
    # Inner shadow (bottom-right)
    draw.line([(4, h - 5), (w - 5, h - 5)], fill=darken(GAUGE_FRAME, 15))
    draw.line([(w - 5, 4), (w - 5, h - 5)], fill=darken(GAUGE_FRAME, 15))

    # Rivets along outer frame
    for rx in range(8, w - 4, 16):
        draw.ellipse([rx, 1, rx + 4, 5], fill=GAUGE_RIVET)
        draw.ellipse([rx, h - 6, rx + 4, h - 2], fill=GAUGE_RIVET)
    for ry in range(8, h - 4, 16):
        draw.ellipse([1, ry, 5, ry + 4], fill=GAUGE_RIVET)
        draw.ellipse([w - 6, ry, w - 2, ry + 4], fill=GAUGE_RIVET)

    # Gold accent strip (top)
    draw.rectangle([6, 6, w - 7, 9], fill=HUD_BORDER)

    return img


def generate_hud_panel():
    """1280x200: Full-width HUD background panel."""
    w, h = 1280, 200
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Main background
    draw.rectangle([0, 0, w - 1, h - 1], fill=HUD_BG)

    # Border (gold/brass like a train instrument panel)
    for t in range(3):
        shade = (HUD_BORDER[0] - t * 20, HUD_BORDER[1] - t * 20, HUD_BORDER[2] - t * 10)
        draw.rectangle([t, t, w - 1 - t, h - 1 - t], outline=shade)

    # Inner recessed panel
    draw.rectangle([6, 6, w - 7, h - 7], fill=(20, 20, 35))

    # Decorative rivets along the border
    for rx in range(12, w - 8, 24):
        draw.ellipse([rx, 1, rx + 5, 6], fill=GAUGE_RIVET)
        draw.ellipse([rx, h - 7, rx + 5, h - 2], fill=GAUGE_RIVET)

    # Vertical dividers for sections
    for dx in [400, 800]:
        draw.rectangle([dx, 8, dx + 2, h - 8], fill=(40, 40, 60))
        draw.rectangle([dx + 3, 8, dx + 4, h - 8], fill=lighten(HUD_BG, 10))

    # Section label areas (subtle rectangles)
    for sx, sw in [(10, 380), (406, 388), (808, 466)]:
        draw.rectangle([sx, 8, sx + sw, 24], fill=(25, 25, 40))
        draw.rectangle([sx, 8, sx + sw, 10], fill=HUD_BORDER)

    return img


# ─────────────────────── Main ───────────────────────

def save(img, name):
    path = os.path.join(OUTPUT_DIR, name)
    img.save(path, "PNG")
    print(f"  [OK] {name} ({img.width}x{img.height})")


def main():
    print("Generating voxel-art assets for Bring the Brotli...")
    print(f"Output: {os.path.abspath(OUTPUT_DIR)}\n")

    save(generate_train_engine(), "train_engine.png")
    save(generate_train_tender(), "train_tender.png")
    save(generate_train_boiler_car(), "train_boiler_car.png")
    save(generate_train_passenger_car(), "train_passenger_car.png")

    save(generate_player_spritesheet(P1_SKIN, P1_HAT, P1_TORSO, P1_LEGS, P1_BOOTS, (60, 100, 200)),
         "player1.png")
    save(generate_player_spritesheet(P2_SKIN, P2_HAT, P2_TORSO, P2_LEGS, P2_BOOTS, (200, 60, 60)),
         "player2.png")

    save(generate_background_hills(), "background_hills.png")
    save(generate_background_midground(), "background_midground.png")
    save(generate_track_tile(), "track_tile.png")
    save(generate_task_station_spritesheet(), "task_station_spritesheet.png")

    save(generate_particle_smoke(), "particle_smoke.png")
    save(generate_particle_steam(), "particle_steam.png")

    save(generate_ui_gauge_frame(), "ui_gauge_frame.png")
    save(generate_hud_panel(), "hud_panel.png")

    print(f"\nDone! {14} assets generated.")


if __name__ == "__main__":
    main()
