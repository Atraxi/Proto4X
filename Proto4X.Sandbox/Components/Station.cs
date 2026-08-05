using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using System;

namespace Proto4X.Sandbox.Components
{
    public enum ShipType
    {
        None = 0,
        Miner,
        Fighter,
    }

    /// <summary>
    /// A station is the AoE town centre: mineral drop-off point, ship factory, and last thing
    /// standing between you and losing. The build queue is two counters rather than a list, since
    /// component data lives in dense arrays and shouldn't hold references to heap collections.
    /// </summary>
    public struct Station(float rallyDistance) : IDependsOnOtherComponents
    {
        public ShipType InProgress = ShipType.None;
        public int QueuedMiners = 0;
        public int QueuedFighters = 0;

        /// <summary>How far from the station newly built ships appear, and the radius they rally around.</summary>
        public float RallyDistance = rallyDistance;

        /// <summary>Rotating offset so consecutive ships don't all spawn on the same spot.</summary>
        public int ShipsBuilt = 0;

        // Timer is a dependency because ProductionSystem drives the build clock through the
        // engine's own TimerSystem rather than hand-rolling a countdown.
        public readonly Type[] GetComponentDependencies => [typeof(Position), typeof(Faction), typeof(Health), typeof(Timer)];
    }
}
