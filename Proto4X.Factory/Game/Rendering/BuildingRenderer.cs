using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Graphics;
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
    public sealed class BuildingRenderer(Surface surface) : IGeometryDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Building>(), ComponentRef.Of<Placement>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var visible = SurfaceView.VisibleTiles(localCullingBounds, surface.Grid, padding: 3);
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
                    var colour = Palette.Building(building.Kind);
                    var layer = GeometryLayer.Opaque;
                    if (buildings.IsPendingRemoval(building.Slot))
                    {
                        colour *= 0.4f;
                        layer = GeometryLayer.Translucent;
                    }

                    if (building.Kind == BuildingKind.Pole)
                    {
                        Shapes.FillCentred(batch, layer, centre, new Vector2(0.35f), 0f, FlatDepth.Building, colour);
                        continue;
                    }

                    var inset = colour * 0.75f;
                    inset.A = colour.A;
                    Shapes.FillCentred(batch, layer, centre, new Vector2(size - 0.15f), 0f, FlatDepth.Building, colour);
                    Shapes.FillCentred(batch, layer, centre, new Vector2(size - 0.45f), 0f, FlatDepth.BuildingInset, inset);

                    // A notch on the facing edge, for the kinds where facing matters.
                    if (building.Kind is BuildingKind.Drill or BuildingKind.Collector)
                    {
                        var forward = SurfaceView.DirectionVector(placement.Direction);
                        Shapes.FillCentred(batch, GeometryLayer.Translucent, centre + forward * (size * 0.5f - 0.15f), new Vector2(0.3f), 0f, FlatDepth.BuildingDetail, Color.Black * 0.6f);
                    }

                    if (building.Kind == BuildingKind.Assembler)
                    {
                        var result = Recipes.Get(buildings.RecipeOf(building.Slot)).Result;
                        Shapes.FillCentred(batch, layer, centre, new Vector2(0.8f), 0.785f, FlatDepth.BuildingDetail, Palette.Item(result));
                    }

                    var start = buildings.InventoryStart(building.Slot);
                    if (start >= 0 && building.Kind is BuildingKind.Chest or BuildingKind.Receiver or BuildingKind.LaunchSilo)
                    {
                        DrawFill(batch, placement, inventories, start, buildings.InventoryLength(building.Slot));
                    }
                }
            }
        }

        /// <summary>One thin bar per occupied stack, coloured by item, filled by how full it is.</summary>
        private static void DrawFill(GeometryBatch batch, in Placement placement, Simulation.Stores.InventoryStore inventories, int start, int length)
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
                var height = 0.5f * fill * placement.Size;
                Shapes.FillRect(batch, GeometryLayer.Opaque, placement.X + placement.Size * 0.1f + k * barWidth, placement.Y + placement.Size * 0.85f - height,
                    barWidth, height, FlatDepth.BuildingFill, Palette.Item(inventories.ItemAt(start + k)));
            }
        }
    }
}
