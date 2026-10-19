using Microsoft.Xna.Framework;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;

namespace Proto4X.Factory
{
    /// <summary>Presentation colours. Kept out of the simulation, which has no business knowing what anything looks like.</summary>
    public static class Palette
    {
        public static readonly Color Background = new(10, 12, 18);
        public static readonly Color Ground = new(52, 46, 38);
        public static readonly Color Foundation = new(58, 64, 76);
        public static readonly Color FoundationEdge = new(96, 108, 128);
        public static readonly Color Belt = new(70, 70, 64);
        public static readonly Color BeltChevron = new(150, 132, 60);
        public static readonly Color Valid = new(90, 220, 120, 140);
        public static readonly Color Invalid = new(230, 70, 60, 140);
        public static readonly Color Progress = new(120, 220, 255);
        public static readonly Color PowerGood = new(110, 220, 110);
        public static readonly Color PowerLow = new(240, 180, 60);
        public static readonly Color PowerNone = new(220, 60, 60);

        public static Color Item(ItemId item) => item switch
        {
            ItemId.IronOre => new Color(120, 140, 170),
            ItemId.CopperOre => new Color(200, 120, 70),
            ItemId.Coal => new Color(30, 30, 30),
            ItemId.IronPlate => new Color(200, 210, 225),
            ItemId.CopperPlate => new Color(235, 150, 90),
            ItemId.Gear => new Color(160, 160, 170),
            ItemId.Wire => new Color(250, 190, 60),
            ItemId.Circuit => new Color(70, 190, 90),
            ItemId.MetallicChunk => new Color(110, 100, 130),
            ItemId.HullSegment => new Color(150, 200, 240),
            _ => Color.Magenta,
        };

        public static Color Ore(ItemId ore) => Item(ore) * 0.55f;

        public static Color Building(BuildingKind kind) => kind switch
        {
            BuildingKind.Inserter => new Color(210, 180, 60),
            BuildingKind.Drill => new Color(120, 90, 60),
            BuildingKind.Furnace => new Color(150, 70, 50),
            BuildingKind.Assembler => new Color(80, 110, 150),
            BuildingKind.Chest => new Color(140, 105, 60),
            BuildingKind.Pole => new Color(110, 80, 50),
            BuildingKind.Generator => new Color(90, 130, 90),
            BuildingKind.LaunchSilo => new Color(180, 180, 200),
            BuildingKind.Receiver => new Color(120, 160, 190),
            BuildingKind.Collector => new Color(120, 100, 160),
            BuildingKind.Belt => Belt,
            _ => Color.Magenta,
        };

        public static Color Tile(TileKind kind) => kind switch
        {
            TileKind.Ground => Ground,
            TileKind.Foundation => Foundation,
            _ => Color.Transparent,
        };
    }
}
