using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System.Collections.Generic;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Ground, deck and ore for the visible tiles. Void draws nothing, which is what makes a ship's
    /// grid read as a hull floating in space rather than a rectangle.
    /// </summary>
    public sealed class TerrainRenderer(Surface surface) : IGeometryDrawSystem
    {
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var grid = surface.Grid;
            var visible = SurfaceView.VisibleTiles(localCullingBounds, grid);
            var oreSize = new Vector2(0.7f);

            for (var y = visible.MinY; y <= visible.MaxY; y++)
            {
                for (var x = visible.MinX; x <= visible.MaxX; x++)
                {
                    var tile = grid.TileAt(x, y);
                    if (tile == TileKind.Void)
                    {
                        continue;
                    }

                    var shade = ((x ^ y) & 1) == 0 ? 1f : 0.93f;
                    var colour = Palette.Tile(tile) * shade;
                    colour.A = 255;
                    Shapes.FillRect(batch, GeometryLayer.Opaque, x, y, 1f, 1f, FlatDepth.Terrain, colour);

                    var ore = grid.OreAt(x, y);
                    if (ore != ItemId.None)
                    {
                        // Palette.Ore is a premultiplied 55% wash, blended over the ground as before.
                        Shapes.FillCentred(batch, GeometryLayer.Translucent, new Vector2(x + 0.5f, y + 0.5f), oreSize, 0.785f, FlatDepth.Ore, Palette.Ore(ore));
                    }
                }
            }
        }
    }
}
