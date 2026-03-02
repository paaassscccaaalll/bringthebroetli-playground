using System;
using System.Collections.Generic;
using BringTheBrotli.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotli.World
{
    /// <summary>
    /// Draws train cars as Crossy-Road-style voxel blocks.
    /// Each car has: top face (lighter), front/south face (medium, uses sprite),
    /// and right side/east face (darkest). Bold dark outlines on every edge.
    /// </summary>
    public class TrainRenderer
    {
        private readonly TextRenderer _text;

        private static readonly Color OL = new(25, 25, 30);   // outline color
        private const int O = 3;                                // outline width

        public TrainRenderer(TextRenderer text) { _text = text; }

        // ═══════════════════════════════════════════════════════════════
        //  TOP FACE  — visible from above, lighter flat color + bold outline
        // ═══════════════════════════════════════════════════════════════
        public void DrawRoofLayer(SpriteBatch sb, Camera camera, List<TrainCar> cars)
        {
            foreach (var car in cars)
            {
                float screenX = camera.WorldToScreenX(car.WorldX);
                int sx = (int)screenX;
                int sw = (int)car.Width;
                int y = (int)Constants.VoxelTopY;
                int h = (int)Constants.VoxelTopH;

                Color top = car.RoofColor;

                // ── Flat fill (voxel = minimal gradient) ──
                _text.DrawRect(sb, new Rectangle(sx, y, sw, h), top);

                // Slight lighter band at back (far) edge for depth hint
                _text.DrawRect(sb, new Rectangle(sx + O, y + O, sw - O * 2, 5), Lighten(top, 20));

                // Slight darker band at front (near) edge
                _text.DrawRect(sb, new Rectangle(sx + O, y + h - O - 5, sw - O * 2, 5), Darken(top, 12));

                // ── Voxel panel lines ──
                for (int ly = y + 14; ly < y + h - 8; ly += 18)
                    _text.DrawRect(sb, new Rectangle(sx + O + 3, ly, sw - O * 2 - 6, 1),
                        Darken(top, 8));

                // ── Rivet dots ──
                for (int ry = y + 10; ry < y + h - 6; ry += 14)
                {
                    _text.DrawRect(sb, new Rectangle(sx + O + 4, ry, 2, 2), Lighten(top, 30));
                    _text.DrawRect(sb, new Rectangle(sx + sw - O - 6, ry, 2, 2), Lighten(top, 30));
                }

                // ── Roof vent (non-engine cars) ──
                if (car.Name != "Engine")
                {
                    int vx = sx + sw / 2 - 8;
                    int vy = y + h / 2 - 4;
                    _text.DrawRect(sb, new Rectangle(vx, vy, 16, 8), Darken(top, 25));
                    VoxelOutline(sb, vx, vy, 16, 8);
                }

                // ── Bold outline ──
                VoxelOutline(sb, sx, y, sw, h);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  FRONT FACE  — south-facing, camera sees this directly
        //  Also draws: side faces, wheels, couplings, shadows
        // ═══════════════════════════════════════════════════════════════
        public void DrawWallLayer(SpriteBatch sb, Camera camera, List<TrainCar> cars)
        {
            int sideW = (int)Constants.VoxelSideW;
            int totalH = (int)(Constants.VoxelTopH + Constants.VoxelFrontH);

            for (int ci = 0; ci < cars.Count; ci++)
            {
                var car = cars[ci];
                float screenX = camera.WorldToScreenX(car.WorldX);
                int sx = (int)screenX;
                int sw = (int)car.Width;
                int fy = (int)Constants.VoxelFrontY;
                int fh = (int)Constants.VoxelFrontH;

                // ── Front face sprite ──
                if (car.Texture != null)
                {
                    sb.Draw(car.Texture, new Rectangle(sx, fy, sw, fh), null, Color.White,
                            0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
                }
                else
                {
                    _text.DrawRect(sb, new Rectangle(sx, fy, sw, fh), car.WallColor);
                }

                // ── Car-specific front details ──
                DrawFrontDetails(sb, sx, sw, fy, fh, car);

                // ── Bold outline (front face) ──
                VoxelOutline(sb, sx, fy, sw, fh);

                // ── Right side face (east face = darkest) ──
                // Draw for the last car (rightmost on screen) at full width;
                // for other cars, fit into the 10px gap
                bool isLast = ci == cars.Count - 1;
                int thisSideW = isLast ? sideW : Math.Min(sideW, 8);
                int sideX = sx + sw;
                int sideY = (int)Constants.VoxelTopY;

                Color sideColor = Darken(car.WallColor, 45);
                _text.DrawRect(sb, new Rectangle(sideX, sideY, thisSideW, totalH), sideColor);
                // Side outline
                _text.DrawRect(sb, new Rectangle(sideX, sideY, thisSideW, O), OL);                // top
                _text.DrawRect(sb, new Rectangle(sideX, sideY + totalH - O, thisSideW, O), OL);   // bottom
                _text.DrawRect(sb, new Rectangle(sideX + thisSideW - 2, sideY, 2, totalH), OL);   // right edge
                // Midline where top meets front on the side
                _text.DrawRect(sb, new Rectangle(sideX, fy, thisSideW, 1), new Color(40, 40, 48));

                // ── Wheels / bogies ──
                DrawWheels(sb, sx, sw);

                // ── Cast shadow on the ground below ──
                int shadY = (int)Constants.VoxelBottomY + 12;
                for (int si = 0; si < 7; si++)
                {
                    int alpha = 35 - si * 4;
                    _text.DrawRect(sb, new Rectangle(sx + 3 + si, shadY + si,
                        sw + thisSideW - si, 2), new Color(0, 0, 0, Math.Max(alpha, 0)));
                }
            }

            // ── Couplings between cars ──
            for (int i = 0; i < cars.Count - 1; i++)
            {
                float gapX = camera.WorldToScreenX(cars[i].RightEdge);
                float nextX = camera.WorldToScreenX(cars[i + 1].WorldX);
                int gw = (int)(nextX - gapX);
                if (gw > 0)
                {
                    int cy = (int)Constants.VoxelFrontY + (int)Constants.VoxelFrontH / 2 - 5;
                    // Coupling bar
                    _text.DrawRect(sb, new Rectangle((int)gapX, cy, gw, 10), new Color(70, 70, 80));
                    VoxelOutline(sb, (int)gapX, cy, gw, 10);
                    // Pin bolts at each end
                    _text.DrawRect(sb, new Rectangle((int)gapX - 2, cy - 1, 5, 12), OL);
                    _text.DrawRect(sb, new Rectangle((int)nextX - 3, cy - 1, 5, 12), OL);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  CAR-SPECIFIC FRONT FACE DETAILS
        // ═══════════════════════════════════════════════════════════════
        private void DrawFrontDetails(SpriteBatch sb, int sx, int sw, int fy, int fh, TrainCar car)
        {
            if (car.Name == "PassengerCar")
            {
                // Windows with bold outlines
                for (int wx = sx + 24; wx < sx + sw - 30; wx += 44)
                {
                    int wy = fy + 22;
                    _text.DrawRect(sb, new Rectangle(wx, wy, 24, 32), new Color(75, 108, 148));
                    // Reflection
                    _text.DrawRect(sb, new Rectangle(wx + 3, wy + 3, 8, 14),
                        new Color(125, 165, 205, 80));
                    VoxelOutline(sb, wx, wy, 24, 32);
                }
            }
            else if (car.Name == "BoilerCar")
            {
                // Gauge
                int gx = sx + sw / 2 - 10;
                int gy = fy + 28;
                _text.DrawRect(sb, new Rectangle(gx, gy, 20, 20), new Color(180, 180, 170));
                _text.DrawRect(sb, new Rectangle(gx + 9, gy + 3, 2, 10), new Color(200, 50, 40));
                VoxelOutline(sb, gx, gy, 20, 20);
                // Pipes
                _text.DrawRect(sb, new Rectangle(sx + 14, fy + 60, sw - 28, 4), new Color(80, 80, 88));
                VoxelOutline(sb, sx + 14, fy + 60, sw - 28, 4);
                _text.DrawRect(sb, new Rectangle(sx + 14, fy + 78, sw - 28, 4), new Color(80, 80, 88));
                VoxelOutline(sb, sx + 14, fy + 78, sw - 28, 4);
            }
            else if (car.Name == "Engine")
            {
                // Headlight
                int hlX = sx + sw - 14;
                int hlY = fy + 12;
                _text.DrawRect(sb, new Rectangle(hlX, hlY, 10, 10), new Color(255, 240, 180));
                _text.DrawRect(sb, new Rectangle(hlX + 2, hlY + 2, 5, 5), new Color(255, 255, 230));
                VoxelOutline(sb, hlX, hlY, 10, 10);
                // Light glow
                _text.DrawRect(sb, new Rectangle(hlX - 4, hlY - 2, 18, 14),
                    new Color(255, 240, 180, 20));

                // Front buffer/bumper
                int bufY = fy + fh - 22;
                _text.DrawRect(sb, new Rectangle(sx + sw - 8, bufY, 8, 20), new Color(65, 65, 72));
                VoxelOutline(sb, sx + sw - 8, bufY, 8, 20);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  WHEEL BOGIES  — voxel-outlined wheels below the front face
        // ═══════════════════════════════════════════════════════════════
        private void DrawWheels(SpriteBatch sb, int sx, int sw)
        {
            int wheelY = (int)Constants.VoxelBottomY - 1;
            int wd = 14; // wheel diameter

            int[] bogieXs = { sx + 18, sx + sw - 18 - wd * 2 - 6 };
            foreach (int bx in bogieXs)
            {
                for (int wi = 0; wi < 2; wi++)
                {
                    int wx = bx + wi * (wd + 6);
                    _text.DrawRect(sb, new Rectangle(wx, wheelY, wd, wd), new Color(48, 48, 54));
                    // Axle
                    _text.DrawRect(sb, new Rectangle(wx + wd / 2 - 2, wheelY + wd / 2 - 2, 4, 4),
                        new Color(100, 100, 112));
                    VoxelOutline(sb, wx, wheelY, wd, wd);
                }
                // Axle bar
                _text.DrawRect(sb, new Rectangle(bx + wd / 2, wheelY + wd / 2 - 1, wd + 6, 2),
                    new Color(58, 58, 68));
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  LOCOMOTIVE DETAILS  — voxel sub-blocks on the top face
        // ═══════════════════════════════════════════════════════════════
        public void DrawLocomotiveDetails(SpriteBatch sb, Camera camera, TrainCar engine)
        {
            float screenX = camera.WorldToScreenX(engine.WorldX);
            int sx = (int)screenX;
            int ew = (int)engine.Width;
            int topY = (int)Constants.VoxelTopY;

            // ── Chimney voxel block (RIGHT / front of engine) ──
            int chX = sx + ew - 55;
            int chW = 22;
            int chH = 30;
            int chY = topY - chH;

            // Chimney top face (lightest)
            _text.DrawRect(sb, new Rectangle(chX, chY, chW, 6), new Color(80, 80, 90));
            // Chimney front face
            _text.DrawRect(sb, new Rectangle(chX, chY + 6, chW, chH - 6), new Color(55, 55, 62));
            // Chimney right side face
            _text.DrawRect(sb, new Rectangle(chX + chW, chY, 6, chH), new Color(38, 38, 45));
            // Full outline
            VoxelOutline(sb, chX, chY, chW, chH);
            // Side outline
            _text.DrawRect(sb, new Rectangle(chX + chW, chY, 6, O), OL);
            _text.DrawRect(sb, new Rectangle(chX + chW + 4, chY, 2, chH), OL);
            _text.DrawRect(sb, new Rectangle(chX + chW, chY + chH - O, 6, O), OL);
            // Inner mouth
            _text.DrawRect(sb, new Rectangle(chX + O + 1, chY + O, chW - O * 2 - 2, 3),
                new Color(18, 18, 20));

            // ── Steam dome voxel block (center) ──
            int dX = sx + ew / 2;
            int dW = 18;
            int dH = 14;
            int dY = topY - dH;
            // Top
            _text.DrawRect(sb, new Rectangle(dX, dY, dW, 5), new Color(92, 92, 102));
            // Front
            _text.DrawRect(sb, new Rectangle(dX, dY + 5, dW, dH - 5), new Color(72, 72, 80));
            // Side
            _text.DrawRect(sb, new Rectangle(dX + dW, dY, 5, dH), new Color(50, 50, 58));
            VoxelOutline(sb, dX, dY, dW, dH);
            _text.DrawRect(sb, new Rectangle(dX + dW, dY, 5, O), OL);
            _text.DrawRect(sb, new Rectangle(dX + dW + 3, dY, 2, dH), OL);

            // ── Cab voxel block (LEFT / back of engine) ──
            int cabW = 52;
            int cabH = 20;
            int cabY = topY - cabH;
            // Top
            _text.DrawRect(sb, new Rectangle(sx, cabY, cabW, 6), new Color(78, 78, 105));
            // Front
            _text.DrawRect(sb, new Rectangle(sx, cabY + 6, cabW, cabH - 6), new Color(58, 58, 80));
            // Side
            _text.DrawRect(sb, new Rectangle(sx + cabW, cabY, 5, cabH), new Color(42, 42, 60));
            VoxelOutline(sb, sx, cabY, cabW, cabH);
            _text.DrawRect(sb, new Rectangle(sx + cabW, cabY, 5, O), OL);
            _text.DrawRect(sb, new Rectangle(sx + cabW + 3, cabY, 2, cabH), OL);
            // Cab window
            _text.DrawRect(sb, new Rectangle(sx + 10, cabY + 8, 18, 10), new Color(80, 102, 142));
            VoxelOutline(sb, sx + 10, cabY + 8, 18, 10);

            // ── Pipe between dome and chimney (on top face) ──
            int pipeY = topY - 4;
            int pipeX = dX + dW + 5;
            int pipeLen = chX - pipeX;
            if (pipeLen > 0)
            {
                _text.DrawRect(sb, new Rectangle(pipeX, pipeY, pipeLen, 3), new Color(72, 72, 80));
                VoxelOutline(sb, pipeX, pipeY, pipeLen, 3);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  CARGO DETAILS  — coal pile + Broetli crate as voxel blocks
        // ═══════════════════════════════════════════════════════════════
        public void DrawCargoDetails(SpriteBatch sb, Camera camera, TrainCar tender)
        {
            float screenX = camera.WorldToScreenX(tender.WorldX);
            int sx = (int)screenX;
            int tw = (int)tender.Width;
            int topY = (int)Constants.VoxelTopY;

            // ── Coal pile ──
            int coalW = tw - 28;
            int coalH = 14;
            int coalX = sx + 14;
            int coalY = topY - coalH;
            // Top face
            _text.DrawRect(sb, new Rectangle(coalX, coalY, coalW, 5), new Color(38, 38, 42));
            // Front face
            _text.DrawRect(sb, new Rectangle(coalX, coalY + 5, coalW, coalH - 5), new Color(25, 25, 28));
            // Side face
            _text.DrawRect(sb, new Rectangle(coalX + coalW, coalY, 5, coalH), new Color(18, 18, 22));
            VoxelOutline(sb, coalX, coalY, coalW, coalH);
            _text.DrawRect(sb, new Rectangle(coalX + coalW, coalY, 5, O), OL);
            _text.DrawRect(sb, new Rectangle(coalX + coalW + 3, coalY, 2, coalH), OL);
            // Lumps on top
            for (int cx = coalX + 4; cx < coalX + coalW - 4; cx += 9)
                _text.DrawRect(sb, new Rectangle(cx, coalY - 2, 6, 3), new Color(32, 32, 36));

            // ── Broetli crate ──
            int crW = 32;
            int crH = 16;
            int crX = sx + tw / 2 - crW / 2;
            int crY = coalY - crH + 3;
            // Top face
            _text.DrawRect(sb, new Rectangle(crX, crY, crW, 5), new Color(230, 192, 115));
            // Front face
            _text.DrawRect(sb, new Rectangle(crX, crY + 5, crW, crH - 5), new Color(200, 162, 82));
            // Side face
            _text.DrawRect(sb, new Rectangle(crX + crW, crY, 5, crH), new Color(170, 132, 58));
            VoxelOutline(sb, crX, crY, crW, crH);
            _text.DrawRect(sb, new Rectangle(crX + crW, crY, 5, O), OL);
            _text.DrawRect(sb, new Rectangle(crX + crW + 3, crY, 2, crH), OL);
            // Label
            _text.DrawString(sb, "B", new Vector2(crX + 10, crY + 5), Color.DarkRed);
        }

        // ── Helpers ──

        /// <summary>Draw a bold 3px outline around a rectangle (voxel edge style).</summary>
        private void VoxelOutline(SpriteBatch sb, int x, int y, int w, int h)
        {
            _text.DrawRect(sb, new Rectangle(x, y, w, O), OL);             // top
            _text.DrawRect(sb, new Rectangle(x, y + h - O, w, O), OL);     // bottom
            _text.DrawRect(sb, new Rectangle(x, y, O, h), OL);             // left
            _text.DrawRect(sb, new Rectangle(x + w - O, y, O, h), OL);     // right
        }

        private static Color Darken(Color c, int amount)
        {
            return new Color(Math.Max(c.R - amount, 0), Math.Max(c.G - amount, 0),
                Math.Max(c.B - amount, 0), c.A);
        }

        private static Color Lighten(Color c, int amount)
        {
            return new Color(Math.Min(c.R + amount, 255), Math.Min(c.G + amount, 255),
                Math.Min(c.B + amount, 255), c.A);
        }
    }
}
