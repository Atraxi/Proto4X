using Microsoft.Xna.Framework;
using Proto4X.Factory.Simulation.Grid;
using System;

namespace Proto4X.Factory.Rendering
{
    /// <summary>Tile-range helpers for renderers that walk a surface's grid in its local tile units.</summary>
    public static class SurfaceView
    {
        /// <summary>
        /// The block of grid tiles that can overlap the scheduler's local culling bounds, padded by
        /// <paramref name="padding"/> tiles and clamped to the grid. Anything drawn above the ground
        /// can be seen from outside the ground footprint, so taller things pass a bigger padding.
        /// </summary>
        public static (int MinX, int MinY, int MaxX, int MaxY) VisibleTiles(Rectangle localBounds, SurfaceGrid grid, int padding = 1)
        {
            return (
                Math.Max(0, localBounds.Left - padding),
                Math.Max(0, localBounds.Top - padding),
                Math.Min(grid.Width - 1, localBounds.Right + padding),
                Math.Min(grid.Height - 1, localBounds.Bottom + padding));
        }

        public static bool IsVisible((int MinX, int MinY, int MaxX, int MaxY) visible, int x, int y, int size) =>
            x + size - 1 >= visible.MinX && x <= visible.MaxX && y + size - 1 >= visible.MinY && y <= visible.MaxY;

        public static Vector2 DirectionVector(Direction direction) => new(direction.Dx(), direction.Dy());
    }
}
