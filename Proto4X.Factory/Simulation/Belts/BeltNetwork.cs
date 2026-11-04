using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Diagnostics;
using Proto4X.Factory.Simulation.Grid;
using Proto4X.Factory.Simulation.Stores;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Simulation.Belts
{
    public enum BeltShape : byte
    {
        Straight,
        /// <summary>Fed from the belt's left side and turning to its own direction.</summary>
        CurveFromLeft,
        CurveFromRight,
    }

    /// <summary>
    /// Every belt on a surface, simulated as lane segments rather than per-tile entities.
    /// </summary>
    /// <remarks>
    /// Belt <em>tiles</em> are still ECS entities (a <c>BeltTile</c> shell for rendering and
    /// deconstruction); belt <em>items</em> never are, and neither is the transport simulation. At the
    /// target scale (hundreds of thousands of belt tiles, millions of items) an item-per-entity or
    /// tile-per-tick design is off the table, and a segment is a variable-length structure that no
    /// fixed-size component can hold. This is the one subsystem where iterating entities is the wrong
    /// shape, not merely an awkward one.
    /// <para>
    /// Topology changes rebuild every segment on the surface, preserving items by (tile, lane,
    /// offset). That's O(belts) per change, fine at hand-building pace and fine for a stamped
    /// stress layout (one rebuild), but a real game would split and merge segments incrementally.
    /// </para>
    /// </remarks>
    public sealed class BeltNetwork
    {
        public const int TileLength = 256;
        public const int ItemSpacing = TileLength / 4;

        /// <summary>Units per tick: 8 * 60 / 256 = 1.875 tiles/s, one item per 8 ticks per lane, 15 items/s per belt.</summary>
        public const int Speed = 8;

        /// <summary>
        /// Long runs are cut into segments of at most this many tiles, which bounds the cost of an
        /// inserter searching a lane for an item in its tile and of mid-lane inserts and removals.
        /// </summary>
        public const int MaxSegmentTiles = 16;

        private readonly SurfaceGrid grid;
        private readonly BuildingTable buildings;

        private readonly List<LaneSegment> segmentPool = [];
        private LaneSegment[] segments = [];
        private int segmentCount;

        // Indexed by belt slot * 2 + lane.
        private int[] laneSegment = [];
        private int[] laneOffset = [];
        // Indexed by belt slot.
        private BeltShape[] shapes = [];

        public bool IsDirty { get; private set; }
        public int SegmentCount => segmentCount;
        public ReadOnlySpan<LaneSegment> Segments => new(segments, 0, segmentCount);

        /// <summary>Items deleted because the belt under them was removed. Kept for conservation checks.</summary>
        public long ItemsDestroyed { get; private set; }

        public BeltNetwork(SurfaceGrid grid, BuildingTable buildings)
        {
            this.grid = grid;
            this.buildings = buildings;
        }

        public void MarkDirty() => IsDirty = true;

        public BeltShape ShapeOf(int beltSlot) => beltSlot < shapes.Length ? shapes[beltSlot] : BeltShape.Straight;

        public void Tick()
        {
            var all = segments;
            for (var i = 0; i < segmentCount; i++)
            {
                var segment = all[i];
                segment.TryTransferFront(all);
                segment.Advance(Speed, all);
            }
        }

        /// <summary>
        /// The lane on the far side of a belt from something approaching it while travelling in
        /// <paramref name="approach"/>. Inserters put items on the far lane, which is what keeps
        /// one-sided feeding from blocking the near lane.
        /// </summary>
        public static int FarLane(Direction beltDirection, Direction approach)
        {
            var cameFrom = approach.Opposite();
            if (cameFrom == beltDirection.Left())
            {
                return 1;
            }

            if (cameFrom == beltDirection.Right())
            {
                return 0;
            }

            return 1;
        }

        public bool TryInsertOnTile(int beltSlot, int lane, ItemId item)
        {
            var index = beltSlot * 2 + lane;
            if (index >= laneSegment.Length || laneSegment[index] < 0)
            {
                return false;
            }

            return segments[laneSegment[index]].TryInsertAt(laneOffset[index] + TileLength / 2, item);
        }

        /// <summary>
        /// Takes the front-most item within this belt tile that <paramref name="filter"/> accepts,
        /// checking the right lane first. Generic over a struct filter so the acceptance test inlines
        /// rather than going through a delegate or interface call.
        /// </summary>
        public bool TryTakeFromTile<TFilter>(int beltSlot, ref TFilter filter, out ItemId item)
            where TFilter : struct, IItemFilter
        {
            for (var lane = 1; lane >= 0; lane--)
            {
                var index = beltSlot * 2 + lane;
                if (index >= laneSegment.Length || laneSegment[index] < 0)
                {
                    continue;
                }

                var segment = segments[laneSegment[index]];
                var tileStart = laneOffset[index];
                var tileEnd = tileStart + TileLength;
                var position = segment.Length;
                for (var i = 0; i < segment.Count; i++)
                {
                    position -= segment.Gaps[i];
                    if (position >= tileEnd)
                    {
                        continue;
                    }

                    if (position < tileStart)
                    {
                        break;
                    }

                    var candidate = segment.Items[i];
                    if (filter.Accepts(candidate))
                    {
                        segment.RemoveAt(i);
                        item = candidate;
                        return true;
                    }
                }
            }

            item = ItemId.None;
            return false;
        }

        public int CountItems(ItemId item)
        {
            var total = 0;
            for (var s = 0; s < segmentCount; s++)
            {
                var segment = segments[s];
                for (var i = 0; i < segment.Count; i++)
                {
                    if (segment.Items[i] == item)
                    {
                        total++;
                    }
                }
            }

            return total;
        }

        public int CountAllItems()
        {
            var total = 0;
            for (var s = 0; s < segmentCount; s++)
            {
                total += segments[s].Count;
            }

            return total;
        }

        // ---------------------------------------------------------------- rebuild

        private readonly record struct SavedItem(int BeltSlot, int Lane, int Offset, ItemId Item);

        private readonly List<SavedItem> savedItems = [];
        private readonly List<int> beltSlots = [];

        /// <summary>
        /// Recomputes every segment from the belts currently on the grid. Runs between ticks, never
        /// from inside a system.
        /// </summary>
        public void Rebuild()
        {
            IsDirty = false;
            SaveItems();

            var capacity = Math.Max(buildings.HighWater, 1);
            if (shapes.Length < capacity)
            {
                Array.Resize(ref shapes, capacity);
                laneSegment = new int[capacity * 2];
                laneOffset = new int[capacity * 2];
            }

            Array.Fill(laneSegment, -1);
            Array.Fill(laneOffset, 0);

            beltSlots.Clear();
            for (var slot = 0; slot < buildings.HighWater; slot++)
            {
                if (buildings.KindOf(slot) == BuildingKind.Belt && buildings.IsUsable(slot))
                {
                    beltSlots.Add(slot);
                }
            }

            var next = new int[capacity];
            var sideloadTarget = new int[capacity];
            var sideloadLane = new int[capacity];
            var hasPredecessor = new bool[capacity];
            Array.Fill(next, -1);
            Array.Fill(sideloadTarget, -1);

            foreach (var slot in beltSlots)
            {
                shapes[slot] = ClassifyShape(slot);
            }

            foreach (var slot in beltSlots)
            {
                var direction = buildings.DirectionOf(slot);
                var x = buildings.OriginX(slot);
                var y = buildings.OriginY(slot);
                var front = UsableBeltAt(x + direction.Dx(), y + direction.Dy());
                if (front < 0)
                {
                    continue;
                }

                var frontDirection = buildings.DirectionOf(front);
                if (frontDirection == direction.Opposite())
                {
                    continue;
                }

                if (frontDirection == direction || shapes[front] != BeltShape.Straight)
                {
                    // Feeding a straight belt from behind, or being the one input of a curve.
                    next[slot] = front;
                    hasPredecessor[front] = true;
                }
                else
                {
                    sideloadTarget[slot] = front;
                    // Which side of the front belt we're on decides which of its lanes we load.
                    sideloadLane[slot] = direction.Opposite() == frontDirection.Left() ? 0 : 1;
                }
            }

            segmentCount = 0;
            var pendingSideloads = new List<(int Segment, int TargetSlot, int TargetLane)>();
            var visited = new bool[capacity * 2];

            for (var lane = 0; lane < 2; lane++)
            {
                // Runs with a beginning first, then whatever is left over must be loops.
                foreach (var start in beltSlots)
                {
                    if (!hasPredecessor[start])
                    {
                        BuildChain(start, lane, next, visited, sideloadTarget, sideloadLane, pendingSideloads, isLoop: false);
                    }
                }

                foreach (var start in beltSlots)
                {
                    if (!visited[start * 2 + lane])
                    {
                        BuildChain(start, lane, next, visited, sideloadTarget, sideloadLane, pendingSideloads, isLoop: true);
                    }
                }
            }

            foreach (var (segment, targetSlot, targetLane) in pendingSideloads)
            {
                var index = targetSlot * 2 + targetLane;
                segments[segment].OutputSegment = laneSegment[index];
                segments[segment].OutputPosition = laneOffset[index] + TileLength / 2;
                segments[segment].OutputIsContinuation = false;
            }

            RestoreItems();
        }

        /// <summary>
        /// A belt is a curve when nothing feeds it from behind and exactly one belt feeds it from a
        /// side; otherwise side inputs load onto it.
        /// </summary>
        private BeltShape ClassifyShape(int slot)
        {
            var direction = buildings.DirectionOf(slot);
            var x = buildings.OriginX(slot);
            var y = buildings.OriginY(slot);

            var back = direction.Opposite();
            if (FeedsInto(x + back.Dx(), y + back.Dy(), direction))
            {
                return BeltShape.Straight;
            }

            var left = direction.Left();
            var right = direction.Right();
            var fromLeft = FeedsInto(x + left.Dx(), y + left.Dy(), right);
            var fromRight = FeedsInto(x + right.Dx(), y + right.Dy(), left);

            if (fromLeft && !fromRight)
            {
                return BeltShape.CurveFromLeft;
            }

            if (fromRight && !fromLeft)
            {
                return BeltShape.CurveFromRight;
            }

            return BeltShape.Straight;
        }

        private bool FeedsInto(int x, int y, Direction requiredDirection)
        {
            var neighbour = UsableBeltAt(x, y);
            return neighbour >= 0 && buildings.DirectionOf(neighbour) == requiredDirection;
        }

        private int UsableBeltAt(int x, int y)
        {
            var slot = grid.OccupantAt(x, y);
            return slot >= 0 && buildings.KindOf(slot) == BuildingKind.Belt && buildings.IsUsable(slot) ? slot : -1;
        }

        private void BuildChain(int start, int lane, int[] next, bool[] visited, int[] sideloadTarget, int[] sideloadLane,
            List<(int, int, int)> pendingSideloads, bool isLoop)
        {
            var firstSegment = segmentCount;
            var current = NewSegment((byte)lane);
            var slot = start;

            while (slot >= 0 && !visited[slot * 2 + lane])
            {
                if (current.TileCount == MaxSegmentTiles)
                {
                    var continued = NewSegment((byte)lane);
                    current.OutputSegment = segmentCount - 1;
                    current.OutputPosition = 0;
                    current.OutputIsContinuation = true;
                    current = continued;
                }

                visited[slot * 2 + lane] = true;
                laneSegment[slot * 2 + lane] = segmentCount - 1;
                laneOffset[slot * 2 + lane] = current.Length;
                current.AppendTile(slot);

                if (next[slot] < 0)
                {
                    if (sideloadTarget[slot] >= 0)
                    {
                        pendingSideloads.Add((segmentCount - 1, sideloadTarget[slot], sideloadLane[slot]));
                    }

                    return;
                }

                slot = next[slot];
            }

            // Stopped on a visited tile: either we closed a loop, or (for a run) walked into a tile
            // that a loop or another run already owns - impossible, since each tile has at most one
            // predecessor, so only the loop case remains.
            if (isLoop && slot >= 0)
            {
                current.OutputSegment = firstSegment;
                current.OutputPosition = 0;
                current.OutputIsContinuation = true;
            }
        }

        private LaneSegment NewSegment(byte lane)
        {
            if (segmentCount == segmentPool.Count)
            {
                segmentPool.Add(new LaneSegment());
            }

            var segment = segmentPool[segmentCount];
            segment.Reset(lane);
            segmentCount++;
            if (segments.Length < segmentPool.Count)
            {
                segments = segmentPool.ToArray();
            }

            return segment;
        }

        private void SaveItems()
        {
            savedItems.Clear();
            for (var s = 0; s < segmentCount; s++)
            {
                var segment = segments[s];
                var position = segment.Length;
                for (var i = 0; i < segment.Count; i++)
                {
                    position -= segment.Gaps[i];
                    var tileIndex = Math.Min(position / TileLength, segment.TileCount - 1);
                    var beltSlot = segment.TileSlots[tileIndex];
                    var offset = position - tileIndex * TileLength;

                    if (buildings.KindOf(beltSlot) == BuildingKind.Belt && buildings.IsUsable(beltSlot))
                    {
                        savedItems.Add(new SavedItem(beltSlot, segment.Lane, offset, segment.Items[i]));
                    }
                    else
                    {
                        ItemsDestroyed++;
                    }
                }
            }
        }

        private void RestoreItems()
        {
            foreach (var saved in savedItems)
            {
                var index = saved.BeltSlot * 2 + saved.Lane;
                var segment = laneSegment[index];
                if (segment < 0 || !segments[segment].TryInsertAt(laneOffset[index] + saved.Offset, saved.Item))
                {
                    ItemsDestroyed++;
                }
            }

            savedItems.Clear();
        }

        public void HashInto(ref ulong hash)
        {
            for (var s = 0; s < segmentCount; s++)
            {
                var segment = segments[s];
                hash = StateHash.Mix(hash, ((ulong)(uint)segment.TileSlots[0] << 8) | segment.Lane);
                for (var i = 0; i < segment.Count; i++)
                {
                    hash = StateHash.Mix(hash, ((ulong)segment.Items[i] << 32) | (uint)segment.Gaps[i]);
                }
            }
        }
    }

    /// <summary>Item acceptance test for <see cref="BeltNetwork.TryTakeFromTile{TFilter}"/>.</summary>
    public interface IItemFilter
    {
        bool Accepts(ItemId item);
    }
}
