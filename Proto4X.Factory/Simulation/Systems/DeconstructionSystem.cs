using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation.Components;
using System.Collections.Generic;

namespace Proto4X.Factory.Simulation.Systems
{
    /// <summary>
    /// Finds the entities of buildings marked for removal and queues them out of their archetypes.
    /// </summary>
    /// <remarks>
    /// Removing a specific building means finding its entity, and there is no way to address an
    /// entity except by visiting it - so this scans every building on the surface. It's gated on a
    /// pending count, so it costs nothing on the overwhelming majority of ticks where nobody has
    /// deconstructed anything. That gate is a hand-rolled version of the filtering the engine doesn't
    /// have yet (ENGINE_REQUESTS.md, "Filtering").
    /// <para>
    /// <b>Iterates each chunk backwards, deliberately.</b> <c>Archetype.ProcessDeferredUpdates</c>
    /// sorts a discarded copy of its removal set and then walks the <c>HashSet</c> itself, so removals
    /// are applied in insertion order. Swap-remove is only correct in descending index order; queueing
    /// in descending order makes insertion order the right order. That relies on <c>HashSet</c>
    /// enumerating in insertion order after a <c>Clear</c>, which is an implementation detail -
    /// logged as a hazard, worked around here rather than fixed.
    /// </para>
    /// </remarks>
    public sealed class DeconstructionSystem(Surface surface) : IUpdateSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Building>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            if (surface.PendingRemovalCount == surface.RemovedThisTick.Count)
            {
                return;
            }

            var buildings = surface.Buildings;
            foreach (var chunk in archetypeChunks)
            {
                var buildingSpan = chunk.GetSpan<Building>();
                for (var i = chunk.EntityCount - 1; i >= 0; i--)
                {
                    var slot = buildingSpan[i].Slot;
                    if (buildings.IsPendingRemoval(slot))
                    {
                        chunk.QueueEntityForRemovalByIndex(i);
                        surface.RemovedThisTick.Add(slot);
                    }
                }
            }
        }
    }
}
