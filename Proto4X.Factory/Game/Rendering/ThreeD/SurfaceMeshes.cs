using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using Proto4X.Factory.Rendering.Models;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Belts;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System;

namespace Proto4X.Factory.Rendering.ThreeD
{
    /// <summary>How much of a model is worth drawing at the current camera distance.</summary>
    public enum DetailTier
    {
        /// <summary>Everything: ore rocks, belt rails and chevrons, item cubes, status cubes.</summary>
        Fine,
        /// <summary>Small parts drop out: ore as flat patches, belts as bare slabs, items as flat squares.</summary>
        Mid,
        /// <summary>Ground in 4x4 blocks, each model as its base box only, no shadows or moving parts.</summary>
        Coarse,
    }

    /// <summary>
    /// How far the camera is, for level of detail. Shared by the 3D renderers and refreshed once a
    /// frame by the style.
    /// </summary>
    public sealed class Detail
    {
        public const float FineDistance = 70f;
        public const float CoarseDistance = 140f;

        public float Distance { get; set; } = 30f;
        public DetailTier Tier => Distance < FineDistance ? DetailTier.Fine : Distance < CoarseDistance ? DetailTier.Mid : DetailTier.Coarse;
        public bool Fine => Tier == DetailTier.Fine;
        public bool Coarse => Tier == DetailTier.Coarse;
    }

    /// <summary>
    /// The parts of one surface that only change when somebody builds, removes or mines out
    /// something - terrain, ore, belts' bodies, every building model and its shadow - meshed per
    /// 32x32 grid chunk and kept as <see cref="StaticMesh"/>es.
    /// </summary>
    /// <remarks>
    /// <b>Invalidation.</b> Each mesh remembers the chunk's
    /// <see cref="SurfaceGrid.ChunkContentVersion"/> it was built from; a mismatch means stale.
    /// Belt curve markers also depend on the belt network's shapes, which are only recomputed at
    /// the next tick boundary - a mesh that read them while the network was dirty is marked
    /// provisional and rebuilt once it is clean.
    /// <para>
    /// <b>What is not here.</b> Anything that moves or changes by the tick (belt items, chevrons,
    /// inserter arms, status cubes, progress bars) and anything transient (a building awaiting
    /// removal, whose tiles are already free) is drawn immediately by the other 3D renderers.
    /// </para>
    /// <para>
    /// <b>Ownership.</b> A building belongs to the chunk holding its origin tile, so it is meshed
    /// exactly once even when its footprint crosses a chunk edge. Its geometry can therefore reach
    /// up to two tiles past the chunk, which <see cref="BodyBounds"/> allows for.
    /// </para>
    /// </remarks>
    public sealed class SurfaceMeshes(Surface surface, Vector2 whiteUv) : IDisposable
    {
        /// <summary>Height of the tallest model, rounded up, for bounds of chunks not yet built.</summary>
        private const float MaxModelHeight = 3f;

        /// <summary>A deck's hull edge hangs this far below the ground plane.</summary>
        private const float HullDepth = 0.45f;

        /// <summary>A building reaches at most this far past the chunk holding its origin (a 3x3 model's last two columns).</summary>
        private const int MaxOverhang = 2;

        private const float ShadowDarkness = 0.6f;
        private static readonly Color hull = new(44, 48, 58);

        private readonly ChunkMeshSet[] chunks = CreateChunks(surface.Grid);

        /// <summary>
        /// One shared scratch buffer every rebuild writes into before it is copied out compactly.
        /// Untextured geometry samples the atlas's white texel, so it must know where that is.
        /// </summary>
        private readonly GeometryBuffer scratch = new() { WhiteUv = whiteUv };

        /// <summary>Whether the mesh being built read belt shapes, which are only current once the belt network is clean.</summary>
        private bool readBeltShapes;

        public SurfaceGrid Grid => surface.Grid;
        public int ChunkCount => chunks.Length;

