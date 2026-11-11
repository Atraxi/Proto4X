using Proto4X.Factory.Simulation.Grid;

namespace Proto4X.Factory.Simulation.Ships
{
    /// <summary>
    /// Starting designs, so the loop runs before the player has designed anything, plus the raiders'.
    /// Each is valid under <see cref="ShipStats"/> (the headless checks hold them to it).
    /// </summary>
    public static class ShipPresets
    {
        private static PartPlacement P(PartKind kind, int x, int y, Direction facing = Direction.North) => new(kind, x, y, facing);

        /// <summary>Light escort: one laser, quick.</summary>
        public static ShipDesign Picket() => new("Picket",
        [
            P(PartKind.Laser, 3, 0),
            P(PartKind.Hull, 2, 1), P(PartKind.Hull, 4, 1),
            P(PartKind.Core, 1, 2), P(PartKind.Reactor, 3, 2),
            P(PartKind.Thruster, 1, 4), P(PartKind.Gyro, 2, 4), P(PartKind.Hull, 3, 4), P(PartKind.Thruster, 4, 4),
        ]);

        /// <summary>Three lasers on a light frame: hits hard, dies fast.</summary>
        public static ShipDesign Lancer() => new("Lancer",
        [
            P(PartKind.Laser, 1, 0), P(PartKind.Laser, 3, 0), P(PartKind.Laser, 5, 0),
            P(PartKind.Core, 1, 2), P(PartKind.Reactor, 3, 2), P(PartKind.Hull, 5, 2), P(PartKind.Hull, 5, 3),
            P(PartKind.Reactor, 1, 4), P(PartKind.Gyro, 3, 4), P(PartKind.Hull, 4, 4), P(PartKind.Hull, 5, 4),
            P(PartKind.Thruster, 5, 5), P(PartKind.Thruster, 1, 6), P(PartKind.Thruster, 2, 6),
        ]);

        /// <summary>Armoured line ship: slow, soaks damage.</summary>
        public static ShipDesign Bulwark(string name = "Bulwark") => new(name,
        [
            P(PartKind.Armor, 0, 0), P(PartKind.Armor, 1, 0), P(PartKind.Armor, 2, 0), P(PartKind.Armor, 3, 0), P(PartKind.Armor, 4, 0), P(PartKind.Armor, 5, 0),
            P(PartKind.Armor, 0, 1), P(PartKind.Core, 1, 1), P(PartKind.Laser, 3, 1), P(PartKind.Reactor, 4, 1),
            P(PartKind.Armor, 0, 2),
            P(PartKind.Armor, 0, 3), P(PartKind.Reactor, 1, 3), P(PartKind.Laser, 3, 3), P(PartKind.Hull, 4, 3), P(PartKind.Hull, 5, 3),
            P(PartKind.Armor, 0, 4), P(PartKind.Hull, 4, 4), P(PartKind.Hull, 5, 4),
            P(PartKind.Thruster, 1, 5), P(PartKind.Gyro, 2, 5), P(PartKind.Hull, 3, 5), P(PartKind.Thruster, 4, 5),
        ]);

        /// <summary>Raider: the fast, light wave filler.</summary>
        public static ShipDesign Skiff() => new("Skiff",
        [
            P(PartKind.Laser, 1, 0),
            P(PartKind.Core, 0, 2), P(PartKind.Reactor, 2, 2),
            P(PartKind.Thruster, 0, 4), P(PartKind.Gyro, 1, 4), P(PartKind.Hull, 2, 4), P(PartKind.Thruster, 3, 4),
        ]);

        /// <summary>Raider: the armoured wave anchor (the Bulwark layout, in raider colours).</summary>
        public static ShipDesign Brute() => Bulwark("Brute");
    }
}
