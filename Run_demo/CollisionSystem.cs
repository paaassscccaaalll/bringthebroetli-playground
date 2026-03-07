using System;
using Microsoft.Xna.Framework;

namespace BringTheBrotliDemo
{
    /// <summary>
    /// Standalone 2.5D collision system.
    ///
    /// All collision is based on the Baker's **foot anchor** — a single
    /// point in screen space.  The sprite body extends upward visually
    /// but plays no part in collision.
    ///
    /// Responsibilities
    /// ────────────────
    ///  • Constrain the foot anchor inside the surface boundary polygon.
    ///  • Push the foot anchor out of obstacle rectangles (with axis
    ///    separation so the Baker slides along walls).
    ///  • Detect which action zone (if any) the foot anchor is inside.
    /// </summary>
    public class CollisionSystem
    {
        // ---------------------------------------------------------------
        // Level data (screen-space, set by Train after loading)
        // ---------------------------------------------------------------

        /// <summary>Walkable surface boundary polygon (screen space).</summary>
        public Vector2[] SurfaceBoundary { get; set; } = Array.Empty<Vector2>();

        /// <summary>Obstacle rectangles in screen space.</summary>
        public ObstacleRect[] Obstacles { get; set; } = Array.Empty<ObstacleRect>();

        /// <summary>Action zone rectangles in screen space.</summary>
        public ActionZone[] ActionZones { get; set; } = Array.Empty<ActionZone>();

        /// <summary>Jump barrier rectangles in screen space — block walking, passable when airborne.</summary>
        public Rectangle[] JumpBarriers { get; set; } = Array.Empty<Rectangle>();

        // ---------------------------------------------------------------
        // Public data types
        // ---------------------------------------------------------------

        public struct ObstacleRect
        {
            public string Label;
            public Rectangle Bounds;
        }

        public struct ActionZone
        {
            public string Label;
            public Rectangle Bounds;
        }

        // ---------------------------------------------------------------
        // Movement resolution
        // ---------------------------------------------------------------

        /// <summary>
        /// Resolve a movement attempt (grounded):
        ///  1. Try full move (X+Y) → clamp to surface → push out of obstacles + barriers.
        ///  2. If blocked, try X-only, then Y-only (axis separation for sliding).
        ///  3. If both axes blocked, stay at current position.
        /// </summary>
        public Vector2 ResolveMovement(Vector2 current, Vector2 desired)
        {
            if (SurfaceBoundary.Length < 3)
                return desired;

            Vector2 candidate = ClampAndPush(desired);
            if (!IsStuck(candidate, current, desired))
                return candidate;

            Vector2 xOnly = new Vector2(desired.X, current.Y);
            Vector2 xRes = ClampAndPush(xOnly);
            bool xOk = !IsStuck(xRes, current, xOnly);

            Vector2 yOnly = new Vector2(current.X, desired.Y);
            Vector2 yRes = ClampAndPush(yOnly);
            bool yOk = !IsStuck(yRes, current, yOnly);

            if (xOk && yOk)
                return ClampAndPush(new Vector2(xRes.X, yRes.Y));
            if (xOk)
                return xRes;
            if (yOk)
                return yRes;

            return current;
        }

        /// <summary>
        /// Resolve movement while airborne — obstacles and jump barriers are
        /// ignored, but the foot anchor is still clamped to the surface
        /// boundary's horizontal (X) extent.
        /// </summary>
        public Vector2 ResolveMovementAirborne(Vector2 current, Vector2 desired)
        {
            if (SurfaceBoundary.Length < 3)
                return desired;

            // Just clamp inside the surface boundary polygon — no obstacles/barriers.
            if (!IsPointInPolygon(desired, SurfaceBoundary))
                desired = NearestPointOnPolygon(desired, SurfaceBoundary);

            return desired;
        }

        /// <summary>
        /// Check whether a foot-anchor position is a valid landing spot:
        /// inside the surface boundary, not inside any obstacle, and not
        /// inside any jump barrier.
        /// </summary>
        public bool IsLandingValid(Vector2 point)
        {
            if (SurfaceBoundary.Length < 3)
                return true;

            if (!IsPointInPolygon(point, SurfaceBoundary))
                return false;

            for (int i = 0; i < Obstacles.Length; i++)
                if (Obstacles[i].Bounds.Contains((int)point.X, (int)point.Y))
                    return false;

            for (int i = 0; i < JumpBarriers.Length; i++)
                if (JumpBarriers[i].Contains((int)point.X, (int)point.Y))
                    return false;

            return true;
        }

