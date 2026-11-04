using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Diagnostics;
using Proto4X.Factory.Simulation.Grid;
using Proto4X.Factory.Simulation.Stores;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Simulation.Power
{
    /// <summary>
    /// Electric networks on one surface: which buildings each pole network reaches, and each
    /// network's satisfaction this tick.
    /// </summary>
    /// <remarks>
    /// Satisfaction is integer fixed-point, <see cref="Full"/> meaning 100%, so the whole sim stays
    /// deterministic without floats. Indices are offset by one: index 0 is "not connected", whose
    /// satisfaction is permanently zero, which lets consumers index blindly with no branch.
    /// <para>
    /// The solve lags a tick on purpose. Consumers record what they wanted this tick, generators
    /// what they could offer, and the ratio is applied next tick. That turns a two-way dependency
    /// (consumers need the ratio, the ratio needs consumers' demand) into a fixed system order.
    /// </para>
    /// </remarks>
    public sealed class PowerGrid
    {
        public const int Full = 1024;

        private readonly SurfaceGrid grid;
        private readonly BuildingTable buildings;

        private int[] satisfaction = [0];
        private int[] load = [0];
        private long[] demand = [0];
        private long[] capacity = [0];
        private int[] tileNetwork = [];

        public int NetworkCount { get; private set; }

        /// <summary>Bumped on every rebuild; buildings cache their network index against it.</summary>
        public int Version { get; private set; }

        public bool IsDirty { get; private set; }

        public PowerGrid(SurfaceGrid grid, BuildingTable buildings)
        {
            this.grid = grid;
            this.buildings = buildings;
        }

        public void MarkDirty() => IsDirty = true;

        public int[] Satisfaction => satisfaction;
        public int[] Load => load;
        public long[] Demand => demand;
        public long[] Capacity => capacity;

        /// <summary>Network index (offset by one, 0 = none) for a building slot.</summary>
        public int IndexOf(int slot) => buildings.PowerNetworkOf(slot) + 1;

        public int SatisfactionOfIndex(int index) => satisfaction[index];

        public void Solve()
        {
            for (var n = 1; n <= NetworkCount; n++)
            {
                var wanted = demand[n];
                var offered = capacity[n];

                satisfaction[n] = wanted == 0
                    ? (offered > 0 ? Full : 0)
                    : (int)Math.Min(Full, offered * Full / wanted);
                load[n] = offered == 0 ? 0 : (int)Math.Min(Full, wanted * Full / offered);

                demand[n] = 0;
                capacity[n] = 0;
            }

            satisfaction[0] = 0;
            load[0] = 0;
            demand[0] = 0;
            capacity[0] = 0;
        }

        /// <summary>Recomputes pole networks and every building's membership. Between ticks only.</summary>
        public void Rebuild()
        {
            IsDirty = false;
            Version++;

            var poles = new List<int>();
            for (var slot = 0; slot < buildings.HighWater; slot++)
            {
                if (buildings.KindOf(slot) == BuildingKind.Pole && buildings.IsUsable(slot))
                {
                    poles.Add(slot);
                }
            }

            // Union-find over poles in wire reach. Bucketed by chunk, since reach < chunk size.
            var parent = new int[poles.Count];
            for (var i = 0; i < parent.Length; i++)
            {
                parent[i] = i;
            }

            var buckets = new Dictionary<int, List<int>>();
            for (var i = 0; i < poles.Count; i++)
            {
                var chunk = grid.ChunkIndexOf(buildings.OriginX(poles[i]), buildings.OriginY(poles[i]));
                if (!buckets.TryGetValue(chunk, out var list))
                {
                    list = [];
                    buckets[chunk] = list;
                }

                list.Add(i);
            }

            const int reachSquared = Buildings.PoleWireReach * Buildings.PoleWireReach;
            for (var i = 0; i < poles.Count; i++)
            {
                var x = buildings.OriginX(poles[i]);
                var y = buildings.OriginY(poles[i]);
                var chunkX = x >> SurfaceGrid.ChunkShift;
                var chunkY = y >> SurfaceGrid.ChunkShift;
                for (var cy = chunkY - 1; cy <= chunkY + 1; cy++)
                {
                    for (var cx = chunkX - 1; cx <= chunkX + 1; cx++)
                    {
                        if (cx < 0 || cy < 0 || cx >= grid.ChunksWide || cy >= grid.ChunksHigh
                            || !buckets.TryGetValue(cy * grid.ChunksWide + cx, out var list))
                        {
                            continue;
                        }

                        foreach (var j in list)
                        {
                            if (j <= i)
                            {
                                continue;
                            }

                            var dx = buildings.OriginX(poles[j]) - x;
                            var dy = buildings.OriginY(poles[j]) - y;
                            if (dx * dx + dy * dy <= reachSquared)
                            {
                                Union(parent, i, j);
                            }
                        }
                    }
                }
            }

            // Number networks by their lowest pole slot, so ids are a function of layout only.
            var networkOfRoot = new Dictionary<int, int>();
            var poleNetwork = new int[poles.Count];
            for (var i = 0; i < poles.Count; i++)
            {
                var root = Find(parent, i);
                if (!networkOfRoot.TryGetValue(root, out var network))
                {
                    network = networkOfRoot.Count;
                    networkOfRoot[root] = network;
                }

                poleNetwork[i] = network;
            }

            NetworkCount = networkOfRoot.Count;
            satisfaction = new int[NetworkCount + 1];
            load = new int[NetworkCount + 1];
            demand = new long[NetworkCount + 1];
            capacity = new long[NetworkCount + 1];

            if (tileNetwork.Length != grid.Width * grid.Height)
            {
                tileNetwork = new int[grid.Width * grid.Height];
            }

            Array.Fill(tileNetwork, -1);
            for (var i = 0; i < poles.Count; i++)
            {
                var px = buildings.OriginX(poles[i]);
                var py = buildings.OriginY(poles[i]);
                for (var y = py - Buildings.PoleSupplyRadius; y <= py + Buildings.PoleSupplyRadius; y++)
                {
                    for (var x = px - Buildings.PoleSupplyRadius; x <= px + Buildings.PoleSupplyRadius; x++)
                    {
                        if (!grid.InBounds(x, y))
                        {
                            continue;
                        }

                        ref var cell = ref tileNetwork[y * grid.Width + x];
                        if (cell < 0 || poleNetwork[i] < cell)
                        {
                            cell = poleNetwork[i];
                        }
                    }
                }
            }

            for (var slot = 0; slot < buildings.HighWater; slot++)
            {
                if (!buildings.IsAlive(slot))
                {
                    continue;
                }

                var size = Buildings.Size(buildings.KindOf(slot));
                var ox = buildings.OriginX(slot);
                var oy = buildings.OriginY(slot);
                var network = -1;
                for (var y = oy; y < oy + size; y++)
                {
                    for (var x = ox; x < ox + size; x++)
                    {
                        var cell = tileNetwork[y * grid.Width + x];
                        if (cell >= 0 && (network < 0 || cell < network))
                        {
                            network = cell;
                        }
                    }
                }

                buildings.SetPowerNetwork(slot, network);
            }
        }

        /// <summary>Which network covers a tile, or -1. For the UI.</summary>
        public int NetworkAtTile(int x, int y) => grid.InBounds(x, y) && tileNetwork.Length > 0 ? tileNetwork[y * grid.Width + x] : -1;

        private static int Find(int[] parent, int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }

            return i;
        }

        private static void Union(int[] parent, int a, int b)
        {
            var rootA = Find(parent, a);
            var rootB = Find(parent, b);
            if (rootA != rootB)
            {
                // Keep the smaller index as root, purely for reproducibility.
                if (rootA < rootB)
                {
                    parent[rootB] = rootA;
                }
                else
                {
                    parent[rootA] = rootB;
                }
            }
        }

        public void HashInto(ref ulong hash)
        {
            for (var n = 0; n <= NetworkCount; n++)
            {
                hash = StateHash.Mix(hash, ((ulong)(uint)satisfaction[n] << 32) | (uint)load[n]);
            }
        }
    }
}
