using Microsoft.Xna.Framework;
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

        /// <summary>Parked and assembling a station. The gather loop leaves this miner alone until it finishes.</summary>
        Constructing,
    }

    /// <summary>
    /// The gather loop, as a state machine driven entirely by the miner's own components plus
    /// nearest-position lookups. Notably there is no "which asteroid" or "which station" field -
    /// those would be entity references, and the engine has no id to reference with.
    /// </summary>
    /// <remarks>
    /// A miner is also the game's builder, in the Age of Empires villager sense: it doubles as the
    /// only way to found a new station. That's why it carries a <c>Timer</c> - construction runs on
    /// the same clock stations use for producing ships.
    /// </remarks>
    public struct Miner(float capacity, float miningRatePerSecond) : IDependsOnOtherComponents
    {
        public MinerTask Task = MinerTask.FlyingToAsteroid;
        public float Carried = 0;
        public float Capacity = capacity;
        public float MiningRatePerSecond = miningRatePerSecond;

        /// <summary>
        /// The asteroid this miner has claimed, held as a location rather than an entity reference.
        /// One miner per asteroid: <c>MinerSystem</c> gathers every claim before handing out new
        /// ones, so a rock that's taken is invisible to the next miner looking for work. Asteroids
        /// never move, so a position is a perfectly stable name for one.
        /// </summary>
        public Vector2? ClaimedAsteroid = null;

        public readonly Type[] GetComponentDependencies => [typeof(Position), typeof(Motion), typeof(MoveOrder), typeof(Faction), typeof(Timer)];
    }
}
