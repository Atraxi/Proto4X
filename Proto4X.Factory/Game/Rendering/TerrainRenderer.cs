using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
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
    public sealed class TerrainRenderer(Surface surface, Texture2D pixel) : IDrawSystem
    {
        public DrawSpace DrawSpace => DrawSpace.World;
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(SpriteBatch spriteBatch, Rectangle cullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var frame = surface.Frame;
            var grid = surface.Grid;
            var visible = SurfaceView.VisibleTiles(frame, cullingBounds, grid);
            var tileSize = new Vector2(SurfaceView.TileSize + 0.6f);
            var oreSize = new Vector2(SurfaceView.TileSize * 0.7f);

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
                    Primitives.FillRectangle(spriteBatch, pixel, SurfaceView.TileToWorld(frame, new Vector2(x, y)), tileSize, frame.Rotation, Palette.Tile(tile) * shade);

                    var ore = grid.OreAt(x, y);
                    if (ore != ItemId.None)
                    {
                        Primitives.FillCentred(spriteBatch, pixel, SurfaceView.TileToWorld(frame, new Vector2(x + 0.5f, y + 0.5f)), oreSize, frame.Rotation + 0.785f, Palette.Ore(ore));
                    }
                }
            }
        }
    }
}
