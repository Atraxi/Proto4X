using Proto4X.Factory.Simulation.Data;
using System;

namespace Proto4X.Factory.Simulation.Belts
{
    /// <summary>
    /// One lane of a run of belt tiles, with its items stored gap-encoded from the downstream end.
    /// </summary>
    /// <remarks>
    /// Positions are fixed-point: one tile is <see cref="BeltNetwork.TileLength"/> units, position 0
    /// is the upstream edge and <see cref="Length"/> the downstream edge. <c>Gaps[0]</c> is the
    /// distance from the front item to the end; <c>Gaps[i]</c> the distance from item i-1 back to
    /// item i.
    /// <para>
    /// The point of the encoding: decreasing one gap moves that item <em>and everything behind it</em>.
    /// A flowing lane costs one subtraction per tick no matter how many items it carries, and a
    /// compressed, blocked lane costs a compare. The per-tick work scales with segments, not items -
    /// which is the property that makes item counts in the millions survivable at all.
    /// </para>
    /// </remarks>
    public sealed class LaneSegment
    {
        public int Length { get; private set; }
        public byte Lane { get; private set; }

        /// <summary>Belt slots this lane runs through, upstream first.</summary>
        public int[] TileSlots { get; private set; } = new int[BeltNetwork.MaxSegmentTiles];
        public int TileCount { get; private set; }

        public ItemId[] Items { get; private set; } = new ItemId[BeltNetwork.MaxSegmentTiles * 4 + 2];
        public int[] Gaps { get; private set; } = new int[BeltNetwork.MaxSegmentTiles * 4 + 2];
        public int Count { get; private set; }

        /// <summary>Sum of all gaps, i.e. the distance from the back-most item to the downstream end.</summary>
        public int GapSum { get; private set; }

        /// <summary>
        /// First index >= 1 whose gap is wider than the item spacing. Everything between the front and
        /// here is nose-to-tail, so movement can skip straight past it.
        /// </summary>
        private int compressedUntil = 1;

        /// <summary>Segment this lane hands its front item to, or -1.</summary>
        public int OutputSegment { get; internal set; } = -1;
        public int OutputPosition { get; internal set; }

        /// <summary>
        /// True when the output is a straight continuation into another segment's upstream end, so
        /// the front item should hold back from that segment's back-most item rather than the edge.
        /// </summary>
        public bool OutputIsContinuation { get; internal set; }

        internal void Reset(byte lane)
        {
            Lane = lane;
            TileCount = 0;
            Length = 0;
            Count = 0;
            GapSum = 0;
            compressedUntil = 1;
            OutputSegment = -1;
            OutputPosition = 0;
            OutputIsContinuation = false;
        }

        internal void AppendTile(int beltSlot)
        {
            TileSlots[TileCount++] = beltSlot;
            Length += BeltNetwork.TileLength;
        }

        /// <summary>Position of the back-most item, or <see cref="int.MaxValue"/> if the lane is empty.</summary>
        public int BackPosition => Count == 0 ? int.MaxValue : Length - GapSum;

        public bool HasRoomAt(int position, out int insertIndex)
        {
            insertIndex = 0;
            if (position < 0 || position > Length)
            {
                return false;
            }

            var itemPosition = Length;
            var ahead = int.MaxValue;
            for (var i = 0; i < Count; i++)
            {
                itemPosition -= Gaps[i];
                if (itemPosition < position)
                {
                    insertIndex = i;
                    return position - itemPosition >= BeltNetwork.ItemSpacing
                        && (ahead == int.MaxValue || ahead - position >= BeltNetwork.ItemSpacing);
                }

                ahead = itemPosition;
            }

            insertIndex = Count;
            return ahead == int.MaxValue || ahead - position >= BeltNetwork.ItemSpacing;
        }

        public bool TryInsertAt(int position, ItemId item)
        {
            if (!HasRoomAt(position, out var index))
            {
                return false;
            }

            InsertAt(index, position, item);
            return true;
        }

