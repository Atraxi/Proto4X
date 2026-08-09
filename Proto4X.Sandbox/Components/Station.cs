using Microsoft.Xna.Framework;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using System;

namespace Proto4X.Sandbox.Components
{
    /// <summary>
    /// A station is the AoE town centre: mineral drop-off point, ship factory, and last thing
    /// standing between you and losing. The build queue is two counters rather than a list, since
    /// component data lives in dense arrays and shouldn't hold references to heap collections.
    /// </summary>
    public struct Station(float rallyDistance, float repairPerSecond) : IDependsOnOtherComponents
    {
        /// <summary>
        /// Passive hull repair. Tuned to sit above a single fighter's damage output and far below a
        /// wave's, so a lone raider achieves nothing and a real assault still works. Taking a
        /// station has to be a decision, not an accumulation of pokes.
        /// </summary>
        public float RepairPerSecond = repairPerSecond;

        public UnitKind InProgress = UnitKind.None;
        public int QueuedMiners = 0;
        public int QueuedFighters = 0;

        /// <summary>How far from the station newly built ships appear, and the radius they rally around.</summary>
        public float RallyDistance = rallyDistance;

        /// <summary>
        /// Where newly built ships are sent. Null means they idle by the station and, if they're
        /// miners, start gathering on their own.
        /// </summary>
        public Vector2? RallyPoint = null;

        /// <summary>Rotating offset so consecutive ships don't all spawn on the same spot.</summary>
        public int ShipsBuilt = 0;

        // Timer is a dependency because ProductionSystem drives the build clock through the
        // engine's own TimerSystem rather than hand-rolling a countdown.
        public readonly Type[] GetComponentDependencies => [typeof(Position), typeof(Faction), typeof(Health), typeof(Timer)];
    }
}