        /// <summary>Chunk meshes built since the last call, for diagnostics.</summary>
        public int TakeRebuildCount()
        {
            var count = rebuilds;
            rebuilds = 0;
            return count;
        }

        private int rebuilds;

        /// <summary>
        /// A conservative box around everything a chunk's body mesh can contain, valid before it is
        /// built: the chunk's tiles, plus overhang east and south, from the hull's underside to the
        /// tallest model.
        /// </summary>
        public BoundingBox BodyBounds(int chunk)
        {
            var (x0, y0, x1, y1) = TileRange(chunk);
            return new BoundingBox(new Vector3(x0, y0, -HullDepth), new Vector3(x1 + 1 + MaxOverhang, y1 + 1 + MaxOverhang, MaxModelHeight));
        }

        /// <summary>
        /// Where a chunk's shadows can land: its body's footprint stretched away from the sun by the
        /// longest shadow the tallest model can throw.
        /// </summary>
        public BoundingBox ShadowBounds(int chunk, Vector3 localSun)
        {
            var body = BodyBounds(chunk);
            var reach = new Vector2(-localSun.X, -localSun.Y) / localSun.Z * MaxModelHeight;
            var min = new Vector3(body.Min.X + MathF.Min(0, reach.X), body.Min.Y + MathF.Min(0, reach.Y), 0f);
            var max = new Vector3(body.Max.X + MathF.Max(0, reach.X), body.Max.Y + MathF.Max(0, reach.Y), 0.01f);
            return new BoundingBox(min, max);
        }

        /// <summary>
        /// The chunk's body mesh at <paramref name="tier"/>, built now if it never has been. A stale
        /// one is rebuilt only while <paramref name="rebuildBudget"/> lasts; otherwise the old one is
        /// returned and caught up on a later frame. <paramref name="force"/> rebuilds regardless.
        /// </summary>
        public StaticMesh Body(int chunk, DetailTier tier, ref int rebuildBudget, bool force = false)
        {
            ref var slot = ref chunks[chunk].Bodies[(int)tier];
            if (NeedsBuild(ref slot, chunk, ref rebuildBudget, force))
            {
                scratch.Clear();
                readBeltShapes = false;
                MeshBody(chunk, tier);
                Commit(ref slot, chunk);
            }

            return slot.Mesh!;
        }

        /// <summary>The chunk's shadow casters (for the fine and mid tiers), under the same rules as <see cref="Body"/>.</summary>
        public StaticMesh Shadows(int chunk, ref int rebuildBudget, bool force = false)
        {
            ref var slot = ref chunks[chunk].Shadows;
            if (NeedsBuild(ref slot, chunk, ref rebuildBudget, force))
            {
                scratch.Clear();
                readBeltShapes = false;
                MeshShadows(chunk);
                Commit(ref slot, chunk);
            }

            return slot.Mesh!;
        }

        /// <summary>Bytes held on the CPU by every built mesh (the GPU holds as much again).</summary>
        public long CpuBytes
        {
            get
            {
                long total = 0;
                foreach (var set in chunks)
                {
                    foreach (var body in set.Bodies)
                    {
                        total += body.Mesh?.CpuBytes ?? 0;
                    }
                    total += set.Shadows.Mesh?.CpuBytes ?? 0;
                }
                return total;
            }
        }

        public void Dispose()
        {
            foreach (var set in chunks)
            {
                foreach (var body in set.Bodies)
                {
                    body.Mesh?.Dispose();
                }
                set.Shadows.Mesh?.Dispose();
            }
        }

        private bool NeedsBuild(ref CachedMesh slot, int chunk, ref int rebuildBudget, bool force)
        {
            if (slot.Mesh is null || force)
            {
                return true;
            }

            var stale = slot.Version != Grid.ChunkContentVersion(chunk) || (slot.Provisional && !surface.Belts.IsDirty);
            if (!stale || rebuildBudget <= 0)
            {
                return false;
            }

            rebuildBudget--;
            return true;
        }

