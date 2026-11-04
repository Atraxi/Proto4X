using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Diagnostics;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Simulation.Stores
{
    /// <summary>
    /// Every inventory on a surface, as flat stack arrays. A building owns a contiguous run of stacks
    /// identified by its start index, which lives in the building's component data.
    /// </summary>
    /// <remarks>
    /// <b>Why this is not a component.</b> Inventories are the medium every building exchanges items
    /// through: an inserter takes from one building's inventory and puts into another's, in the same
    /// tick, and has to know whether the target has room before it commits. That is a transactional
    /// read-then-write into a <em>different</em> entity's data, and the engine has no shape for it yet
    /// - its planned answer is a system-declared, frame-bound archetype handle (ENGINE_REQUESTS.md,
    /// "Cross-building exchange"). Until then this store is the stand-in: every call into it from a
    /// system is a call the declared handle will have to support, which is the useful output.
    /// <para>
    /// A stack is either <em>fixed</em> (its item is set by a recipe and survives being emptied - an
    /// assembler's ingredient slot) or <em>free</em> (takes anything, forgets its item at zero - a
    /// chest slot). A limit of 0 means "the item's stack size".
    /// </para>
    /// </remarks>
    public sealed class InventoryStore
    {
        private ItemId[] items = new ItemId[256];
        private int[] counts = new int[256];
        private int[] limits = new int[256];
        private bool[] fixedItem = new bool[256];
        private int used;

        // Allocation-time only (placement, recipe change) - never touched by a tick.
        private readonly Dictionary<int, Stack<int>> freeRunsByLength = [];

        public int StackCapacity => items.Length;

        public int Allocate(int length)
        {
            if (freeRunsByLength.TryGetValue(length, out var free) && free.Count > 0)
            {
                var reused = free.Pop();
                Clear(reused, length);
                return reused;
            }

            if (used + length > items.Length)
            {
                var newSize = Math.Max(items.Length * 2, used + length);
                Array.Resize(ref items, newSize);
                Array.Resize(ref counts, newSize);
                Array.Resize(ref limits, newSize);
                Array.Resize(ref fixedItem, newSize);
            }

            var start = used;
            used += length;
            Clear(start, length);
            return start;
        }

        public void Free(int start, int length)
        {
            Clear(start, length);
            if (!freeRunsByLength.TryGetValue(length, out var free))
            {
                free = new Stack<int>();
                freeRunsByLength[length] = free;
            }

            free.Push(start);
        }

        private void Clear(int start, int length)
        {
            for (var i = start; i < start + length; i++)
            {
                items[i] = ItemId.None;
                counts[i] = 0;
                limits[i] = 0;
                fixedItem[i] = false;
            }
        }

        public void ConfigureFixed(int stack, ItemId item, int limit)
        {
            items[stack] = item;
            limits[stack] = limit;
            fixedItem[stack] = true;
        }

        public void ConfigureFree(int stack, int limit)
        {
            items[stack] = ItemId.None;
            limits[stack] = limit;
            fixedItem[stack] = false;
        }

        public ItemId ItemAt(int stack) => items[stack];

        public int CountAt(int stack) => counts[stack];

        public int LimitAt(int stack)
        {
            var limit = limits[stack];
            return limit > 0 ? limit : Items.StackSize(items[stack]);
        }

        /// <summary>Room for <paramref name="item"/> in one stack, without changing anything.</summary>
        public bool StackAccepts(int stack, ItemId item, int amount)
        {
            if (items[stack] == item)
            {
                var limit = limits[stack] > 0 ? limits[stack] : Items.StackSize(item);
                return counts[stack] + amount <= limit;
            }

            if (items[stack] == ItemId.None && !fixedItem[stack])
            {
                var limit = limits[stack] > 0 ? limits[stack] : Items.StackSize(item);
                return amount <= limit;
            }

            return false;
        }

        /// <summary>Finds the stack in the run that would take <paramref name="item"/>, preferring one that already holds it.</summary>
        public int FindStackFor(int start, int length, ItemId item, int amount)
        {
            var empty = -1;
            for (var stack = start; stack < start + length; stack++)
            {
                if (items[stack] == item)
                {
                    if (StackAccepts(stack, item, amount))
                    {
                        return stack;
                    }
                }
                else if (empty < 0 && items[stack] == ItemId.None && !fixedItem[stack] && StackAccepts(stack, item, amount))
                {
                    empty = stack;
                }
            }

            return empty;
        }

        public bool TryInsert(int start, int length, ItemId item, int amount)
        {
            var stack = FindStackFor(start, length, item, amount);
            if (stack < 0)
            {
                return false;
            }

            items[stack] = item;
            counts[stack] += amount;
            return true;
        }

        public void AddToStack(int stack, int amount) => counts[stack] += amount;

        public void SetStack(int stack, ItemId item, int count)
        {
            items[stack] = item;
            counts[stack] = count;
        }

        /// <summary>Removes items from one stack; a free stack forgets its item when it empties.</summary>
        public void RemoveFromStack(int stack, int amount)
        {
            counts[stack] -= amount;
            if (counts[stack] <= 0)
            {
                counts[stack] = 0;
                if (!fixedItem[stack])
                {
                    items[stack] = ItemId.None;
                }
            }
        }

        public int CountOf(int start, int length, ItemId item)
        {
            var total = 0;
            for (var stack = start; stack < start + length; stack++)
            {
                if (items[stack] == item)
                {
                    total += counts[stack];
                }
            }

            return total;
        }

        public int TotalItems(int start, int length)
        {
            var total = 0;
            for (var stack = start; stack < start + length; stack++)
            {
                total += counts[stack];
            }

            return total;
        }

        public void HashInto(ref ulong hash)
        {
            for (var i = 0; i < used; i++)
            {
                hash = StateHash.Mix(hash, ((ulong)items[i] << 32) | (uint)counts[i]);
            }
        }
    }
}
