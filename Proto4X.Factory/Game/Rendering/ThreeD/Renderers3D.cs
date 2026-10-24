using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Input;
using Proto4X.Factory.Rendering.Models;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Belts;
using Proto4X.Factory.Simulation.Components;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Rendering.ThreeD
{
    /// <summary>
    /// Submits a surface's cached chunk meshes (<see cref="SurfaceMeshes"/>): each chunk the camera
    /// can see, at the current detail tier, plus its shadows flattened along the sun.
    /// </summary>
    /// <remarks>
    /// Per frame this costs one box-versus-frustum test per chunk and one draw call per visible
    /// mesh, however many buildings the chunks hold. Rebuilding a stale chunk is the expensive
    /// part, so at most <see cref="RebuildBudget"/> happen per frame; a chunk that has never been
    /// built is built regardless, since the alternative is a hole.
    /// </remarks>
    public sealed class StaticSurface3DRenderer(SurfaceMeshes meshes, Detail detail) : IGeometryDrawSystem
    {
        /// <summary>Shadows sit this far above the ground so they never fight it for depth.</summary>
        private const float ShadowLift = 0.004f;

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        /// <summary>False rebuilds every visible chunk every frame: the immediate behaviour, for comparison and debugging.</summary>
        public bool Retained { get; set; } = true;

        /// <summary>False submits every chunk, visible or not. For proving the culling is exact.</summary>
        public bool CullChunks { get; set; } = true;

        public int RebuildBudget { get; set; } = 4;

        public SurfaceMeshes Meshes => meshes;

        public void Draw(GeometryBatch batch, in GeometryDrawContext context, List<ArchetypeChunk> archetypeChunks)
        {
            var tier = detail.Tier;
            var budget = RebuildBudget;
            var force = !Retained;
            var frustum = context.LocalFrustum;

            // The sun is fixed in the sky (camera world space); the shadow matrix has to aim along it
            // from this surface's local space, so it turns as the surface does.
            var localSun = batch.Lighting.SunInLocal(context.WorldTransform);
            var shadowTransform = SceneLighting.PlanarShadow(localSun, ShadowLift);

            for (var chunk = 0; chunk < meshes.ChunkCount; chunk++)
            {
                if (!CullChunks || frustum.Intersects(meshes.BodyBounds(chunk)))
                {
                    batch.Draw(meshes.Body(chunk, tier, ref budget, force), Matrix.Identity);
                }

                if (tier != DetailTier.Coarse && (!CullChunks || frustum.Intersects(meshes.ShadowBounds(chunk, localSun))))
                {
                    batch.Draw(meshes.Shadows(chunk, ref budget, force), shadowTransform);
                }
            }
        }
    }

    /// <summary>Chevrons crawling along each belt, up close. Unlit, so they read as markings rather than parts.</summary>
    public sealed class BeltChevron3DRenderer(Surface surface, Detail detail) : IGeometryDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Placement>(), ComponentRef.Of<BeltTile>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, in GeometryDrawContext context, List<ArchetypeChunk> archetypeChunks)
        {
            if (!detail.Fine)
            {
                return;
            }

            var buffer = batch.Immediate;
            var visible = SurfaceView.VisibleTiles(context.LocalCullingBounds, surface.Grid, padding: 2);
            var crawl = surface.CurrentTick * BeltNetwork.Speed % BeltNetwork.TileLength / (float)BeltNetwork.TileLength;
            const float z = BeltBody.Top + 0.005f;
            foreach (var chunk in archetypeChunks)
            {
                var placementSpan = chunk.GetSpan<Placement>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var placement = ref placementSpan[i];
                    if (!SurfaceView.IsVisible(visible, placement.X, placement.Y, 1))
                    {
                        continue;
                    }

                    var centre = new Vector2(placement.X + 0.5f, placement.Y + 0.5f);
                    var forward = SurfaceView.DirectionVector(placement.Direction);
                    var side = SurfaceView.DirectionVector(placement.Direction.Left());
                    for (var c = 0; c < 2; c++)
                    {
                        var along = (c * 0.5f + crawl * 0.5f) % 1f - 0.5f;
                        var tip = centre + forward * (along + 0.12f);
                        Mesh3D.FlatLine(buffer, GeometryLayer.Emissive, centre + forward * along + side * 0.28f, tip, 0.06f, z, Palette.BeltChevron);
                        Mesh3D.FlatLine(buffer, GeometryLayer.Emissive, centre + forward * along - side * 0.28f, tip, 0.06f, z, Palette.BeltChevron);
                    }
                }
            }
        }
    }

    /// <summary>Belt items as little cubes riding the slab.</summary>
    public sealed class BeltItem3DRenderer(Surface surface, Detail detail) : IGeometryDrawSystem
    {
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(GeometryBatch batch, in GeometryDrawContext context, List<ArchetypeChunk> archetypeChunks)
        {
            var buffer = batch.Immediate;
            var visible = SurfaceView.VisibleTiles(context.LocalCullingBounds, surface.Grid, padding: 2);
            var coarse = !detail.Fine;
            foreach (var item in BeltItems.Visible(surface, visible))
            {
                if (coarse)
                {
                    Mesh3D.Flat(buffer, GeometryLayer.Opaque, item.Position.X - 0.11f, item.Position.Y - 0.11f, 0.22f, 0.22f, 0.12f, Palette.Item(item.Item));
                }
                else
                {
                    Mesh3D.Cube(buffer, GeometryLayer.Opaque, item.Position, 0.2f, 0.1f, Palette.Item(item.Item));
                }
            }
        }
    }

    /// <summary>
    /// What changes on a building between rebuilds of its chunk: the recipe floating and turning
    /// over an assembler, the top item on a chest, and a building awaiting removal, drawn
    /// see-through (its tiles are already free, so its chunk mesh has dropped it).
    /// </summary>
    public sealed class BuildingStatus3DRenderer(Surface surface, Detail detail) : IGeometryDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Building>(), ComponentRef.Of<Placement>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, in GeometryDrawContext context, List<ArchetypeChunk> archetypeChunks)
        {
            // Status cubes only show up close; removals can show at any tier but the coarsest.
            var showStatus = detail.Fine;
            var showRemovals = !detail.Coarse && surface.PendingRemovalCount > 0;
            if (!showStatus && !showRemovals)
            {
                return;
            }

            var buffer = batch.Immediate;
            var visible = SurfaceView.VisibleTiles(context.LocalCullingBounds, surface.Grid, padding: 4);
            var buildings = surface.Buildings;
            var inventories = surface.Inventories;
            var spin = surface.CurrentTick * 0.03f;

            foreach (var chunk in archetypeChunks)
            {
                var buildingSpan = chunk.GetSpan<Building>();
                var placementSpan = chunk.GetSpan<Placement>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var building = ref buildingSpan[i];
                    if (building.Kind == BuildingKind.Belt)
                    {
                        continue;
                    }

                    ref var placement = ref placementSpan[i];
                    if (!SurfaceView.IsVisible(visible, placement.X, placement.Y, placement.Size))
                    {
                        continue;
                    }

                    var model = BlockModels.For(building.Kind);
                    if (buildings.IsPendingRemoval(building.Slot))
                    {
                        if (showRemovals)
                        {
                            var place = BlockModels.Facing(placement.Direction, placement.Size) * Matrix.CreateTranslation(placement.X, placement.Y, 0f);
                            ModelMesher.EmitModel(buffer, GeometryLayer.Translucent, model, place, Color.White * 0.4f);
                        }

                        continue;
                    }

                    if (!showStatus)
                    {
                        continue;
                    }

                    var centre = new Vector2(placement.X + placement.Size * 0.5f, placement.Y + placement.Size * 0.5f);
                    if (building.Kind == BuildingKind.Assembler)
                    {
                        var result = Recipes.Get(buildings.RecipeOf(building.Slot)).Result;
                        var turn = Matrix.CreateTranslation(-0.5f, -0.5f, 0f) * Matrix.CreateRotationZ(spin) * Matrix.CreateScale(0.4f) *
                            Matrix.CreateTranslation(centre.X, centre.Y, 1.55f);
                        ModelMesher.EmitBox(buffer, GeometryLayer.Opaque, new ModelBox(Vector3.Zero, Vector3.One, Palette.Item(result), Emissive: true), turn, Color.White);
                    }

                    var start = buildings.InventoryStart(building.Slot);
                    if (start >= 0 && building.Kind is BuildingKind.Chest or BuildingKind.Receiver)
                    {
                        var item = TopItem(inventories, start, buildings.InventoryLength(building.Slot));
                        if (item != ItemId.None)
                        {
                            Mesh3D.Cube(buffer, GeometryLayer.Opaque, centre, 0.26f, model.Height, Palette.Item(item));
                        }
                    }
                }
            }
        }

        private static ItemId TopItem(Simulation.Stores.InventoryStore inventories, int start, int length)
        {
            var best = ItemId.None;
            var bestCount = 0;
            for (var k = 0; k < length; k++)
            {
                var count = inventories.CountAt(start + k);
                if (count > bestCount)
                {
                    bestCount = count;
                    best = inventories.ItemAt(start + k);
                }
            }

            return best;
        }
    }

    /// <summary>Progress bars lying just south of crafters, readable from any angle above.</summary>
    public sealed class Progress3DRenderer(Surface surface, Detail detail) : IGeometryDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Placement>(), ComponentRef.Of<Crafter>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, in GeometryDrawContext context, List<ArchetypeChunk> archetypeChunks)
        {
            if (detail.Coarse)
            {
                return;
            }

            var visible = SurfaceView.VisibleTiles(context.LocalCullingBounds, surface.Grid, padding: 4);
            foreach (var chunk in archetypeChunks)
            {
                var placementSpan = chunk.GetSpan<Placement>();
                var crafterSpan = chunk.GetSpan<Crafter>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var placement = ref placementSpan[i];
                    ref var crafter = ref crafterSpan[i];
                    if (!crafter.IsCrafting || !SurfaceView.IsVisible(visible, placement.X, placement.Y, placement.Size))
                    {
                        continue;
                    }

                    DrawBar(batch.Immediate, placement, MachineState.CraftFraction(crafter), Palette.Progress);
                }
            }
        }

        internal static void DrawBar(GeometryBuffer buffer, in Placement placement, float fraction, Color colour)
        {
            // Just south of the footprint, at knee height, so it never hides inside a tall model.
            var x = placement.X + 0.1f;
            var y = placement.Y + placement.Size + 0.02f;
            var width = placement.Size - 0.2f;
            Mesh3D.Box(buffer, GeometryLayer.Opaque, new Vector3(x, y, 0f), new Vector3(x + width, y + 0.14f, 0.06f), new Color(20, 22, 26));
            Mesh3D.Box(buffer, GeometryLayer.Opaque, new Vector3(x, y + 0.02f, 0.06f), new Vector3(x + width * fraction, y + 0.12f, 0.08f), colour, emissive: true);
        }
    }

    public sealed class Launcher3DRenderer(Detail detail) : IGeometryDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Placement>(), ComponentRef.Of<Launcher>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, in GeometryDrawContext context, List<ArchetypeChunk> archetypeChunks)
        {
            if (detail.Coarse)
            {
                return;
            }

            foreach (var chunk in archetypeChunks)
            {
                var placementSpan = chunk.GetSpan<Placement>();
                var launcherSpan = chunk.GetSpan<Launcher>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var launcher = ref launcherSpan[i];
                    if (launcher.IsLaunching)
                    {
                        Progress3DRenderer.DrawBar(batch.Immediate, placementSpan[i], MachineState.LaunchFraction(launcher), Color.White);
                    }
                }
            }
        }
    }

    /// <summary>Inserter arms: two beams from the pivot to the hand, with the held item as a cube. The base is in the chunk mesh.</summary>
    public sealed class Inserter3DRenderer(Surface surface, Detail detail) : IGeometryDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Placement>(), ComponentRef.Of<Inserter>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, in GeometryDrawContext context, List<ArchetypeChunk> archetypeChunks)
        {
            if (detail.Coarse)
            {
                return;
            }

            var buffer = batch.Immediate;
            var visible = SurfaceView.VisibleTiles(context.LocalCullingBounds, surface.Grid, padding: 2);
            var arm = Palette.Building(BuildingKind.Inserter);
            foreach (var chunk in archetypeChunks)
            {
                var placementSpan = chunk.GetSpan<Placement>();
                var inserterSpan = chunk.GetSpan<Inserter>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var placement = ref placementSpan[i];
                    if (!SurfaceView.IsVisible(visible, placement.X, placement.Y, 1))
                    {
                        continue;
                    }

                    ref var inserter = ref inserterSpan[i];
                    var pivot = new Vector3(placement.X + 0.5f, placement.Y + 0.5f, 0.42f);
                    var hand2 = MachineState.Hand(placement, inserter);
                    var hand = new Vector3(hand2, 0.3f);
                    var elbow = (pivot + hand) * 0.5f + new Vector3(0f, 0f, 0.32f);
                    Mesh3D.Beam(buffer, GeometryLayer.Opaque, pivot, elbow, 0.08f, arm);
                    Mesh3D.Beam(buffer, GeometryLayer.Opaque, elbow, hand, 0.07f, arm);
                    if (inserter.Held != ItemId.None)
                    {
                        Mesh3D.Cube(buffer, GeometryLayer.Opaque, hand2, 0.18f, 0.12f, Palette.Item(inserter.Held));
                    }
                }
            }
        }
    }

    /// <summary>The build preview: the model itself, tinted green or red and see-through.</summary>
    public sealed class Ghost3DRenderer(Surface surface, BuildController controller) : IGeometryDrawSystem
    {
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(GeometryBatch batch, in GeometryDrawContext context, List<ArchetypeChunk> archetypeChunks)
        {
            if (controller.Surface != surface)
            {
                return;
            }

            var buffer = batch.Immediate;
            var (x, y) = controller.Hovered;
            if (controller.Tool == BuildingKind.None)
            {
                Mesh3D.Flat(buffer, GeometryLayer.Translucent, x, y, 1f, 1f, 0.02f, Color.White * 0.25f);
                return;
            }

            var valid = surface.CanPlace(controller.Tool, x, y, controller.Recipe) == PlacementResult.Placed;
            var tint = valid ? Palette.Valid : Palette.Invalid;
            var size = Buildings.Size(controller.Tool);
            Mesh3D.Flat(buffer, GeometryLayer.Translucent, x, y, size, size, 0.02f, tint);
            var model = BlockModels.For(controller.Tool);
            if (model.Boxes.Length > 0)
            {
                var place = BlockModels.Facing(controller.Direction, size) * Matrix.CreateTranslation(x, y, 0f);
                ModelMesher.EmitModel(buffer, GeometryLayer.Translucent, model, place, tint);
            }
            else
            {
                // Belts have no model: an arrow on the ground.
                var centre = new Vector2(x + 0.5f, y + 0.5f);
                Mesh3D.FlatLine(buffer, GeometryLayer.Translucent, centre, centre + SurfaceView.DirectionVector(controller.Direction) * 0.5f, 0.1f, 0.03f, Color.White * 0.8f);
            }
        }
    }
}
