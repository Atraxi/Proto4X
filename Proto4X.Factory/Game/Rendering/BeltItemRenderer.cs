using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Belts;
using Proto4X.Factory.Simulation.Grid;
using System.Collections.Generic;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Items riding belts, read straight out of the lane segments - they are not entities, so there is
    /// nothing for the scheduler to hand this system. Curves are drawn as if straight along the tile's
    /// own direction, which is wrong by a few pixels on the corner tile and nowhere else.
    /// </summary>
    public sealed class BeltItemRenderer(Surface surface, Texture2D pixel) : IDrawSystem
    {
        public DrawSpace DrawSpace => DrawSpace.World;
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(SpriteBatch spriteBatch, Rectangle cullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var frame = surface.Frame;
            var buildings = surface.Buildings;
            var visible = SurfaceView.VisibleTiles(frame, cullingBounds, surface.Grid);
            var itemSize = new Vector2(SurfaceView.TileSize * 0.22f);

            foreach (var segment in surface.Belts.Segments)
            {
                var position = segment.Length;
                for (var i = 0; i < segment.Count; i++)
                {
                    position -= segment.Gaps[i];
                    var tileIndex = System.Math.Min(position / BeltNetwork.TileLength, segment.TileCount - 1);
                    var slot = segment.TileSlots[tileIndex];
                    var x = buildings.OriginX(slot);
                    var y = buildings.OriginY(slot);
                    if (!SurfaceView.IsVisible(visible, x, y, 1))
                    {
                        continue;
                    }

                    var along = (position - tileIndex * BeltNetwork.TileLength) / (float)BeltNetwork.TileLength - 0.5f;
                    var direction = buildings.DirectionOf(slot);
                    var forward = SurfaceView.DirectionVector(direction);
                    var left = SurfaceView.DirectionVector(direction.Left());
                    var lateral = segment.Lane == 0 ? 0.24f : -0.24f;
                    var local = new Vector2(x + 0.5f, y + 0.5f) + forward * along + left * lateral;

                    Primitives.FillCentred(spriteBatch, pixel, SurfaceView.TileToWorld(frame, local), itemSize, frame.Rotation, Palette.Item(segment.Items[i]));
                }
            }
        }
    }
}
