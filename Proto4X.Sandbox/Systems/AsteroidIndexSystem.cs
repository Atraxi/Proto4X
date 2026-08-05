using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Sandbox.Components;
using System;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Systems
{
    /// <summary>
    /// Publishes a flat snapshot of every asteroid's location once per frame, so that systems which
    /// query a different archetype (miners) can still ask "where is the nearest rock".
    /// </summary>
    /// <remarks>
    /// This is the standing workaround for the engine having no cross-archetype join: a system only
    /// ever receives chunks matching its own single AND-mask, so relating two unrelated archetypes
    /// means one of them publishing a copy for the other to read. It only works because asteroids
    /// are read-only to miners here - a snapshot gives you no way to write back. That's the direct
    /// reason asteroids are infinite in this build. See ENGINE_REQUESTS.md.
    /// </remarks>
    public class AsteroidIndexSystem : ISystem, IUpdateSystem
    {
        private readonly List<Vector2> locations = [];

        public IReadOnlyList<Vector2> Locations => locations;

        private static readonly ComponentRef[] requiredComponentProviders =
            [ComponentRef.Of<Position>(), ComponentRef.Of<Gatherable>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            locations.Clear();

            foreach (var archetypeChunk in archetypeChunks)
            {
                var positionSpan = archetypeChunk.GetSpan<Position>();
                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    locations.Add(positionSpan[i].Location);
                }
            }
        }

        /// <summary>
        /// The <paramref name="rank"/>-th nearest asteroid, 0 being the closest. Miners rotate
        /// through the first few ranks so a squad spreads over a cluster instead of all stacking on
        /// whichever single rock happens to be closest.
        /// </summary>
        public Vector2? FindNearest(Vector2 from, int rank = 0)
        {
            if (locations.Count == 0)
            {
                return null;
            }

            rank = Math.Clamp(rank, 0, locations.Count - 1);

            // Selection rather than a sort: rank is small and this runs per miner per decision.
            // Exact distance ties can exhaust a pass early, in which case the last rank that did
            // resolve is the answer rather than nothing.
            var previousBestDistanceSquared = float.MinValue;
            var resultIndex = -1;

            for (var pass = 0; pass <= rank; pass++)
            {
                var bestDistanceSquared = float.MaxValue;
                var bestIndex = -1;

                for (var i = 0; i < locations.Count; i++)
                {
                    var distanceSquared = Vector2.DistanceSquared(from, locations[i]);
                    if (distanceSquared > previousBestDistanceSquared && distanceSquared < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        bestIndex = i;
                    }
                }

                if (bestIndex < 0)
                {
                    break;
                }

                resultIndex = bestIndex;
                previousBestDistanceSquared = bestDistanceSquared;
            }

            return resultIndex >= 0 ? locations[resultIndex] : null;
        }
    }
}
