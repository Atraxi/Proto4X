using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation.Components;
using Proto4X.Factory.Simulation.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Proto4X.Factory.Simulation.Systems
{
    /// <summary>
    /// Folds every <typeparamref name="T"/> on the surface into the surface's component hash, on the
    /// ticks a hash is requested. Registered only by the headless determinism check.
    /// </summary>
    /// <remarks>
    /// Hashes raw bytes, which is sound only because every component is unmanaged and initialised
    /// field by field. Pairs each component with its building slot, so the hash is about state, not
    /// archetype order - though order is deterministic too, given the same command stream.
    /// </remarks>
    public sealed class ComponentHashSystem<T>(Surface surface) : IUpdateSystem where T : unmanaged
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Building>(), ComponentRef.Of<T>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            if (!surface.HashRequested)
            {
                return;
            }

            var hash = surface.ComponentHash;
            foreach (var chunk in archetypeChunks)
            {
                var buildingSpan = chunk.GetSpan<Building>();
                var componentSpan = chunk.GetSpan<T>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    hash = StateHash.Mix(hash, (ulong)buildingSpan[i].Slot);
                    foreach (var b in MemoryMarshal.AsBytes(componentSpan.Slice(i, 1)))
                    {
                        hash = StateHash.Mix(hash, b);
                    }
                }
            }

            surface.ComponentHash = hash;
        }
    }
}