        private void InsertAt(int index, int position, ItemId item)
        {
            // Position of the item ahead of the insertion point (or the lane's end).
            var aheadPosition = Length;
            for (var i = 0; i < index; i++)
            {
                aheadPosition -= Gaps[i];
            }

            Array.Copy(Items, index, Items, index + 1, Count - index);
            Array.Copy(Gaps, index, Gaps, index + 1, Count - index);
            Items[index] = item;
            Gaps[index] = aheadPosition - position;

            if (index < Count)
            {
                // The item now behind the new one measured its gap from the item ahead; split it.
                Gaps[index + 1] -= Gaps[index];
            }
            else
            {
                GapSum = Length - position;
            }

            Count++;
            compressedUntil = Math.Max(1, Math.Min(compressedUntil, index));
        }

        public void RemoveAt(int index)
        {
            if (index < Count - 1)
            {
                Gaps[index + 1] += Gaps[index];
            }
            else
            {
                GapSum -= Gaps[index];
            }

            Array.Copy(Items, index + 1, Items, index, Count - index - 1);
            Array.Copy(Gaps, index + 1, Gaps, index, Count - index - 1);
            Count--;
            compressedUntil = Math.Max(1, Math.Min(compressedUntil, index));
        }

        /// <summary>
        /// How far the front item may advance this tick. For a continuation it stops a full spacing
        /// short of the next segment's back-most item, so spacing holds across the seam.
        /// </summary>
        private int FrontSlack(LaneSegment[] segments)
        {
            var slack = Gaps[0];
            if (OutputIsContinuation && OutputSegment >= 0)
            {
                var deficit = BeltNetwork.ItemSpacing - segments[OutputSegment].BackPosition;
                if (deficit > 0)
                {
                    slack -= deficit;
                }
            }

            return Math.Max(0, slack);
        }

        /// <summary>Hands the front item downstream if it's at the end and there's room. Returns whether one moved.</summary>
        internal bool TryTransferFront(LaneSegment[] segments)
        {
            if (Count == 0 || Gaps[0] != 0 || OutputSegment < 0)
            {
                return false;
            }

            var target = segments[OutputSegment];
            if (!target.HasRoomAt(OutputPosition, out var insertIndex))
            {
                return false;
            }

            var item = Items[0];
            RemoveAt(0);

            if (ReferenceEquals(target, this))
            {
                // A loop closed by a single segment: removing the front shifted every index, so the
                // insertion point has to be found again.
                if (!target.HasRoomAt(OutputPosition, out insertIndex))
                {
                    throw new InvalidOperationException("Belt loop lost room for its own front item.");
                }
            }

            target.InsertAt(insertIndex, OutputPosition, item);
            return true;
        }

        internal void Advance(int speed, LaneSegment[] segments)
        {
            if (Count == 0)
            {
                return;
            }

            var remaining = speed;
            var frontSlack = FrontSlack(segments);
            if (frontSlack > 0)
            {
                var move = Math.Min(remaining, frontSlack);
                Gaps[0] -= move;
                GapSum -= move;
                remaining -= move;
            }

            // Whatever the front couldn't use lets the items behind close up on it, in order. The
            // compressed run directly behind the front is skipped via the cache.
            var i = compressedUntil;
            while (i < Count)
            {
                var slack = Gaps[i] - BeltNetwork.ItemSpacing;
                if (slack > 0)
                {
                    if (remaining == 0)
                    {
                        break;
                    }

                    var move = Math.Min(remaining, slack);
                    Gaps[i] -= move;
                    GapSum -= move;
                    remaining -= move;
                    if (Gaps[i] > BeltNetwork.ItemSpacing)
                    {
                        break;
                    }
                }

                i++;
            }

            compressedUntil = Math.Max(1, i);
        }

        /// <summary>Position of the item at <paramref name="index"/>; O(index), for rendering and rebuilds only.</summary>
        public int PositionOf(int index)
        {
            var position = Length;
            for (var i = 0; i <= index; i++)
            {
                position -= Gaps[i];
            }

            return position;
        }
    }
}
