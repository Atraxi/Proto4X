namespace Proto4X.Factory.Simulation.Grid
{
    /// <summary>Screen-space compass: north is -Y. Values are quarter turns clockwise from north.</summary>
    public enum Direction : byte
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
    }

    public static class DirectionExtensions
    {
        public static int Dx(this Direction direction) => direction switch
        {
            Direction.East => 1,
            Direction.West => -1,
            _ => 0,
        };

        public static int Dy(this Direction direction) => direction switch
        {
            Direction.South => 1,
            Direction.North => -1,
            _ => 0,
        };

        public static Direction Opposite(this Direction direction) => (Direction)(((int)direction + 2) & 3);

        /// <summary>The direction to the left of someone facing <paramref name="direction"/>.</summary>
        public static Direction Left(this Direction direction) => (Direction)(((int)direction + 3) & 3);

        public static Direction Right(this Direction direction) => (Direction)(((int)direction + 1) & 3);

        public static Direction RotateClockwise(this Direction direction) => direction.Right();
    }
}
