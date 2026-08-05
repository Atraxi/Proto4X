using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Input;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Sandbox.Components;
using Proto4X.Sandbox.Game;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Systems
{
    /// <summary>
    /// The player's build hotkeys: 1 queues a miner, 2 queues a fighter, at their station.
    /// Minerals are charged when the order is placed, as in Age of Empires.
    /// </summary>
    public class BuildCommandSystem(Economy economy) : ISystem, IUpdateSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders =
            [ComponentRef.Of<Faction>(), ComponentRef.Of<Health>(), ComponentRef.Of<Station>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var keyboard = InputManager.Instance.KeyboardInfo;
            var wantsMiner = keyboard.WasKeyJustPressed(Keys.D1) || keyboard.WasKeyJustPressed(Keys.NumPad1);
            var wantsFighter = keyboard.WasKeyJustPressed(Keys.D2) || keyboard.WasKeyJustPressed(Keys.NumPad2);

            if (!wantsMiner && !wantsFighter)
            {
                return;
            }

            foreach (var archetypeChunk in archetypeChunks)
            {
                var factionSpan = archetypeChunk.GetSpan<Faction>();
                var healthSpan = archetypeChunk.GetSpan<Health>();
                var stationSpan = archetypeChunk.GetSpan<Station>();

                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    if (factionSpan[i].Id != FactionId.Player || healthSpan[i].Current <= 0f)
                    {
                        continue;
                    }

                    ref var station = ref stationSpan[i];

                    if (wantsMiner && economy.TrySpend(FactionId.Player, ShipBlueprints.Miner.MineralCost))
                    {
                        station.QueuedMiners++;
                    }

                    if (wantsFighter && economy.TrySpend(FactionId.Player, ShipBlueprints.Fighter.MineralCost))
                    {
                        station.QueuedFighters++;
                    }

                    // One station only - the moment there are several, this would charge once per station.
                    return;
                }
            }
        }
    }
}
