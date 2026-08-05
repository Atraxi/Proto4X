using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Sandbox.Components;
using Proto4X.Sandbox.Game;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Systems
{
    /// <summary>
    /// The enemy's build order: grow the mining fleet to a target size, then spend everything on
    /// fighters. Deliberately about as clever as an Age of Empires "Standard" AI on a bad day - it
    /// exists to give the player something to lose to, not to be a good opponent.
    /// </summary>
    public class EnemyProductionAiSystem(Economy economy, FleetCensusSystem fleetCensus) : ISystem, IUpdateSystem
    {
        private const int TargetMiners = 6;
        private const int MaximumQueueDepth = 2;

        private static readonly ComponentRef[] requiredComponentProviders =
            [ComponentRef.Of<Faction>(), ComponentRef.Of<Health>(), ComponentRef.Of<Station>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            foreach (var archetypeChunk in archetypeChunks)
            {
                var factionSpan = archetypeChunk.GetSpan<Faction>();
                var healthSpan = archetypeChunk.GetSpan<Health>();
                var stationSpan = archetypeChunk.GetSpan<Station>();

                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    if (factionSpan[i].Id != FactionId.Enemy || healthSpan[i].Current <= 0f)
                    {
                        continue;
                    }

                    ref var station = ref stationSpan[i];
                    if (station.QueuedMiners + station.QueuedFighters >= MaximumQueueDepth)
                    {
                        continue;
                    }

                    var wantsMoreMiners = fleetCensus.MinersFor(FactionId.Enemy) + station.QueuedMiners < TargetMiners;
                    if (wantsMoreMiners)
                    {
                        if (economy.TrySpend(FactionId.Enemy, ShipBlueprints.Miner.MineralCost))
                        {
                            station.QueuedMiners++;
                        }
                    }
                    else if (economy.TrySpend(FactionId.Enemy, ShipBlueprints.Fighter.MineralCost))
                    {
                        station.QueuedFighters++;
                    }
                }
            }
        }
    }
}