        private void Commit(ref CachedMesh slot, int chunk)
        {
            slot.Mesh ??= new StaticMesh();
            slot.Mesh.CopyFrom(scratch);
            slot.Version = Grid.ChunkContentVersion(chunk);
            slot.Provisional = readBeltShapes && surface.Belts.IsDirty;
            rebuilds++;
        }

        private (int X0, int Y0, int X1, int Y1) TileRange(int chunk)
        {
            var grid = Grid;
            var x0 = chunk % grid.ChunksWide * SurfaceGrid.ChunkSize;
            var y0 = chunk / grid.ChunksWide * SurfaceGrid.ChunkSize;
            return (x0, y0, Math.Min(x0 + SurfaceGrid.ChunkSize, grid.Width) - 1, Math.Min(y0 + SurfaceGrid.ChunkSize, grid.Height) - 1);
        }

        // ---------------------------------------------------------------- meshing

        private void MeshBody(int chunk, DetailTier tier)
        {
            var grid = Grid;
            var (x0, y0, x1, y1) = TileRange(chunk);

            // Terrain first, then what stands on it, in the same order as the spike's per-system
            // passes, so depth ties between coplanar faces resolve the same way.
            if (tier == DetailTier.Coarse)
            {
                MeshCoarseGround(x0, y0, x1, y1);
            }
            else
            {
                for (var y = y0; y <= y1; y++)
                {
                    for (var x = x0; x <= x1; x++)
                    {
                        MeshTile(x, y, tier);
                    }
                }
            }

            var buildings = surface.Buildings;
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var slot = OwnedBuildingAt(x, y);
                    if (slot >= 0 && buildings.KindOf(slot) == BuildingKind.Belt)
                    {
                        MeshBelt(slot, x, y, buildings.DirectionOf(slot), tier);
                    }
                }
            }

            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var slot = OwnedBuildingAt(x, y);
                    if (slot < 0 || buildings.KindOf(slot) == BuildingKind.Belt)
                    {
                        continue;
                    }

