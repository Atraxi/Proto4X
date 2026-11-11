namespace Proto4X.Factory.Simulation.Data
{
    /// <summary>
    /// Items are data ids, not component types. The engine's component budget is 128 types shared
    /// by every surface and every system, and a factory game has hundreds of items - so an item can
    /// never be "a component", only a value inside one.
    /// </summary>
    public enum ItemId : byte
    {
        None = 0,
        IronOre,
        CopperOre,
        Coal,
        IronPlate,
        CopperPlate,
        Gear,
        Wire,
        Circuit,
        /// <summary>Orbit-only raw material, gathered from passing debris rather than mined from tiles.</summary>
        MetallicChunk,
        /// <summary>Orbit-only product: its recipe carries a surface condition.</summary>
        HullSegment,

        // Ship components (plans/03): made on the ground, launched to the shipyard.
        Steel,
        Frame,
        ArmorPlate,
        Engine,
        PowerCell,
        Processor,
        Emitter,
    }

    public static class Items
    {
        public const int Count = (int)ItemId.Emitter + 1;

        /// <summary>Joules of electrical energy a generator extracts from one unit of fuel.</summary>
        public const long CoalFuelValue = 4_000_000;

        public static int StackSize(ItemId item) => item switch
        {
            ItemId.IronOre or ItemId.CopperOre or ItemId.Coal or ItemId.MetallicChunk => 50,
            ItemId.Wire => 200,
            ItemId.HullSegment => 20,
            ItemId.Frame or ItemId.ArmorPlate or ItemId.Engine or ItemId.PowerCell or ItemId.Processor or ItemId.Emitter => 50,
            ItemId.None => 0,
            _ => 100,
        };

        public static bool IsFuel(ItemId item) => item == ItemId.Coal;

        public static long FuelValue(ItemId item) => item == ItemId.Coal ? CoalFuelValue : 0;
    }
}
