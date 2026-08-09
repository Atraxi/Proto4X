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
    /// The build order for every AI faction: grow the mining fleet to a target size, then spend
    /// everything on fighters. Deliberately about as clever as an Age of Empires "Standard" AI on a
    /// bad day - it exists to give the player something to lose to, not to be a good opponent.
    /// </summary>
    /// <remarks>
    /// Faction-agnostic: it drives whichever factions are listed in <c>Faction.AiControlled</c>, so
    /// adding a third player to the map cost nothing here. Each AI faction has its own economy, so
    /// they compete for asteroids without competing for money.
    /// </remarks>
    public class EnemyProductionAiSystem(Economy economy, FleetCensusSystem fleetCensus, StationIndexSystem stationIndex) : ISystem, IUpdateSystem
    {
        private const int TargetMinersPerStation = 5;
        private const int MaximumQueueDepth = 2;

        private static readonly ComponentRef[] requiredComponentProviders =
        [
            ComponentRef.Of<Faction>(), ComponentRef.Of<Health>(), ComponentRef.Of<Station>(),
        ];

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
                    var faction = factionSpan[i].Id;
                    if (faction == FactionId.Player || faction == FactionId.Neutral || healthSpan[i].Current <= 0f)
                    {
                        continue;
                    }

                    ref var station = ref stationSpan[i];
                    if (station.QueuedMiners + station.QueuedFighters >= MaximumQueueDepth)
                    {
                        continue;
                    }

                    // Miner target scales with station count, so an expanding AI keeps staffing the
                    // bases it founds rather than stalling at a fixed fleet size.
                    var minerTarget = TargetMinersPerStation * stationIndex.CountFor(faction);
                    if (fleetCensus.MinersFor(faction) + station.QueuedMiners < minerTarget)
                    {
                        if (economy.TrySpend(faction, ShipBlueprints.Miner.MineralCost))
                        {
                            station.QueuedMiners++;
                        }
                    }
                    else if (economy.TrySpend(faction, ShipBlueprints.Fighter.MineralCost))
                    {
                        station.QueuedFighters++;
                    }
                }
            }
        }
    }
}
