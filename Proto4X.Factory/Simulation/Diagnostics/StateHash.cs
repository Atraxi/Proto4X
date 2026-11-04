namespace Proto4X.Factory.Simulation.Diagnostics
{
    /// <summary>
    /// A cheap order-sensitive hash for determinism checks. Lockstep multiplayer and replays depend on
    /// two runs from the same commands producing bit-identical state; folding everything into one
    /// number makes that a single comparison.
    /// </summary>
    public static class StateHash
    {
        public const ulong Seed = 14695981039346656037UL;

        public static ulong Mix(ulong hash, ulong value)
        {
            hash ^= value + 0x9E3779B97F4A7C15UL + (hash << 6) + (hash >> 2);
            return hash * 1099511628211UL;
        }
    }
}
