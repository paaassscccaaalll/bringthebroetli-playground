using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BringTheBrotli.World
{
    /// <summary>
    /// Simple particle system for smoke and steam effects.
    /// Particles are drawn using spritesheet frames from TextureAtlas.
    /// Max 80 active particles at once.
    /// </summary>
    public class ParticleSystem
    {
        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;       // remaining seconds
            public float MaxLife;
            public float Scale;
            public bool IsSteam;     // false = smoke, true = steam
            public float Alpha;
        }

        private readonly List<Particle> _particles = new(MaxParticles);
        private const int MaxParticles = 80;
        private static readonly Random _rng = new();

        // Emitter state
        private float _smokeTimer;
        private float _steamTimer;

        /// <summary>Spawn a burst of smoke particles at the given world position.</summary>
        public void EmitSmoke(float worldX, float worldY, int count = 3)
        {
            for (int i = 0; i < count && _particles.Count < MaxParticles; i++)
            {
                _particles.Add(new Particle
                {
                    Position = new Vector2(worldX + (float)(_rng.NextDouble() * 10 - 5),
                                           worldY + (float)(_rng.NextDouble() * 4 - 2)),
                    Velocity = new Vector2((float)(_rng.NextDouble() * 20 - 10),
                                           -20f - (float)(_rng.NextDouble() * 30)),
                    Life = 1.0f + (float)(_rng.NextDouble() * 0.5),
                    MaxLife = 1.5f,
                    Scale = 0.8f + (float)(_rng.NextDouble() * 0.4),
                    IsSteam = false,
                    Alpha = 1f
                });
            }
        }

        /// <summary>Spawn a burst of steam particles at the given world position.</summary>
        public void EmitSteam(float worldX, float worldY, int count = 2)
        {
            for (int i = 0; i < count && _particles.Count < MaxParticles; i++)
            {
                _particles.Add(new Particle
                {
                    Position = new Vector2(worldX + (float)(_rng.NextDouble() * 8 - 4),
                                           worldY + (float)(_rng.NextDouble() * 4 - 2)),
                    Velocity = new Vector2((float)(_rng.NextDouble() * 30 - 15),
                                           -30f - (float)(_rng.NextDouble() * 20)),
                    Life = 0.6f + (float)(_rng.NextDouble() * 0.4),
                    MaxLife = 1.0f,
                    Scale = 0.6f + (float)(_rng.NextDouble() * 0.3),
                    IsSteam = true,
                    Alpha = 1f
                });
            }
        }

        /// <summary>
        /// Update all particles. Call once per frame.
        /// Also handles continuous chimney smoke emission.
        /// </summary>
        public void Update(float dt, float chimneyWorldX, float chimneyWorldY, float trainSpeed,
                            bool steamLeakActive, float steamVentWorldX, float steamVentWorldY)
        {
            // Continuous chimney smoke (rate increases with speed)
            _smokeTimer += dt;
            float smokeInterval = trainSpeed > 10f ? 0.08f : 0.2f;
            if (_smokeTimer >= smokeInterval)
            {
                _smokeTimer -= smokeInterval;
                EmitSmoke(chimneyWorldX, chimneyWorldY, trainSpeed > 50f ? 2 : 1);
            }

            // Steam leak particles
            if (steamLeakActive)
            {
                _steamTimer += dt;
                if (_steamTimer >= 0.15f)
                {
                    _steamTimer -= 0.15f;
                    EmitSteam(steamVentWorldX, steamVentWorldY, 2);
                }
            }

            // Update existing particles
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Life -= dt;
                if (p.Life <= 0)
                {
                    _particles.RemoveAt(i);
                    continue;
                }

                p.Position += p.Velocity * dt;
                // Slow down horizontal, accelerate upward drift
                p.Velocity.X *= 0.98f;
                p.Velocity.Y -= 5f * dt; // drift upward
                // Fade out
                p.Alpha = Math.Clamp(p.Life / p.MaxLife, 0f, 1f);
                // Grow slightly
                p.Scale += 0.3f * dt;
                _particles[i] = p;
            }
        }

        /// <summary>Draw all active particles.</summary>
        public void Draw(SpriteBatch sb, Camera camera)
        {
            foreach (var p in _particles)
            {
                var tex = p.IsSteam ? TextureAtlas.ParticleSteam : TextureAtlas.ParticleSmoke;

                // Pick frame based on remaining life (earlier frames for younger particles)
                float lifePct = 1f - Math.Clamp(p.Life / p.MaxLife, 0f, 1f);
                int frame = (int)(lifePct * (TextureAtlas.ParticleFrameCount - 1));
                var srcRect = TextureAtlas.GetParticleFrame(frame);

                float screenX = camera.WorldToScreenX(p.Position.X);
                float screenY = p.Position.Y;

                int drawSize = (int)(TextureAtlas.ParticleFrameSize * p.Scale * 2f);
                var dest = new Rectangle(
                    (int)(screenX - drawSize / 2f),
                    (int)(screenY - drawSize / 2f),
                    drawSize, drawSize);

                byte alpha = (byte)(p.Alpha * 200);
                sb.Draw(tex, dest, srcRect, new Color(255, 255, 255, (int)alpha));
            }
        }

        /// <summary>Remove all particles.</summary>
        public void Clear()
        {
            _particles.Clear();
            _smokeTimer = 0f;
            _steamTimer = 0f;
        }
    }
}