        /// <summary>
        /// Returns the label of the action zone containing the foot anchor,
        /// or null if the foot is not in any zone.
        /// </summary>
        public string? GetActiveZone(Vector2 footPosition)
        {
            for (int i = 0; i < ActionZones.Length; i++)
            {
                if (ActionZones[i].Bounds.Contains(
                        (int)footPosition.X, (int)footPosition.Y))
                    return ActionZones[i].Label;
            }
            return null;
        }

        // ---------------------------------------------------------------
        // Internal helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Clamp point to surface boundary, then push out of all obstacles
        /// and jump barriers.
        /// </summary>
        private Vector2 ClampAndPush(Vector2 point)
        {
            if (!IsPointInPolygon(point, SurfaceBoundary))
                point = NearestPointOnPolygon(point, SurfaceBoundary);

            for (int i = 0; i < Obstacles.Length; i++)
                point = PushOutOfRect(point, Obstacles[i].Bounds);

            for (int i = 0; i < JumpBarriers.Length; i++)
                point = PushOutOfRect(point, JumpBarriers[i]);

            if (!IsPointInPolygon(point, SurfaceBoundary))
                point = NearestPointOnPolygon(point, SurfaceBoundary);

            return point;
        }

        /// <summary>
        /// Check if the resolved position is effectively stuck (moved
        /// backward or negligibly).
        /// </summary>
        private static bool IsStuck(Vector2 resolved, Vector2 current, Vector2 desired)
        {
            Vector2 intent = desired - current;
            Vector2 actual = resolved - current;
            // Stuck if the dot product is ≤ 0 (moved backward or not at all)
            // and the movement was non-trivial.
            if (intent.LengthSquared() < 0.01f)
                return false;
            return Vector2.Dot(intent, actual) <= 0.01f;
        }

        /// <summary>
        /// If the point is inside the rectangle, push it out from the
        /// nearest edge (minimum penetration axis).
        /// </summary>
        private static Vector2 PushOutOfRect(Vector2 point, Rectangle rect)
        {
            if (!rect.Contains((int)point.X, (int)point.Y))
                return point;

            // Compute penetration from each edge.
            float pushLeft  = point.X - rect.Left;
            float pushRight = rect.Right - point.X;
            float pushUp    = point.Y - rect.Top;
            float pushDown  = rect.Bottom - point.Y;

            float min = pushLeft;
            Vector2 result = new Vector2(rect.Left - 1f, point.Y);

            if (pushRight < min)
            {
                min = pushRight;
                result = new Vector2(rect.Right + 1f, point.Y);
            }
            if (pushUp < min)
            {
                min = pushUp;
                result = new Vector2(point.X, rect.Top - 1f);
            }
            if (pushDown < min)
            {
                result = new Vector2(point.X, rect.Bottom + 1f);
            }

            return result;
        }

        // ---------------------------------------------------------------
        // Polygon geometry (static helpers)
        // ---------------------------------------------------------------

        /// <summary>
        /// Ray-casting (even-odd rule) point-in-polygon test.
        /// </summary>
        public static bool IsPointInPolygon(Vector2 point, Vector2[] poly)
        {
            if (poly.Length < 3) return false;

            bool inside = false;
            int n = poly.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if ((poly[i].Y > point.Y) != (poly[j].Y > point.Y) &&
                    point.X < (poly[j].X - poly[i].X) * (point.Y - poly[i].Y)
                              / (poly[j].Y - poly[i].Y) + poly[i].X)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>
        /// Find the nearest point on the polygon boundary.
        /// </summary>
        public static Vector2 NearestPointOnPolygon(Vector2 point, Vector2[] poly)
        {
            float bestDistSq = float.MaxValue;
            Vector2 best = point;
            int n = poly.Length;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vector2 closest = ClosestPointOnSegment(point, poly[i], poly[j]);
                float distSq = Vector2.DistanceSquared(point, closest);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = closest;
                }
            }
            return best;
        }

        /// <summary>Closest point on line segment AB to point P.</summary>
        private static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.LengthSquared();
            if (lenSq < 0.0001f) return a;

            float t = MathHelper.Clamp(
                Vector2.Dot(p - a, ab) / lenSq, 0f, 1f);
            return a + ab * t;
        }
    }
}
