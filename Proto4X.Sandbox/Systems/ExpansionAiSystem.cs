using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Sandbox.Components;
using Proto4X.Sandbox.Game;
using System;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Systems
{
    /// <summary>
    /// Makes AI factions expand: once one can afford a station on top of its fleet budget, it sends
    /// a miner out to the edge of its own territory and founds a base there.
    /// </summary>
    /// <remarks>
    /// Uses the same miner-builds-station mechanic the player has, rather than a separate AI-only
    /// path, so both sides are playing the same game.
    ///
    /// Site choice is the whole strategy, and the obvious version of it is wrong. Picking the
    /// asteroid <em>furthest</em> from your own stations sounds like "claim the most new ground" but
    /// actually means "walk to the opposite corner of the map", frequently into the middle of
    /// someone else's base. Expanding outward from your own frontier is the real behaviour: take the
    /// <em>nearest</em> field that isn't already yours, and refuse sites too close to a rival.
    /// </remarks>
    public class ExpansionAiSystem(
        Economy economy,
        AsteroidIndexSystem asteroidIndex,
        StationIndexSystem stationIndex) : ISystem, IUpdateSystem
    {
        /// <summary>Kept in reserve on top of the station cost, so expanding never starves the fleet entirely.</summary>
        private const int EconomicBuffer = 120;

        private const int MaximumStationsPerFaction = 6;
        private const float ArrivalRadius = 70f;

        /// <summary>Closer than this to one of our own stations and it isn't expansion, it's infill.</summary>
        private const float MinimumDistanceFromOwnStations = 700f;

        /// <summary>Any nearer to a rival's station than this and we'd be founding a base inside their territory.</summary>
        private const float MinimumDistanceFromRivalStations = 1100f;

        private readonly Vector2?[] expansionSites = new Vector2?[Enum.GetValues<FactionId>().Length];

        private static readonly ComponentRef[] requiredComponentProviders =
        [
            ComponentRef.Of<Position>(), ComponentRef.Of<MoveOrder>(), ComponentRef.Of<Faction>(),
            ComponentRef.Of<Timer>(), ComponentRef.Of<Miner>(),
        ];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var stationCost = ShipBlueprints.For(UnitKind.Station).MineralCost;
            var buildDuration = TimeSpan.FromSeconds(ShipBlueprints.For(UnitKind.Station).BuildSeconds);

            ChooseExpansionSites(stationCost);

            foreach (var archetypeChunk in archetypeChunks)
            {
                var positionSpan = archetypeChunk.GetSpan<Position>();
                var moveOrderSpan = archetypeChunk.GetSpan<MoveOrder>();
                var factionSpan = archetypeChunk.GetSpan<Faction>();
                var timerSpan = archetypeChunk.GetSpan<Timer>();
                var minerSpan = archetypeChunk.GetSpan<Miner>();

                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    var faction = factionSpan[i].Id;
                    if (faction == FactionId.Player || faction == FactionId.Neutral)
                    {
                        continue;
                    }

                    ref var miner = ref minerSpan[i];

                    // Already on the job - MinerConstructionSystem finishes what this started.
                    if (miner.Task == MinerTask.Constructing)
                    {
                        continue;
                    }

                    if (expansionSites[(int)faction] is not Vector2 site)
                    {
                        continue;
                    }

                    var location = positionSpan[i].Location;
                    if (Vector2.DistanceSquared(location, site) > ArrivalRadius * ArrivalRadius)
                    {
                        // Send exactly one miner and let it travel. Clearing the site stops the rest
                        // of the fleet setting off for the same rock this frame.
                        moveOrderSpan[i].Destination = site;
                        expansionSites[(int)faction] = null;
                        continue;
                    }

                    if (!economy.TrySpend(faction, stationCost))
                    {
                        continue;
                    }

                    miner.Task = MinerTask.Constructing;
                    miner.ClaimedAsteroid = null;
                    moveOrderSpan[i].Destination = null;
                    timerSpan[i].Restart(buildDuration);
                    expansionSites[(int)faction] = null;
                }
            }
        }

        /// <summary>
        /// Picks one site per faction, once per frame, rather than re-deriving it for every miner.
        /// </summary>
        private void ChooseExpansionSites(int stationCost)
        {
            Array.Clear(expansionSites);

            foreach (var faction in Faction.AiControlled)
            {
                if (stationIndex.CountFor(faction) >= MaximumStationsPerFaction
                    || economy.GetMinerals(faction) < stationCost + EconomicBuffer)
                {
                    continue;
                }

                expansionSites[(int)faction] = FindFrontierSite(faction);
            }
        }

        /// <summary>
        /// The closest asteroid that's outside our own territory and outside anyone else's - the
        /// frontier. Nearest-qualifying rather than furthest, so bases grow outward in a ring
        /// instead of teleporting across the map.
        /// </summary>
        private Vector2? FindFrontierSite(FactionId faction)
        {
            var minimumOwnSquared = MinimumDistanceFromOwnStations * MinimumDistanceFromOwnStations;
            var minimumRivalSquared = MinimumDistanceFromRivalStations * MinimumDistanceFromRivalStations;

            Vector2? best = null;
            var bestDistanceSquared = float.MaxValue;

            foreach (var asteroid in asteroidIndex.Locations)
            {
                if (stationIndex.FindNearest(asteroid, faction) is not Vector2 nearestOwn)
                {
                    continue;
                }

                var ownDistanceSquared = Vector2.DistanceSquared(asteroid, nearestOwn);
                if (ownDistanceSquared < minimumOwnSquared || ownDistanceSquared >= bestDistanceSquared)
                {
                    continue;
                }

                if (stationIndex.FindNearestHostile(asteroid, faction) is Vector2 nearestRival
                    && Vector2.DistanceSquared(asteroid, nearestRival) < minimumRivalSquared)
                {
                    continue;
                }

                bestDistanceSquared = ownDistanceSquared;
                best = asteroid;
            }

            return best;
        }
    }
}
