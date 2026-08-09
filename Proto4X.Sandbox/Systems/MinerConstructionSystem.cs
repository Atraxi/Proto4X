using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Input;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Sandbox.Components;
using Proto4X.Sandbox.Game;
using System;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Systems
{
    /// <summary>
    /// Miners double as builders: with one selected, the build hotkey founds a new station where
    /// that miner is standing. Expansion is therefore a movement decision - fly a miner to the
    /// asteroid field you want to work, then build there.
    /// </summary>
    /// <remarks>
    /// Construction runs on the miner's own <c>Timer</c>, the same clock stations use to produce
    /// ships, so it gets the same radial progress ring for free.
    ///
    /// Charging per miner is deliberately self-limiting: select ten miners with 200 minerals and
    /// exactly one starts building, because the other nine fail <c>TrySpend</c>. No queueing rules
    /// or "who goes first" logic needed.
    /// </remarks>
    public class MinerConstructionSystem(Economy economy, SpawnQueue spawnQueue) : ISystem, IUpdateSystem
    {
        private const float StepAsideDistance = 90f;

        private static readonly ComponentRef[] requiredComponentProviders =
        [
            ComponentRef.Of<Position>(), ComponentRef.Of<MoveOrder>(), ComponentRef.Of<Faction>(),
            ComponentRef.Of<Timer>(), ComponentRef.Of<Selectable>(), ComponentRef.Of<Miner>(),
        ];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var keyboard = InputManager.Instance.KeyboardInfo;
            var wantsToBuild = false;
            foreach (var command in ShipBlueprints.CommandsFor(UnitKind.Miner))
            {
                if (command.Produces == UnitKind.Station && keyboard.WasKeyJustPressed(command.Hotkey))
                {
                    wantsToBuild = true;
                }
            }

            var cost = ShipBlueprints.For(UnitKind.Station).MineralCost;
            var buildDuration = TimeSpan.FromSeconds(ShipBlueprints.For(UnitKind.Station).BuildSeconds);

            foreach (var archetypeChunk in archetypeChunks)
            {
                var positionSpan = archetypeChunk.GetSpan<Position>();
                var moveOrderSpan = archetypeChunk.GetSpan<MoveOrder>();
                var factionSpan = archetypeChunk.GetSpan<Faction>();
                var timerSpan = archetypeChunk.GetSpan<Timer>();
                var selectableSpan = archetypeChunk.GetSpan<Selectable>();
                var minerSpan = archetypeChunk.GetSpan<Miner>();

                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    ref var miner = ref minerSpan[i];
                    ref var timer = ref timerSpan[i];
                    ref var moveOrder = ref moveOrderSpan[i];
                    var faction = factionSpan[i].Id;

                    if (miner.Task == MinerTask.Constructing)
                    {
                        if (timer.HasElapsed)
                        {
                            CompleteStation(ref miner, ref moveOrder, positionSpan[i].Location, faction);
                        }
                        continue;
                    }

                    if (!wantsToBuild || !selectableSpan[i].IsSelected || faction != FactionId.Player)
                    {
                        continue;
                    }

                    // continue, not return: later entities in this sweep still need their
                    // completion checks, and TrySpend deducts nothing when it fails.
                    if (!economy.TrySpend(FactionId.Player, cost))
                    {
                        continue;
                    }

                    miner.Task = MinerTask.Constructing;
                    moveOrder.Destination = null;
                    timer.Restart(buildDuration);
                }
            }
        }

        private void CompleteStation(ref Miner miner, ref MoveOrder moveOrder, Vector2 location, FactionId faction)
        {
            spawnQueue.Enqueue(new SpawnRequest(UnitKind.Station, faction, location));

            // Step clear of the station that just appeared underneath, then resume the gather loop.
            miner.Task = MinerTask.FlyingToAsteroid;
            moveOrder.Destination = location + new Vector2(StepAsideDistance, 0f);
        }
    }
}
