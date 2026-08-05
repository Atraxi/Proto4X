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
    /// The gather loop: fly to the nearest asteroid, mine until full, fly home, deposit, repeat.
    /// </summary>
    /// <remarks>
    /// A pending <c>MoveOrder.Destination</c> means "someone is already driving this ship", which is
    /// what lets a player order override the loop for free: the miner won't pick a new destination
    /// until it has arrived where the player sent it, then resumes on its own.
    /// </remarks>
    public class MinerSystem(AsteroidIndexSystem asteroidIndex, StationIndexSystem stationIndex, Economy economy) : ISystem, IUpdateSystem
    {
        private const float AsteroidArrivalRadius = 38f;
        private const float StationArrivalRadius = 64f;

        /// <summary>How many of the closest asteroids miners spread themselves over.</summary>
        private const int AsteroidChoiceSpread = 3;

        private int nextAsteroidRank;

        private static readonly ComponentRef[] requiredComponentProviders =
            [ComponentRef.Of<Position>(), ComponentRef.Of<Motion>(), ComponentRef.Of<MoveOrder>(), ComponentRef.Of<Faction>(), ComponentRef.Of<Miner>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (var archetypeChunk in archetypeChunks)
            {
                var positionSpan = archetypeChunk.GetSpan<Position>();
                var moveOrderSpan = archetypeChunk.GetSpan<MoveOrder>();
                var factionSpan = archetypeChunk.GetSpan<Faction>();
                var minerSpan = archetypeChunk.GetSpan<Miner>();

                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    ref var miner = ref minerSpan[i];
                    ref var moveOrder = ref moveOrderSpan[i];
                    var location = positionSpan[i].Location;

                    switch (miner.Task)
                    {
                        case MinerTask.FlyingToAsteroid:
                            if (moveOrder.Destination is not null)
                            {
                                break;
                            }

                            // Arrival is checked against the closest rock, but the one a miner is
                            // sent to rotates through the nearest few, so a squad fans out over a
                            // cluster instead of stacking on one asteroid.
                            if (asteroidIndex.FindNearest(location) is not Vector2 closestAsteroid)
                            {
                                break;
                            }

                            if (Vector2.DistanceSquared(location, closestAsteroid) <= AsteroidArrivalRadius * AsteroidArrivalRadius)
                            {
                                miner.Task = MinerTask.Mining;
                                break;
                            }

                            nextAsteroidRank = (nextAsteroidRank + 1) % AsteroidChoiceSpread;
                            moveOrder.Destination = asteroidIndex.FindNearest(location, nextAsteroidRank) ?? closestAsteroid;
                            break;

                        case MinerTask.Mining:
                            // A move order issued while mining wins - the player told it to go somewhere.
                            if (moveOrder.Destination is not null)
                            {
                                miner.Task = MinerTask.FlyingToAsteroid;
                                break;
                            }

                            miner.Carried += miner.MiningRatePerSecond * elapsedSeconds;
                            if (miner.Carried >= miner.Capacity)
                            {
                                miner.Carried = miner.Capacity;
                                miner.Task = MinerTask.FlyingToStation;
                            }
                            break;

                        case MinerTask.FlyingToStation:
                            if (moveOrder.Destination is not null)
                            {
                                break;
                            }

                            if (stationIndex.FindNearest(location, factionSpan[i].Id) is not Vector2 station)
                            {
                                // Nowhere left to deliver to - the faction's last station is gone.
                                break;
                            }

                            if (Vector2.DistanceSquared(location, station) <= StationArrivalRadius * StationArrivalRadius)
                            {
                                economy.Deposit(factionSpan[i].Id, miner.Carried);
                                miner.Carried = 0f;
                                miner.Task = MinerTask.FlyingToAsteroid;
                            }
                            else
                            {
                                moveOrder.Destination = station;
                            }
                            break;
                    }
                }
            }
        }
    }
}
