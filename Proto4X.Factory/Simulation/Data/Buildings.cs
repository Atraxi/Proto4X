namespace Proto4X.Factory.Simulation.Data
{
    public enum BuildingKind : byte
    {
        None = 0,
        Belt,
        Inserter,
        Drill,
        Furnace,
        Assembler,
        Chest,
        Pole,
        Generator,
        /// <summary>Planet side of the interplanetary link: fills up, then launches its payload.</summary>
        LaunchSilo,
        /// <summary>Orbit side of the link: a chest that payloads land in.</summary>
        Receiver,
        /// <summary>The orbit's drill: gathers chunks out of thin air instead of from ore tiles.</summary>
        Collector,
    }

    public enum SurfaceKind : byte
    {
        Planet,
        Orbit,
    }

    /// <summary>
    /// Static per-kind tuning. Power figures are joules per tick (60 ticks per second), so
    /// 1,500 J/tick is 90 kW.
    /// </summary>
    public readonly record struct BuildingSpec(int Size, int ActiveDraw, int IdleDraw, SurfaceCondition AllowedOn);

    public static class Buildings
    {
        public const int Count = (int)BuildingKind.Collector + 1;

        /// <summary>Chest capacity in stacks.</summary>
        public const int ChestStacks = 16;
        public const int LaunchSiloStacks = 4;
        public const int LaunchPayload = 20;
        public const int LaunchTicks = 300;

        public const int InserterSwingTicks = 26;
        public const int DrillTicksPerOre = 120;
        public const int GeneratorMaxOutput = 15_000;
        public const int PoleSupplyRadius = 2;
        public const int PoleWireReach = 7;

        private static readonly BuildingSpec[] specs = BuildSpecs();

        public static BuildingSpec Spec(BuildingKind kind) => specs[(int)kind];

        public static int Size(BuildingKind kind) => specs[(int)kind].Size;

        /// <summary>Whether the kind does anything with power. Poles carry it but don't consume it.</summary>
        public static bool ConsumesPower(BuildingKind kind) => specs[(int)kind].ActiveDraw > 0;

        private static BuildingSpec[] BuildSpecs()
        {
            var table = new BuildingSpec[Count];
            table[(int)BuildingKind.None] = new(1, 0, 0, SurfaceCondition.Anywhere);
            table[(int)BuildingKind.Belt] = new(1, 0, 0, SurfaceCondition.Anywhere);
            table[(int)BuildingKind.Inserter] = new(1, 220, 7, SurfaceCondition.Anywhere);
            table[(int)BuildingKind.Drill] = new(2, 1_500, 0, SurfaceCondition.Planet);
            table[(int)BuildingKind.Furnace] = new(2, 3_000, 50, SurfaceCondition.Anywhere);
            table[(int)BuildingKind.Assembler] = new(3, 1_250, 40, SurfaceCondition.Anywhere);
            table[(int)BuildingKind.Chest] = new(1, 0, 0, SurfaceCondition.Anywhere);
            table[(int)BuildingKind.Pole] = new(1, 0, 0, SurfaceCondition.Anywhere);
            table[(int)BuildingKind.Generator] = new(2, 0, 0, SurfaceCondition.Anywhere);
            table[(int)BuildingKind.LaunchSilo] = new(3, 2_000, 30, SurfaceCondition.Planet);
            table[(int)BuildingKind.Receiver] = new(2, 0, 0, SurfaceCondition.Orbit);
            table[(int)BuildingKind.Collector] = new(2, 1_000, 0, SurfaceCondition.Orbit);
            return table;
        }
    }
}
