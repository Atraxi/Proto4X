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
    /// Enemy fighters loiter near home until there are enough of them, then commit to a wave against
    /// the player's station. If the wave is ground down, the survivors fall back and the enemy
    /// rebuilds. Fighters shoot whatever comes into range on the way there - CombatSystem handles
    /// engagement, this only decides where the fleet is pointed.
    /// </summary>
    public class EnemyFleetAiSystem(StationIndexSystem stationIndex, FleetCensusSystem fleetCensus) : ISystem, IUpdateSystem
    {
        private const int WaveLaunchStrength = 4;
        private const int WaveBreakStrength = 2;
        private const float LoiterRadius = 130f;

        private bool isWaveCommitted;
        private int loiterSlot;

        public bool IsWaveCommitted => isWaveCommitted;

        private static readonly ComponentRef[] requiredComponentProviders =
            [ComponentRef.Of<Position>(), ComponentRef.Of<Motion>(), ComponentRef.Of<MoveOrder>(), ComponentRef.Of<Faction>(), ComponentRef.Of<Weapon>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var enemyFighters = fleetCensus.FightersFor(FactionId.Enemy);
            if (!isWaveCommitted && enemyFighters >= WaveLaunchStrength)
            {
                isWaveCommitted = true;
            }
            else if (isWaveCommitted && enemyFighters <= WaveBreakStrength)
            {
                isWaveCommitted = false;
            }

            foreach (var archetypeChunk in archetypeChunks)
            {
                var positionSpan = archetypeChunk.GetSpan<Position>();
                var moveOrderSpan = archetypeChunk.GetSpan<MoveOrder>();
                var factionSpan = archetypeChunk.GetSpan<Faction>();
                var weaponSpan = archetypeChunk.GetSpan<Weapon>();

                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    if (factionSpan[i].Id != FactionId.Enemy || weaponSpan[i].Range <= 0f)
                    {
                        continue;
                    }

                    ref var moveOrder = ref moveOrderSpan[i];
                    if (moveOrder.Destination is not null)
                    {
                        continue;
                    }

                    var location = positionSpan[i].Location;
                    var target = isWaveCommitted
                        ? stationIndex.FindNearest(location, FactionId.Player)
                        : Loiter(stationIndex.FindNearest(location, FactionId.Enemy));

                    if (target is Vector2 destination)
                    {
                        moveOrder.Destination = destination;
                    }
                }
            }
        }

        private Vector2? Loiter(Vector2? homeStation)
        {
            if (homeStation is not Vector2 home)
            {
                return null;
            }

            var angle = loiterSlot++ * 1.1f;
            return home + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * LoiterRadius;
        }
    }
}
