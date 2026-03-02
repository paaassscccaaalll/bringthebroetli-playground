using System;
using BringTheBrotli.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotli.World
{
    /// <summary>
    /// Crossy-Road-style background: sky gradient, distant hills, and a continuous
    /// ground plane that fills behind and in front of the voxel train.
    /// </summary>
    public class BackgroundRenderer
    {
        private readonly TextRenderer _text;

        private float _hillOffset;
        private float _groundOffset;
        private float _cloudOffset;

        // Colors
        private static readonly Color SkyTop = new(95, 155, 225);
        private static readonly Color SkyBottom = new(175, 215, 245);
        private static readonly Color GrassA = new(95, 180, 65);
        private static readonly Color GrassB = new(80, 155, 50);
        private static readonly Color GrassC = new(65, 135, 40);
        private static readonly Color Dirt = new(130, 100, 65);
        private static readonly Color DirtDark = new(100, 75, 50);
        private static readonly Color TrackBed = new(110, 95, 75);
        private static readonly Color Rail = new(60, 60, 68);
        private static readonly Color Sleeper = new(100, 70, 45);
        private static readonly Color Outline = new(25, 25, 30);

        public BackgroundRenderer(TextRenderer text) { _text = text; }

        public void Update(float trainSpeed, float dt)
        {
            float scroll = trainSpeed * 2f;
            _cloudOffset += scroll * 0.05f * dt;
            _hillOffset += scroll * 0.15f * dt;
            _groundOffset += scroll * 1.0f * dt;
        }

        // ═══════════════════════════════════════════════
        //  SKY  (gradient + sun + clouds)
        // ═══════════════════════════════════════════════
        public void DrawSky(SpriteBatch sb)
        {
            int skyH = (int)(Constants.SkyBottomY - Constants.SkyTopY);
            int bands = 5;
            int bh = skyH / bands + 1;
            for (int i = 0; i < bands; i++)
            {
                float t = i / (float)(bands - 1);
                _text.DrawRect(sb, new Rectangle(0, (int)Constants.SkyTopY + i * bh,
                    Constants.ScreenWidth, bh), LerpColor(SkyTop, SkyBottom, t));
            }

            // Sun
            _text.DrawRect(sb, new Rectangle(1055, 18, 44, 44), new Color(255, 240, 190, 50));
            _text.DrawRect(sb, new Rectangle(1063, 26, 28, 28), new Color(255, 248, 210, 80));

            // Clouds
            float cOff = -(_cloudOffset % 1600f);
            if (cOff > 0) cOff -= 1600f;
            DrawCloud(sb, cOff + 80, 20, 100, 16);
            DrawCloud(sb, cOff + 380, 50, 80, 12);
            DrawCloud(sb, cOff + 680, 10, 130, 18);
            DrawCloud(sb, cOff + 1020, 55, 70, 11);
            DrawCloud(sb, cOff + 1380, 30, 95, 14);
        }

        private void DrawCloud(SpriteBatch sb, float x, float y, int w, int h)
        {
            _text.DrawRect(sb, new Rectangle((int)x, (int)y, w, h), new Color(240, 245, 255, 80));
            _text.DrawRect(sb, new Rectangle((int)x + w / 4, (int)y - h / 3, w / 2, h / 2),
                new Color(245, 248, 255, 55));
        }

        // ═══════════════════════════════════════════════
        //  HILLS  (parallax, distant)
        // ═══════════════════════════════════════════════
        public void DrawHills(SpriteBatch sb)
        {
            var tex = TextureAtlas.BackgroundHills;
            int hillH = (int)(Constants.GroundStartY - Constants.HillsY);
            int texW = tex.Width;

            float offset = -(_hillOffset % texW);
            if (offset > 0) offset -= texW;

            for (float x = offset; x < Constants.ScreenWidth + texW; x += texW)
            {
                sb.Draw(tex, new Rectangle((int)x, (int)Constants.HillsY, texW, hillH),
                    new Rectangle(0, 0, texW, tex.Height), Color.White);
            }

            // Haze transition
            for (int i = 0; i < 6; i++)
                _text.DrawRect(sb, new Rectangle(0, (int)Constants.GroundStartY - 6 + i,
                    Constants.ScreenWidth, 1), new Color(170, 210, 245, 8 + i * 5));
        }

        // ═══════════════════════════════════════════════
        //  GROUND PLANE  (continuous, behind + under + in front of train)
        // ═══════════════════════════════════════════════
        public void DrawGround(SpriteBatch sb)
        {
            int w = Constants.ScreenWidth;
            int top = (int)Constants.GroundStartY;
            int bot = (int)Constants.HudTopY;

            // ── Main grass fill ──
            _text.DrawRect(sb, new Rectangle(0, top, w, bot - top), GrassA);

            // Scrolling grass stripe texture
            float gOff = -(_groundOffset * 0.3f % 60f);
            if (gOff > 0) gOff -= 60f;
            for (float gx = gOff; gx < w + 30; gx += 30)
            {
                _text.DrawRect(sb, new Rectangle((int)gx, top + 8, 14, 3), GrassB);
                _text.DrawRect(sb, new Rectangle((int)gx + 15, top + 20, 10, 2), GrassC);
            }

            // ── Darker grass band where train will sit ──
            _text.DrawRect(sb, new Rectangle(0, (int)Constants.VoxelTopY - 6, w, 6), GrassC);

            // ── Dirt/earth zone (behind the front face, visible in gaps) ──
            int dirtTop = (int)Constants.VoxelFrontY;
            int dirtBot = (int)Constants.VoxelBottomY + 4;
            _text.DrawRect(sb, new Rectangle(0, dirtTop, w, dirtBot - dirtTop), Dirt);
            _text.DrawRect(sb, new Rectangle(0, dirtTop, w, 3), DirtDark);

            // ── Track bed ──
            int tbY = (int)Constants.VoxelBottomY - 2;
            _text.DrawRect(sb, new Rectangle(0, tbY, w, bot - tbY), TrackBed);
            _text.DrawRect(sb, new Rectangle(0, tbY, w, 3), Outline);

            // ── Sleepers (scrolling) ──
            float tOff = -(_groundOffset % 28f);
            if (tOff > 0) tOff -= 28f;
            for (float tx = tOff; tx < w + 28; tx += 28)
            {
                int slX = (int)tx;
                int slY = (int)Constants.TrackY - 1;
                _text.DrawRect(sb, new Rectangle(slX, slY, 14, 14), Sleeper);
                // Sleeper outline (voxel style)
                _text.DrawRect(sb, new Rectangle(slX, slY, 14, 2), Outline);
                _text.DrawRect(sb, new Rectangle(slX, slY + 12, 14, 2), Outline);
                _text.DrawRect(sb, new Rectangle(slX, slY, 2, 14), Outline);
                _text.DrawRect(sb, new Rectangle(slX + 12, slY, 2, 14), Outline);
            }

            // ── Rails ──
            int railY1 = (int)Constants.TrackY + 2;
            int railY2 = (int)Constants.TrackY + 9;
            _text.DrawRect(sb, new Rectangle(0, railY1, w, 3), Rail);
            _text.DrawRect(sb, new Rectangle(0, railY1, w, 1), new Color(85, 85, 92));
            _text.DrawRect(sb, new Rectangle(0, railY2, w, 3), Rail);
            _text.DrawRect(sb, new Rectangle(0, railY2, w, 1), new Color(85, 85, 92));

            // ── Foreground grass below tracks ──
            int fgY = (int)Constants.GroundY + 2;
            _text.DrawRect(sb, new Rectangle(0, fgY, w, bot - fgY), GrassB);
            _text.DrawRect(sb, new Rectangle(0, bot - 4, w, 4), GrassC);

            // ── Bold outline at HUD boundary ──
            _text.DrawRect(sb, new Rectangle(0, bot - 2, w, 2), Outline);
        }

        public void Reset()
        {
            _hillOffset = 0f;
            _groundOffset = 0f;
            _cloudOffset = 0f;
        }

        private static Color LerpColor(Color a, Color b, float t)
        {
            return new Color(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }
}
