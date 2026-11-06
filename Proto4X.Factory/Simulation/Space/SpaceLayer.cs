using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.World;
using Proto4X.Factory.Simulation.Diagnostics;
using Proto4X.Factory.Simulation.Ships;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Simulation.Space
{
    /// <summary>One body as of the end of the last tick, for targeting, rendering, picking and the HUD.</summary>
    public readonly record struct ShipView(int Serial, Faction Faction, BodyRole Role, int DesignKey, Vector2 Position, float Rotation, Vector2 Velocity,
        float Hp, float MaxHp, bool Firing, Vector2 BeamTarget, float Range, HelmOrder Order, Vector2 OrderTarget);

    public enum Outcome : byte
    {
        Undecided,
        Won,
        Lost,
    }

    /// <summary>
    /// Space around the orbital station (plans/03, pillar 5): its own GameWorld and systems, ticked
    /// after the surfaces. Ships, the station's body and the raider bastion live here; the station's
    /// factory is still the orbit surface, which sits in this space at its frame.
    /// </summary>
    /// <remarks>
    /// Floats, like <see cref="SurfaceFrame"/>: deterministic on one machine, not across them
    /// (fixed-point is listed as out of scope). Everything that crosses into this layer - launched
    /// ships, player orders - arrives between ticks.
    /// </remarks>
    public sealed class SpaceLayer
    {
        public const float Width = 480f;
        public const float Height = 320f;
        public const float PlayerAggroRange = 30f;
        public const float RaiderAggroRange = 40f;
        public const int FirstWaveTick = 6 * 60 * Universe.TicksPerSecond;
        public const int WaveIntervalTicks = 3 * 60 * Universe.TicksPerSecond;

        private readonly List<ShipView> snapshot = [];
        private readonly List<ShipDesign> designs = [];
        private int nextSerial = 1;
        private bool stationSeen;
        private bool bastionSeen;

        public SpaceLayer()
        {
            World = new GameWorld(0, 0, (int)Width, (int)Height);
        }

        public GameWorld World { get; }

        /// <summary>Every design a body was spawned from, by key. Spawns capture the design, so later edits don't change ships in flight.</summary>
        public IReadOnlyList<ShipDesign> Designs => designs;

        public IReadOnlyList<ShipView> Snapshot => snapshot;
        internal List<ShipView> WritableSnapshot => snapshot;

        /// <summary>Damage dealt this tick, by target serial; written by gunnery, pulled by damage.</summary>
        internal Dictionary<int, float> Damage { get; } = [];

        /// <summary>Bodies destroyed this tick: for explosions in the renderer and for the outcome.</summary>
        public List<(int Serial, Faction Faction, BodyRole Role, Vector2 Position)> Deaths { get; } = [];

        internal List<(HashSet<int> Serials, Vector2 Target)> PendingOrders { get; } = [];

        public Outcome Outcome { get; private set; }
        public bool Peaceful { get; set; }
        public int WavesSpawned { get; private set; }
        public long NextWaveTick { get; private set; } = FirstWaveTick;
        public Vector2 StationPosition { get; private set; }
        public Vector2 BastionPosition { get; private set; }

        internal void RegisterSystems(SystemScheduler scheduler)
        {
            scheduler.Add(World, new ShipOrderSystem(this));
            scheduler.Add(World, new FlightSystem(this));
            scheduler.Add(World, new GunnerySystem(this));
            scheduler.Add(World, new DamageSystem(this));
            scheduler.Add(World, new SpaceSnapshotSystem(this));
        }

        /// <summary>Queues a body. Between ticks only; it appears in the snapshot after the next tick.</summary>
        public int Spawn(ShipDesign design, Faction faction, Vector2 position, float rotation, Vector2 post)
        {
            var key = designs.IndexOf(design);
            if (key < 0)
            {
                key = designs.Count;
                designs.Add(design);
            }

            var stats = design.Stats;
            return Add(faction, BodyRole.Ship, key, position, rotation, post, stats.Hp, stats.Dps, stats.Range, new Drive
            {
                Forward = stats.Acceleration[(int)Grid.Direction.North],
                Reverse = stats.Acceleration[(int)Grid.Direction.South],
                Right = stats.Acceleration[(int)Grid.Direction.East],
                Left = stats.Acceleration[(int)Grid.Direction.West],
                Angular = stats.AngularAcceleration,
                MaxTurnRate = ShipStats.MaxTurnRate,
            });
        }

        public int AddStation(Vector2 position, float hp, float dps, float range)
        {
            StationPosition = position;
            return Add(Faction.Player, BodyRole.Station, -1, position, 0f, position, hp, dps, range, default);
        }

        public int AddBastion(Vector2 position, float hp, float dps, float range)
        {
            BastionPosition = position;
            return Add(Faction.Raider, BodyRole.Bastion, -1, position, 0f, position, hp, dps, range, default);
        }

        private int Add(Faction faction, BodyRole role, int designKey, Vector2 position, float rotation, Vector2 post, float hp, float dps, float range, Drive drive)
        {
            var serial = nextSerial++;
            World.AddEntity(EntityBuilder.CreateEntity()
                .With(new SpaceShip { Serial = serial, Faction = faction, Role = role, DesignKey = designKey, Hp = hp, MaxHp = hp })
                .With(new Body { Position = position, Rotation = rotation })
                .With(drive)
                .With(new Helm { Order = HelmOrder.Idle, Post = post, Target = post, Engaging = -1 })
                .With(new Gunnery { Dps = dps, Range = range, Target = -1 }));
            return serial;
        }

        /// <summary>Queues a move order for the next tick.</summary>
        public void Order(IEnumerable<int> serials, Vector2 target) => PendingOrders.Add(([.. serials], target));

        /// <summary>Nearest hostile to <paramref name="from"/> among those within <paramref name="radius"/> of <paramref name="anchor"/>, from the last snapshot.</summary>
        public bool TryFindNearestHostile(Faction faction, Vector2 anchor, Vector2 from, float radius, out ShipView found)
        {
            found = default;
            var best = float.MaxValue;
            var radiusSquared = radius * radius;
            foreach (var view in snapshot)
            {
                if (view.Faction == faction || Vector2.DistanceSquared(view.Position, anchor) > radiusSquared)
                {
                    continue;
                }

                var distance = Vector2.DistanceSquared(view.Position, from);
                if (distance < best)
                {
                    best = distance;
                    found = view;
                }
            }

            return best < float.MaxValue;
        }

        /// <summary>Between ticks: the wave timer (Mindustry's) and the end state, judged only once both bases have been seen.</summary>
        internal void AfterTick(long tick)
        {
            foreach (var view in snapshot)
            {
                stationSeen |= view.Role == BodyRole.Station;
                bastionSeen |= view.Role == BodyRole.Bastion;
            }

            if (Outcome == Outcome.Undecided && stationSeen && bastionSeen)
            {
                foreach (var death in Deaths)
                {
                    if (death.Role == BodyRole.Station)
                    {
                        Outcome = Outcome.Lost;
                    }
                    else if (death.Role == BodyRole.Bastion && Outcome == Outcome.Undecided)
                    {
                        Outcome = Outcome.Won;
                    }
                }
            }

            if (Peaceful)
            {
                // The timer holds while peaceful, so turning it off doesn't release a backlog.
                NextWaveTick++;
            }
            else if (tick >= NextWaveTick && Outcome == Outcome.Undecided && bastionSeen && !Deaths.Exists(d => d.Role == BodyRole.Bastion))
            {
                SpawnWave();
                NextWaveTick = tick + WaveIntervalTicks;
            }
        }

        internal void ClearTickEvents() => Deaths.Clear();

        /// <summary>Wave n: n+1 skiffs and n/2 brutes, fanned out in front of the bastion, heading for the station.</summary>
        private void SpawnWave()
        {
            var wave = ++WavesSpawned;
            var skiff = RaiderSkiff;
            var brute = RaiderBrute;
            var heading = FlightSystem.Heading(StationPosition - BastionPosition);
            var count = wave + 1 + wave / 2;
            for (var i = 0; i < count; i++)
            {
                var design = i < wave + 1 ? skiff : brute;
                var offset = new Vector2(-14f, (i - count / 2f) * 7f);
                Spawn(design, Faction.Raider, BastionPosition + offset, heading, StationPosition);
            }
        }

        // One instance each, so every raider shares a design key.
        private static readonly ShipDesign RaiderSkiff = ShipPresets.Skiff();
        private static readonly ShipDesign RaiderBrute = ShipPresets.Brute();

        public void HashInto(ref ulong hash)
        {
            foreach (var view in snapshot)
            {
                hash = StateHash.Mix(hash, (ulong)view.Serial);
                hash = StateHash.Mix(hash, ((ulong)BitConverter.SingleToUInt32Bits(view.Position.X) << 32) | BitConverter.SingleToUInt32Bits(view.Position.Y));
                hash = StateHash.Mix(hash, ((ulong)BitConverter.SingleToUInt32Bits(view.Hp) << 32) | BitConverter.SingleToUInt32Bits(view.Rotation));
            }

            hash = StateHash.Mix(hash, ((ulong)WavesSpawned << 32) | (uint)Outcome);
        }
    }
}
