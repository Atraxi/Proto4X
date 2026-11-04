using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Diagnostics;
using Proto4X.Factory.Simulation.Grid;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Simulation.Stores
{
    /// <summary>
    /// The game-side registry of placed buildings, indexed by a slot assigned at placement.
    /// </summary>
    /// <remarks>
    /// This exists because the engine cannot hand back an entity id at creation (ENGINE_REQUESTS.md,
    /// "No id at creation"), and a factory needs to name a building the moment it's placed - the
    /// occupancy grid has to say who is on a tile before the entity has even been flushed into its
    /// archetype. The slot is that name. It goes into the entity's <c>Building</c> component, so the
    /// entity can find its row, and into the grid, so a tile can.
    /// <para>
    /// It is deliberately <em>not</em> a list of entities - nothing here can reach an entity's
    /// components. It holds what other buildings need to know about this one (kind, footprint,
    /// which stacks are its inputs and outputs) plus the configuration the outside world can change
    /// (recipe), which the owning system pulls in when <see cref="Surface.ConfigVersion"/> moves.
    /// </para>
    /// </remarks>
    public sealed class BuildingTable
    {
        private BuildingKind[] kinds = new BuildingKind[64];
        private int[] originX = new int[64];
        private int[] originY = new int[64];
        private Direction[] directions = new Direction[64];
        private bool[] alive = new bool[64];
        private bool[] pendingRemoval = new bool[64];
        private int[] inventoryStart = new int[64];
        private int[] inventoryLength = new int[64];
        private int[] inputStacks = new int[64];
        private RecipeId[] recipes = new RecipeId[64];
        private int[] powerNetwork = new int[64];

        private int highWater;
        private readonly Stack<int> freeSlots = new();

        public int HighWater => highWater;
        public int AliveCount { get; private set; }

        public int Allocate(BuildingKind kind, int x, int y, Direction direction)
        {
            int slot;
            if (freeSlots.Count > 0)
            {
                slot = freeSlots.Pop();
            }
            else
            {
                slot = highWater++;
                if (slot == kinds.Length)
                {
                    Grow(kinds.Length * 2);
                }
            }

            kinds[slot] = kind;
            originX[slot] = x;
            originY[slot] = y;
            directions[slot] = direction;
            alive[slot] = true;
            pendingRemoval[slot] = false;
            inventoryStart[slot] = -1;
            inventoryLength[slot] = 0;
            inputStacks[slot] = 0;
            recipes[slot] = RecipeId.None;
            powerNetwork[slot] = -1;
            AliveCount++;
            return slot;
        }

        public void Release(int slot)
        {
            alive[slot] = false;
            pendingRemoval[slot] = false;
            kinds[slot] = BuildingKind.None;
            freeSlots.Push(slot);
            AliveCount--;
        }

        private void Grow(int size)
        {
            Array.Resize(ref kinds, size);
            Array.Resize(ref originX, size);
            Array.Resize(ref originY, size);
            Array.Resize(ref directions, size);
            Array.Resize(ref alive, size);
            Array.Resize(ref pendingRemoval, size);
            Array.Resize(ref inventoryStart, size);
            Array.Resize(ref inventoryLength, size);
            Array.Resize(ref inputStacks, size);
            Array.Resize(ref recipes, size);
            Array.Resize(ref powerNetwork, size);
        }

        public BuildingKind KindOf(int slot) => kinds[slot];
        public int OriginX(int slot) => originX[slot];
        public int OriginY(int slot) => originY[slot];
        public Direction DirectionOf(int slot) => directions[slot];

        /// <summary>Alive and not being torn down - the only state other buildings may interact with.</summary>
        public bool IsUsable(int slot) => alive[slot] && !pendingRemoval[slot];
        public bool IsAlive(int slot) => alive[slot];
        public bool IsPendingRemoval(int slot) => pendingRemoval[slot];
        public void MarkPendingRemoval(int slot) => pendingRemoval[slot] = true;

        public void SetInventory(int slot, int start, int length, int inputs)
        {
            inventoryStart[slot] = start;
            inventoryLength[slot] = length;
            inputStacks[slot] = inputs;
        }

        public int InventoryStart(int slot) => inventoryStart[slot];
        public int InventoryLength(int slot) => inventoryLength[slot];
        public int InputStacks(int slot) => inputStacks[slot];

        public RecipeId RecipeOf(int slot) => recipes[slot];
        public void SetRecipe(int slot, RecipeId recipe) => recipes[slot] = recipe;

        public int PowerNetworkOf(int slot) => powerNetwork[slot];
        public void SetPowerNetwork(int slot, int network) => powerNetwork[slot] = network;

        public void HashInto(ref ulong hash)
        {
            for (var slot = 0; slot < highWater; slot++)
            {
                if (!alive[slot])
                {
                    continue;
                }

                hash = StateHash.Mix(hash, (ulong)slot);
                hash = StateHash.Mix(hash, ((ulong)kinds[slot] << 40) | ((ulong)(uint)originX[slot] << 16) | (uint)originY[slot]);
                hash = StateHash.Mix(hash, ((ulong)recipes[slot] << 32) | (uint)(powerNetwork[slot] + 1));
            }
        }
    }
}
