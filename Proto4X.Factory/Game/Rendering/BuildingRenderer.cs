using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Components;
using Proto4X.Factory.Simulation.Data;
using System.Collections.Generic;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// The body of every building except belts and inserters, which have their own renderers. Shows
    /// the recipe's product as a swatch on assemblers and a fill bar of a chest's contents - read from
    /// the stores, since that's where inventories live.
    /// </summary>
    public sealed class BuildingRenderer(Surface surface, Texture2D pixel) : IDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Building>(), ComponentRef.Of<Placement>()];

        public DrawSpace DrawSpace => DrawSpace.World;
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(SpriteBatch spriteBatch, Rectangle cullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var frame = surface.Frame;
            var visible = SurfaceView.VisibleTiles(frame, cullingBounds, surface.Grid);
            var buildings = surface.Buildings;
            var inventories = surface.Inventories;

            foreach (var chunk in archetypeChunks)
            {
                var buildingSpan = chunk.GetSpan<Building>();
                var placementSpan = chunk.GetSpan<Placement>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var building = ref buildingSpan[i];
                    if (building.Kind is BuildingKind.Belt or BuildingKind.Inserter)
                    {
                        continue;
                    }

                    ref var placement = ref placementSpan[i];
                    if (!SurfaceView.IsVisible(visible, placement.X, placement.Y, placement.Size))
                    {
                        continue;
                    }

                    var size = placement.Size;
                    var centre = new Vector2(placement.X + size * 0.5f, placement.Y + size * 0.5f);
                    var worldCentre = SurfaceView.TileToWorld(frame, centre);
                    var colour = Palette.Building(building.Kind);
                    if (buildings.IsPendingRemoval(building.Slot))
                    {
                        colour *= 0.4f;
                    }

                    if (building.Kind == BuildingKind.Pole)
                    {
                        Primitives.FillCentred(spriteBatch, pixel, worldCentre, new Vector2(SurfaceView.TileSize * 0.35f), frame.Rotation, colour);
                        continue;
                    }

                    Primitives.FillCentred(spriteBatch, pixel, worldCentre, new Vector2(SurfaceView.TileSize * (size - 0.15f)), frame.Rotation, colour);
                    Primitives.FillCentred(spriteBatch, pixel, worldCentre, new Vector2(SurfaceView.TileSize * (size - 0.45f)), frame.Rotation, colour * 0.75f);

                    // A notch on the facing edge, for the kinds where facing matters.
                    if (building.Kind is BuildingKind.Drill or BuildingKind.Collector)
                    {
                        var forward = SurfaceView.DirectionVector(placement.Direction);
                        Primitives.FillCentred(spriteBatch, pixel, SurfaceView.TileToWorld(frame, centre + forward * (size * 0.5f - 0.15f)),
                            new Vector2(SurfaceView.TileSize * 0.3f), frame.Rotation, Color.Black * 0.6f);
                    }

                    if (building.Kind == BuildingKind.Assembler)
                    {
                        var result = Recipes.Get(buildings.RecipeOf(building.Slot)).Result;
                        Primitives.FillCentred(spriteBatch, pixel, worldCentre, new Vector2(SurfaceView.TileSize * 0.8f), frame.Rotation + 0.785f, Palette.Item(result));
                    }

                    var start = buildings.InventoryStart(building.Slot);
                    if (start >= 0 && building.Kind is BuildingKind.Chest or BuildingKind.Receiver or BuildingKind.LaunchSilo)
                    {
                        DrawFill(spriteBatch, frame, placement, inventories, start, buildings.InventoryLength(building.Slot));
                    }
                }
            }
        }

        /// <summary>One thin bar per occupied stack, coloured by item, filled by how full it is.</summary>
        private void DrawFill(SpriteBatch spriteBatch, in SurfaceFrame frame, in Placement placement, Simulation.Stores.InventoryStore inventories, int start, int length)
        {
            var width = placement.Size * 0.8f;
            var barWidth = width / length;
            for (var k = 0; k < length; k++)
            {
                var count = inventories.CountAt(start + k);
                if (count == 0)
                {
                    continue;
                }

                var fill = count / (float)inventories.LimitAt(start + k);
                var corner = new Vector2(placement.X + placement.Size * 0.1f + k * barWidth, placement.Y + placement.Size * 0.85f - 0.5f * fill * placement.Size);
                Primitives.FillRectangle(spriteBatch, pixel, SurfaceView.TileToWorld(frame, corner),
                    new Vector2(barWidth * SurfaceView.TileSize, 0.5f * fill * placement.Size * SurfaceView.TileSize), frame.Rotation,
                    Palette.Item(inventories.ItemAt(start + k)));
            }
        }
    }
}
