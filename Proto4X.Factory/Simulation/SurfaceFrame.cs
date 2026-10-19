using Microsoft.Xna.Framework;

namespace Proto4X.Factory.Simulation
{
    /// <summary>
    /// Where a surface's tile grid sits in the space that contains it.
    /// </summary>
    /// <remarks>
    /// A planet's frame never moves. A ship is a whole surface - its own grid, stores, GameWorld and
    /// systems, running the same factory simulation as a planet - whose grid is carried around by a
    /// moving frame. Nothing inside the ship's simulation knows or cares that it's moving: belts,
    /// inserters and machines all work in grid coordinates, and the frame only matters to rendering,
    /// picking, and (later) whatever flies the ship.
    /// <para>
    /// That separation is the whole design for freely designed, moving ships: ship-internal logistics
    /// are a solved problem (they're the factory), and ship-as-a-body is a separate, much smaller
    /// rigid-body problem over one transform per ship. Ship-to-ship interaction is out of scope.
    /// </para>
    /// <para>
    /// Floats, for now. A lockstep game would want fixed-point here like everywhere else in the sim;
    /// that's noted in DESIGN.md rather than solved, since nothing flies under player control yet.
    /// </para>
    /// </remarks>
    public struct SurfaceFrame
    {
        /// <summary>World position of the grid's (0, 0) corner, in tiles.</summary>
        public Vector2 Position;

        /// <summary>Radians, clockwise in screen space.</summary>
        public float Rotation;

        /// <summary>Tiles per tick.</summary>
        public Vector2 Velocity;

        /// <summary>Radians per tick.</summary>
        public float AngularVelocity;

        /// <summary>Grid-local tile position to world tile position.</summary>
        public readonly Vector2 LocalToWorld(Vector2 local)
        {
            if (Rotation == 0f)
            {
                return Position + local;
            }

            var cos = System.MathF.Cos(Rotation);
            var sin = System.MathF.Sin(Rotation);
            return Position + new Vector2(local.X * cos - local.Y * sin, local.X * sin + local.Y * cos);
        }

        public readonly Vector2 WorldToLocal(Vector2 world)
        {
            var offset = world - Position;
            if (Rotation == 0f)
            {
                return offset;
            }

            var cos = System.MathF.Cos(-Rotation);
            var sin = System.MathF.Sin(-Rotation);
            return new Vector2(offset.X * cos - offset.Y * sin, offset.X * sin + offset.Y * cos);
        }

        public readonly bool IsMoving => Velocity != Vector2.Zero || AngularVelocity != 0f;

        internal void Integrate()
        {
            Position += Velocity;
            Rotation += AngularVelocity;
        }
    }
}
