using Proto4X.Factory.Simulation.Data;
using System;

namespace Proto4X.Factory.Simulation.Grid
{
    public enum TileKind : byte
    {
        /// <summary>Nothing to build on - the default outside an orbital platform.</summary>
        Void = 0,
        Ground,
        /// <summary>Orbital platform deck.</summary>
        Foundation,
    }

    /// <summary>
    /// One surface's tile map: terrain, ore, which building occupies each tile, and a version stamp
    /// per 32x32 chunk.
    /// </summary>
    /// <remarks>
    /// Bounded and flat on purpose. An unbounded map wants a chunk dictionary, but that puts a hash
    /// lookup on every tile query, and nothing in the vertical slice needs more than a fixed area.
    /// <para>
    /// The occupancy array is what makes tile coordinates a stable name for a building. Buildings
    /// never move, so "the building at (x, y)" means the same thing until somebody removes it - the
    /// same trick the sandbox uses for asteroids, applied to everything here.
    /// </para>
    /// <para>
    /// Chunk versions are how cached cross-building references stay correct without the engine
    /// telling anyone about neighbours. Every structural change bumps the chunks around it; an
    /// inserter or drill compares its cached version against its own chunk once per tick, a single
    /// load, and re-resolves its targets only on a mismatch.
    /// </para>
    /// <para>
    /// Content versions are the same idea for anyone mirroring what the grid <em>shows</em> - a
    /// renderer's cached chunk meshes. They move whenever a tile, an ore patch's presence, or
    /// occupancy changes, including the neighbours one tile out (a belt's curve and a deck's hull
    /// edge depend on them). They are separate from chunk versions so that a viewer's needs never
    /// make the simulation re-resolve anything; nothing in the simulation reads them.
    /// </para>
    /// </remarks>
    public sealed class SurfaceGrid
    {
        public const int ChunkShift = 5;
        public const int ChunkSize = 1 << ChunkShift;

        public int Width { get; }
        public int Height { get; }
        public int ChunksWide { get; }
        public int ChunksHigh { get; }

        private readonly TileKind[] tiles;
        private readonly ItemId[] ore;
        private readonly int[] oreAmount;
        private readonly int[] occupant;
        private readonly int[] chunkVersions;
        private readonly int[] contentVersions;

        public SurfaceGrid(int width, int height)
        {
            Width = width;
            Height = height;
            ChunksWide = (width + ChunkSize - 1) >> ChunkShift;
            ChunksHigh = (height + ChunkSize - 1) >> ChunkShift;

            tiles = new TileKind[width * height];
            ore = new ItemId[width * height];
            oreAmount = new int[width * height];
            occupant = new int[width * height];
            Array.Fill(occupant, -1);
            chunkVersions = new int[ChunksWide * ChunksHigh];
            contentVersions = new int[ChunksWide * ChunksHigh];
        }

        public bool InBounds(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;

        public TileKind TileAt(int x, int y) => InBounds(x, y) ? tiles[y * Width + x] : TileKind.Void;

        public void SetTile(int x, int y, TileKind kind)
        {
            tiles[y * Width + x] = kind;
            BumpContentAround(x, y, 1);
        }

        public ItemId OreAt(int x, int y) => InBounds(x, y) && oreAmount[y * Width + x] > 0 ? ore[y * Width + x] : ItemId.None;

        public int OreAmountAt(int x, int y) => InBounds(x, y) ? oreAmount[y * Width + x] : 0;

        public void SetOre(int x, int y, ItemId item, int amount)
        {
            ore[y * Width + x] = item;
            oreAmount[y * Width + x] = amount;
            BumpContentAround(x, y, 1);
        }

        /// <summary>Takes one unit of ore from the tile. Returns the item, or <see cref="ItemId.None"/> if it was empty.</summary>
        public ItemId MineOne(int x, int y)
        {
            var index = y * Width + x;
            if (oreAmount[index] <= 0)
            {
                return ItemId.None;
            }

            // Only the last unit changes what the tile shows; every other unit is invisible.
            if (--oreAmount[index] == 0)
            {
                BumpContentAround(x, y, 1);
            }

            return ore[index];
        }

        /// <summary>The building slot occupying the tile, or -1.</summary>
        public int OccupantAt(int x, int y) => InBounds(x, y) ? occupant[y * Width + x] : -1;

        public void SetOccupant(int x, int y, int size, int slot)
        {
            for (var dy = 0; dy < size; dy++)
            {
                for (var dx = 0; dx < size; dx++)
                {
                    occupant[(y + dy) * Width + x + dx] = slot;
                }
            }

            BumpContentAround(x, y, size);
        }

        public int ChunkIndexOf(int x, int y) => (y >> ChunkShift) * ChunksWide + (x >> ChunkShift);

        public int ChunkVersion(int chunkIndex) => chunkVersions[chunkIndex];

        /// <summary>Moves whenever anything visible in the chunk may have changed. See the remarks on the class.</summary>
        public int ChunkContentVersion(int chunkIndex) => contentVersions[chunkIndex];

        /// <summary>Bumps the content version of every chunk within one tile of the footprint.</summary>
        private void BumpContentAround(int x, int y, int size)
        {
            var minChunkX = Math.Max(0, (x - 1) >> ChunkShift);
            var maxChunkX = Math.Min(ChunksWide - 1, (x + size) >> ChunkShift);
            var minChunkY = Math.Max(0, (y - 1) >> ChunkShift);
            var maxChunkY = Math.Min(ChunksHigh - 1, (y + size) >> ChunkShift);

            for (var chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
            {
                for (var chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
                {
                    contentVersions[chunkY * ChunksWide + chunkX]++;
                }
            }
        }

        /// <summary>
        /// Invalidates every cached reference that could point into the footprint. A building reaches
        /// at most a couple of tiles past its own edge, so bumping the 3x3 chunk block around the
        /// footprint's corners covers every possible holder.
        /// </summary>
        public void BumpChunksAround(int x, int y, int size)
        {
            var minChunkX = Math.Max(0, ((x - 2) >> ChunkShift));
            var maxChunkX = Math.Min(ChunksWide - 1, (x + size + 2) >> ChunkShift);
            var minChunkY = Math.Max(0, ((y - 2) >> ChunkShift));
            var maxChunkY = Math.Min(ChunksHigh - 1, (y + size + 2) >> ChunkShift);

            for (var chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
            {
                for (var chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
                {
                    chunkVersions[chunkY * ChunksWide + chunkX]++;
                }
            }
        }

        /// <summary>Folds the mutable parts of the grid into a determinism hash.</summary>
        public void HashInto(ref ulong hash)
        {
            for (var i = 0; i < oreAmount.Length; i++)
            {
                hash = Diagnostics.StateHash.Mix(hash, (ulong)oreAmount[i]);
                hash = Diagnostics.StateHash.Mix(hash, (ulong)(occupant[i] + 1));
            }
        }
    }
}
