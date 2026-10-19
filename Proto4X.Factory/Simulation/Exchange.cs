using Proto4X.Factory.Simulation.Belts;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Stores;

namespace Proto4X.Factory.Simulation
{
    /// <summary>
    /// The rules for moving one item into or out of a building: which stacks are its inputs and
    /// outputs, and what each kind will accept.
    /// </summary>
    /// <remarks>
    /// Every method here is a cross-building access - the caller is iterating one archetype and
    /// reaching into another building's inventory. If the engine grows a declared archetype handle,
    /// this file is the specification for what it has to do: resolve a building by stable name,
    /// test-then-write its storage, within one tick.
    /// </remarks>
    public static class Exchange
    {
        /// <summary>The usable building on a tile, or -1.</summary>
        public static int ResolveAt(Surface surface, int x, int y)
        {
            var slot = surface.Grid.OccupantAt(x, y);
            return slot >= 0 && surface.Buildings.IsUsable(slot) ? slot : -1;
        }

        public static bool CanInsert(Surface surface, int slot, ItemId item)
        {
            var buildings = surface.Buildings;
            var inventories = surface.Inventories;
            var start = buildings.InventoryStart(slot);

            switch (buildings.KindOf(slot))
            {
                case BuildingKind.Furnace:
                    return Recipes.SmeltingRecipeFor(item) != RecipeId.None
                        && inventories.StackAccepts(start, item, 1);

                case BuildingKind.Assembler:
                case BuildingKind.Generator:
                    return inventories.FindStackFor(start, buildings.InputStacks(slot), item, 1) >= 0;

                case BuildingKind.Chest:
                case BuildingKind.Receiver:
                case BuildingKind.LaunchSilo:
                    return inventories.FindStackFor(start, buildings.InventoryLength(slot), item, 1) >= 0;

                default:
                    return false;
            }
        }

        public static bool TryInsert(Surface surface, int slot, ItemId item)
        {
            if (!CanInsert(surface, slot, item))
            {
                return false;
            }

            var buildings = surface.Buildings;
            var start = buildings.InventoryStart(slot);
            var length = buildings.KindOf(slot) is BuildingKind.Furnace or BuildingKind.Assembler or BuildingKind.Generator
                ? buildings.InputStacks(slot)
                : buildings.InventoryLength(slot);
            return surface.Inventories.TryInsert(start, length, item, 1);
        }

        /// <summary>Drops onto a belt tile or into a building, whichever the slot is.</summary>
        public static bool TryDeliver(Surface surface, int slot, ItemId item, Grid.Direction approach)
        {
            if (surface.Buildings.KindOf(slot) == BuildingKind.Belt)
            {
                var lane = BeltNetwork.FarLane(surface.Buildings.DirectionOf(slot), approach);
                return surface.Belts.TryInsertOnTile(slot, lane, item);
            }

            return TryInsert(surface, slot, item);
        }

        /// <summary>Whether a building has a pickup side at all - an inserter facing away from one has nothing to do.</summary>
        public static bool HasOutputs(BuildingKind kind) =>
            kind is BuildingKind.Belt or BuildingKind.Chest or BuildingKind.Receiver or BuildingKind.Furnace or BuildingKind.Assembler;

        public static bool HasInputs(BuildingKind kind) =>
            kind is BuildingKind.Belt or BuildingKind.Chest or BuildingKind.Receiver or BuildingKind.Furnace
                or BuildingKind.Assembler or BuildingKind.Generator or BuildingKind.LaunchSilo;

        /// <summary>
        /// Takes one item from <paramref name="fromSlot"/>'s outputs that <paramref name="toSlot"/> will
        /// accept. A belt target accepts anything; whether there's room on it is the drop's problem.
        /// </summary>
        public static bool TryTakeFor(Surface surface, int fromSlot, int toSlot, out ItemId item)
        {
            var filter = new TargetFilter(surface, toSlot);
            var buildings = surface.Buildings;

            if (buildings.KindOf(fromSlot) == BuildingKind.Belt)
            {
                return surface.Belts.TryTakeFromTile(fromSlot, ref filter, out item);
            }

            var inventories = surface.Inventories;
            var start = buildings.InventoryStart(fromSlot);
            var length = buildings.InventoryLength(fromSlot);
            var first = buildings.KindOf(fromSlot) is BuildingKind.Furnace or BuildingKind.Assembler
                ? start + buildings.InputStacks(fromSlot)
                : start;

            for (var stack = first; stack < start + length; stack++)
            {
                if (inventories.CountAt(stack) == 0)
                {
                    continue;
                }

                var candidate = inventories.ItemAt(stack);
                if (filter.Accepts(candidate))
                {
                    inventories.RemoveFromStack(stack, 1);
                    item = candidate;
                    return true;
                }
            }

            item = ItemId.None;
            return false;
        }

        private readonly struct TargetFilter(Surface surface, int toSlot) : IItemFilter
        {
            public bool Accepts(ItemId item) =>
                surface.Buildings.KindOf(toSlot) == BuildingKind.Belt || CanInsert(surface, toSlot, item);
        }
    }
}