                    var model = BlockModels.For(buildings.KindOf(slot));
                    var place = BlockModels.Facing(buildings.DirectionOf(slot), model.Size) * Matrix.CreateTranslation(x, y, 0f);
                    if (tier == DetailTier.Coarse)
                    {
                        // Far out: the base box alone carries the silhouette and colour.
                        ModelMesher.EmitBox(scratch, GeometryLayer.Opaque, model.Boxes[0], place, Color.White);
                    }
                    else
                    {
                        ModelMesher.EmitModel(scratch, GeometryLayer.Opaque, model, place, Color.White);
                    }
                }
            }
        }

        private void MeshShadows(int chunk)
        {
            var grid = Grid;
            var buildings = surface.Buildings;
            var (x0, y0, x1, y1) = TileRange(chunk);
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var slot = OwnedBuildingAt(x, y);
                    if (slot < 0 || buildings.KindOf(slot) == BuildingKind.Belt)
                    {
                        continue;
                    }

                    // Opaque, in one colour per building - the ground under its origin, darkened -
                    // so overlapping shadow faces never double-darken.
                    var model = BlockModels.For(buildings.KindOf(slot));
                    var place = BlockModels.Facing(buildings.DirectionOf(slot), model.Size) * Matrix.CreateTranslation(x, y, 0f);
                    var shade = ModelMesher.Scale(Palette.Tile(grid.TileAt(x, y)), ShadowDarkness);
                    ModelMesher.EmitShadow(scratch, model, place, shade);
                }
            }
        }

        /// <summary>
        /// The slot of the building whose origin is this tile, or -1. Found through occupancy, so a
        /// building awaiting removal (its tiles already freed) is already gone.
        /// </summary>
        private int OwnedBuildingAt(int x, int y)
        {
            var slot = Grid.OccupantAt(x, y);
            var buildings = surface.Buildings;
            return slot >= 0 && buildings.OriginX(slot) == x && buildings.OriginY(slot) == y ? slot : -1;
        }

        private void MeshTile(int x, int y, DetailTier tier)
        {
            var grid = Grid;
            var tile = grid.TileAt(x, y);
            if (tile == TileKind.Void)
            {
                return;
            }

            var shade = 0.94f + 0.06f * Mesh3D.Hash01(x, y);
            var colour = Palette.Tile(tile) * shade;
            colour.A = 255;
            Mesh3D.Flat(scratch, GeometryLayer.Opaque, x, y, 1f, 1f, 0f, colour);

            if (tile == TileKind.Foundation)
            {
                MeshHullEdges(x, y);
            }

            var ore = grid.OreAt(x, y);
            if (ore == ItemId.None)
            {
                return;
            }

            var oreColour = Palette.Item(ore);
            var dark = ModelMesher.Scale(oreColour, 0.55f);
            if (tier != DetailTier.Fine)
            {
                Mesh3D.Flat(scratch, GeometryLayer.Opaque, x + 0.15f, y + 0.15f, 0.7f, 0.7f, 0.01f, dark);
                return;
            }

            for (var rock = 0; rock < 3; rock++)
            {
                var size = 0.16f + 0.14f * Mesh3D.Hash01(x, y, rock * 3);
                var centre = new Vector2(x + 0.2f + 0.6f * Mesh3D.Hash01(x, y, rock * 3 + 1), y + 0.2f + 0.6f * Mesh3D.Hash01(x, y, rock * 3 + 2));
                var height = size * (0.5f + 0.5f * Mesh3D.Hash01(x, y, rock + 17));
                Mesh3D.Box(scratch, GeometryLayer.Opaque, new Vector3(centre.X - size * 0.5f, centre.Y - size * 0.5f, 0f),
                    new Vector3(centre.X + size * 0.5f, centre.Y + size * 0.5f, height), rock == 0 ? oreColour : dark);
            }
        }

        /// <summary>A ship deck is a slab: drop a hull wall wherever a deck tile borders void.</summary>
        private void MeshHullEdges(int x, int y)
        {
            const float depth = -HullDepth;
            if (IsVoid(x, y - 1))
            {
                scratch.AddQuad(GeometryLayer.Opaque, new Vector3(x, y, 0), new Vector3(x + 1, y, 0), new Vector3(x + 1, y, depth), new Vector3(x, y, depth), -Vector3.UnitY, hull);
            }

            if (IsVoid(x, y + 1))
            {
                scratch.AddQuad(GeometryLayer.Opaque, new Vector3(x, y + 1, 0), new Vector3(x + 1, y + 1, 0), new Vector3(x + 1, y + 1, depth), new Vector3(x, y + 1, depth), Vector3.UnitY, hull);
            }

            if (IsVoid(x - 1, y))
            {
                scratch.AddQuad(GeometryLayer.Opaque, new Vector3(x, y, 0), new Vector3(x, y + 1, 0), new Vector3(x, y + 1, depth), new Vector3(x, y, depth), -Vector3.UnitX, hull);
            }

            if (IsVoid(x + 1, y))
            {
                scratch.AddQuad(GeometryLayer.Opaque, new Vector3(x + 1, y, 0), new Vector3(x + 1, y + 1, 0), new Vector3(x + 1, y + 1, depth), new Vector3(x + 1, y, depth), Vector3.UnitX, hull);
            }
        }

        private bool IsVoid(int x, int y) => Grid.TileAt(x, y) == TileKind.Void;

        /// <summary>Far out, one quad per 4x4 block, coloured by its first tile and any ore in it.</summary>
        private void MeshCoarseGround(int x0, int y0, int x1, int y1)
        {
            const int block = 4;
            var grid = Grid;
            for (var by = y0; by <= y1; by += block)
            {
                for (var bx = x0; bx <= x1; bx += block)
                {
                    var tile = grid.TileAt(bx, by);
                    if (tile == TileKind.Void)
                    {
                        continue;
                    }

                    var colour = Palette.Tile(tile);
                    var ore = grid.OreAt(bx + 1, by + 1);
                    if (ore != ItemId.None)
                    {
                        colour = Color.Lerp(colour, Palette.Item(ore), 0.35f);
                    }

                    var w = Math.Min(block, grid.Width - bx);
                    var h = Math.Min(block, grid.Height - by);
                    Mesh3D.Flat(scratch, GeometryLayer.Opaque, bx, by, w, h, 0f, colour);
                }
            }
        }

        /// <summary>A belt's body: a low slab, plus rails and a curve marker up close. The crawling chevrons are drawn live.</summary>
        private void MeshBelt(int slot, int x, int y, Direction direction, DetailTier tier)
        {
            const float top = BeltBody.Top;
            var horizontal = direction is Direction.East or Direction.West;
            Mesh3D.Box(scratch, GeometryLayer.Opaque,
                new Vector3(x + (horizontal ? 0f : 0.1f), y + (horizontal ? 0.1f : 0f), 0f),
                new Vector3(x + (horizontal ? 1f : 0.9f), y + (horizontal ? 0.9f : 1f), top), BeltBody.Bed);

            if (tier != DetailTier.Fine)
            {
                return;
            }

            if (horizontal)
            {
                Mesh3D.Box(scratch, GeometryLayer.Opaque, new Vector3(x, y + 0.06f, 0f), new Vector3(x + 1f, y + 0.14f, top + 0.05f), BeltBody.Rail);
                Mesh3D.Box(scratch, GeometryLayer.Opaque, new Vector3(x, y + 0.86f, 0f), new Vector3(x + 1f, y + 0.94f, top + 0.05f), BeltBody.Rail);
            }
            else
            {
                Mesh3D.Box(scratch, GeometryLayer.Opaque, new Vector3(x + 0.06f, y, 0f), new Vector3(x + 0.14f, y + 1f, top + 0.05f), BeltBody.Rail);
                Mesh3D.Box(scratch, GeometryLayer.Opaque, new Vector3(x + 0.86f, y, 0f), new Vector3(x + 0.94f, y + 1f, top + 0.05f), BeltBody.Rail);
            }

            readBeltShapes = true;
            if (surface.Belts.ShapeOf(slot) is var shape && shape != BeltShape.Straight)
            {
                var side = SurfaceView.DirectionVector(direction.Left());
                var feedSide = shape == BeltShape.CurveFromLeft ? side : -side;
                var marker = new Vector2(x + 0.5f, y + 0.5f) + feedSide * 0.38f;
                Mesh3D.Flat(scratch, GeometryLayer.Opaque, marker.X - 0.06f, marker.Y - 0.06f, 0.12f, 0.12f, top + 0.006f, Palette.BeltChevron);
            }
        }

        private static ChunkMeshSet[] CreateChunks(SurfaceGrid grid)
        {
            var sets = new ChunkMeshSet[grid.ChunksWide * grid.ChunksHigh];
            for (var i = 0; i < sets.Length; i++)
            {
                sets[i] = new ChunkMeshSet();
            }
            return sets;
        }

        private struct CachedMesh
        {
            public StaticMesh? Mesh;
            public int Version;
            public bool Provisional;
        }

        private sealed class ChunkMeshSet
        {
            public readonly CachedMesh[] Bodies = new CachedMesh[3];
            public CachedMesh Shadows;
        }
    }

    /// <summary>Belt body dimensions and colours, shared by the static mesh and the live chevrons.</summary>
    public static class BeltBody
    {
        public const float Top = 0.1f;
        public static readonly Color Bed = new(58, 60, 62);
        public static readonly Color Rail = new(120, 112, 90);
    }
}
