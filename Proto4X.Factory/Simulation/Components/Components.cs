using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;

// One file for every component on purpose: they're small, and seeing the whole data model on one
// screen is the point. Each is a plain blittable struct; nothing here holds a managed reference.
namespace Proto4X.Factory.Simulation.Components
{
    /// <summary>Every placed building has one. <see cref="Slot"/> is its row in the surface's BuildingTable.</summary>
    public struct Building(int slot, BuildingKind kind)
    {
        public int Slot = slot;
        public BuildingKind Kind = kind;
    }

    /// <summary>Footprint on the tile grid. Buildings never move, so this is written once.</summary>
    public struct Placement(int x, int y, int size, Direction direction)
    {
        public int X = x;
        public int Y = y;
        public int Size = size;
        public Direction Direction = direction;
    }

    /// <summary>
    /// Tag for belt tiles. The transport simulation lives in BeltNetwork; this only gives belts their
    /// own archetype so renderers can iterate them without testing a kind per entity.
    /// </summary>
    public struct BeltTile
    {
    }

    public enum InserterPhase : byte
    {
        /// <summary>Hand empty, over the pickup side, looking for something the drop side wants.</summary>
        WaitingForPickup,
        SwingingToDrop,
        /// <summary>Over the drop side, holding an item the target hasn't accepted yet.</summary>
        WaitingToDrop,
        SwingingBack,
    }

    public struct Inserter(int pickupX, int pickupY, int dropX, int dropY, int chunk)
    {
        /// <summary>The grid chunk this inserter sits in, whose version guards the cached slots below.</summary>
        public int Chunk = chunk;

        public InserterPhase Phase = InserterPhase.WaitingForPickup;
        public ItemId Held = ItemId.None;

        /// <summary>Swing progress in power-scaled units; a swing completes at SwingTicks * PowerGrid.Full.</summary>
        public int Progress;

        public int PickupX = pickupX;
        public int PickupY = pickupY;
        public int DropX = dropX;
        public int DropY = dropY;

        /// <summary>
        /// Cached building slots for each side, valid while <see cref="WiringVersion"/> matches the
        /// chunk version. This is the entity-to-entity reference, held as component data - the
        /// reference has to live somewhere, and keeping it in the iterated data keeps it on the
        /// iteration path rather than in system-side lists.
        /// </summary>
        public int PickupSlot = -1;
        public int DropSlot = -1;
        public int WiringVersion = -1;

        public int PowerIndex;
        public int PowerVersion = -1;
    }

    public struct Drill(int outputX, int outputY, ItemId produces, int chunk)
    {
        public int Chunk = chunk;

        public int Progress;

        /// <summary>Mined but not yet placed, because the output was full.</summary>
        public ItemId Held = ItemId.None;

        /// <summary><see cref="ItemId.None"/> to mine the ore under the footprint; otherwise a collector's fixed product.</summary>
        public ItemId Produces = produces;
        public bool Depleted;

        public int OutputX = outputX;
        public int OutputY = outputY;
        public int OutputSlot = -1;
        public int WiringVersion = -1;

        public int PowerIndex;
        public int PowerVersion = -1;
    }

    /// <summary>Furnaces and assemblers. Ingredient and product stacks live in the InventoryStore.</summary>
    public struct Crafter(int inventory, RecipeId recipe, bool picksRecipeFromInput)
    {
        public int Inventory = inventory;
        public RecipeId Recipe = recipe;
        public bool PicksRecipeFromInput = picksRecipeFromInput;
        public bool IsCrafting;
        public int Progress;

        /// <summary>
        /// The recipe can be changed from outside (the player), which no system may write into this
        /// component directly. The change lands in the BuildingTable and bumps the surface's config
        /// version; the crafter system notices the mismatch and pulls the new recipe in.
        /// </summary>
        public int ConfigVersion;

        public int PowerIndex;
        public int PowerVersion = -1;
    }

    public struct Generator(int inventory)
    {
        public int Inventory = inventory;
        public long FuelEnergy;
        public int LastOffered;
        public int PowerIndex;
        public int PowerVersion = -1;
    }

    /// <summary>The planet end of the interplanetary link.</summary>
    public struct Launcher(int inventory, int stacks)
    {
        public int Inventory = inventory;
        public int Stacks = stacks;
        public bool IsLaunching;
        public int Progress;
        public int PowerIndex;
        public int PowerVersion = -1;
    }
}
