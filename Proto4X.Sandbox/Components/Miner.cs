using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using System;

namespace Proto4X.Sandbox.Components
{
    public enum MinerTask
    {
        FlyingToAsteroid,
        Mining,
        FlyingToStation,
    }

    /// <summary>
    /// The gather loop, as a three-state machine driven entirely by the miner's own components plus
    /// nearest-position lookups. Notably there is no "which asteroid" or "which station" field -
    /// those would be entity references, and the engine has no id to reference with.
    /// </summary>
    public struct Miner(float capacity, float miningRatePerSecond) : IDependsOnOtherComponents
    {
        public MinerTask Task = MinerTask.FlyingToAsteroid;
        public float Carried = 0;
        public float Capacity = capacity;
        public float MiningRatePerSecond = miningRatePerSecond;

        public readonly Type[] GetComponentDependencies => [typeof(Position), typeof(Motion), typeof(MoveOrder), typeof(Faction)];
    }
}
