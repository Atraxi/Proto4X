using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Simulation.Space
{
    /// <summary>Applies this tick's move orders. Orders name ships by serial; this visits ships, never looks one up.</summary>
    public sealed class ShipOrderSystem(SpaceLayer space) : IUpdateSystem
    {
        private static readonly ComponentRef[] required = [ComponentRef.Of<SpaceShip>(), ComponentRef.Of<Helm>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => required;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            if (space.PendingOrders.Count == 0)
            {
                return;
            }

            foreach (var chunk in archetypeChunks)
            {
                var ships = chunk.GetSpan<SpaceShip>();
                var helms = chunk.GetSpan<Helm>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    foreach (var (serials, target) in space.PendingOrders)
                    {
                        if (ships[i].Faction == Faction.Player && serials.Contains(ships[i].Serial))
                        {
                            helms[i].Order = HelmOrder.Move;
                            helms[i].Target = target;
                            helms[i].Post = target;
                            helms[i].Engaging = -1;
                        }
                    }
                }
            }

            space.PendingOrders.Clear();
        }
    }

    /// <summary>
    /// Autopilot and flight, Cosmoteer-style: thrust-limited acceleration per local axis, linear drag,
    /// torque-limited turning with bang-bang braking. Decides each ship's goal from its helm order and
    /// the previous tick's snapshot (engage the nearest hostile in aggro range, else the post).
    /// </summary>
    public sealed class FlightSystem(SpaceLayer space) : IUpdateSystem
    {
        private const float Dt = 1f / Universe.TicksPerSecond;
        private static readonly ComponentRef[] required =
            [ComponentRef.Of<SpaceShip>(), ComponentRef.Of<Body>(), ComponentRef.Of<Drive>(), ComponentRef.Of<Helm>(), ComponentRef.Of<Gunnery>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => required;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            foreach (var chunk in archetypeChunks)
            {
                var ships = chunk.GetSpan<SpaceShip>();
                var bodies = chunk.GetSpan<Body>();
                var drives = chunk.GetSpan<Drive>();
                var helms = chunk.GetSpan<Helm>();
                var guns = chunk.GetSpan<Gunnery>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var body = ref bodies[i];
                    ref var drive = ref drives[i];
                    ref var helm = ref helms[i];
                    if (drive.Forward <= 0f && drive.Angular <= 0f)
                    {
                        continue; // station, bastion: immobile
                    }

                    var (goal, face) = Decide(ships[i], ref body, ref helm, guns[i].Range);
                    Steer(ref body, drive, goal, face);
                }
            }
        }

        private (Vector2 Goal, Vector2? Face) Decide(in SpaceShip ship, ref Body body, ref Helm helm, float range)
        {
            if (helm.Order == HelmOrder.Move)
            {
                if (Vector2.Distance(body.Position, helm.Target) < 2.5f && body.Velocity.Length() < 1.5f)
                {
                    helm.Order = HelmOrder.Idle;
                }

                helm.Engaging = -1;
                return (helm.Target, null);
            }

            var aggro = ship.Faction == Faction.Raider ? SpaceLayer.RaiderAggroRange : SpaceLayer.PlayerAggroRange;
            var anchor = ship.Faction == Faction.Raider ? body.Position : helm.Post;
            if (space.TryFindNearestHostile(ship.Faction, anchor, body.Position, aggro, out var enemy))
            {
                helm.Engaging = enemy.Serial;
                var away = body.Position - enemy.Position;
                var distance = away.Length();
                var standoff = Math.Max(4f, range * 0.8f);
                var goal = distance > 0.01f ? enemy.Position + away / distance * standoff : body.Position;
                return (goal, enemy.Position);
            }

            helm.Engaging = -1;
            return (helm.Post, null);
        }

        private static void Steer(ref Body body, in Drive drive, Vector2 goal, Vector2? face)
        {
            var toGoal = goal - body.Position;
            var distance = toGoal.Length();

            // Heading: face the target in a fight, else the direction of travel when it's worth turning for.
            float? desiredHeading = face is { } f ? Heading(f - body.Position) : distance > 3f ? Heading(toGoal) : null;
            var desiredSpin = 0f;
            if (desiredHeading is { } heading)
            {
                var error = WrapAngle(heading - body.Rotation);
                desiredSpin = MathF.Sign(error) * MathF.Min(drive.MaxTurnRate, MathF.Sqrt(2f * drive.Angular * MathF.Abs(error)));
            }

            var spinStep = drive.Angular * Dt;
            body.AngularVelocity += Math.Clamp(desiredSpin - body.AngularVelocity, -spinStep, spinStep);

            // Arrival: as fast as possible, but no faster than can still stop in time.
            var braking = MathF.Max(drive.Reverse, drive.Forward * 0.5f);
            var topSpeed = drive.Forward / Ships.ShipStats.Drag;
            var desiredVelocity = distance > 0.3f
                ? toGoal / distance * MathF.Min(topSpeed, MathF.Sqrt(2f * braking * distance))
                : Vector2.Zero;

            var forward = new Vector2(MathF.Sin(body.Rotation), -MathF.Cos(body.Rotation));
            var right = new Vector2(MathF.Cos(body.Rotation), MathF.Sin(body.Rotation));
            var change = desiredVelocity - body.Velocity;
            var alongForward = Math.Clamp(Vector2.Dot(change, forward) / Dt, -drive.Reverse, drive.Forward);
            var alongRight = Math.Clamp(Vector2.Dot(change, right) / Dt, -drive.Left, drive.Right);

            body.Velocity += (forward * alongForward + right * alongRight) * Dt;
            body.Velocity -= body.Velocity * (Ships.ShipStats.Drag * Dt);
            body.Position += body.Velocity * Dt;
            body.Rotation = WrapAngle(body.Rotation + body.AngularVelocity * Dt);
        }

        /// <summary>The rotation whose nose points along <paramref name="direction"/>.</summary>
        public static float Heading(Vector2 direction) => MathF.Atan2(direction.X, -direction.Y);

        public static float WrapAngle(float angle)
        {
            while (angle > MathF.PI) angle -= MathF.Tau;
            while (angle < -MathF.PI) angle += MathF.Tau;
            return angle;
        }
    }

    /// <summary>
    /// Beams: each armed body fires at the nearest hostile in range. Damage goes into the layer's
    /// per-tick damage store, keyed by serial - the store-and-pull pattern Exchange uses for
    /// cross-archetype writes - and <see cref="DamageSystem"/> pulls it.
    /// </summary>
    public sealed class GunnerySystem(SpaceLayer space) : IUpdateSystem
    {
        private static readonly ComponentRef[] required = [ComponentRef.Of<SpaceShip>(), ComponentRef.Of<Body>(), ComponentRef.Of<Gunnery>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => required;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            foreach (var chunk in archetypeChunks)
            {
                var ships = chunk.GetSpan<SpaceShip>();
                var bodies = chunk.GetSpan<Body>();
                var guns = chunk.GetSpan<Gunnery>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var gun = ref guns[i];
                    gun.Firing = false;
                    gun.Target = -1;
                    if (gun.Dps <= 0f)
                    {
                        continue;
                    }

                    var position = bodies[i].Position;
                    if (!space.TryFindNearestHostile(ships[i].Faction, position, position, gun.Range, out var enemy))
                    {
                        continue;
                    }

                    gun.Firing = true;
                    gun.Target = enemy.Serial;
                    gun.TargetPosition = enemy.Position;
                    space.Damage[enemy.Serial] = space.Damage.GetValueOrDefault(enemy.Serial) + gun.Dps / Universe.TicksPerSecond;
                }
            }
        }
    }

    /// <summary>
    /// Pulls damage, removes the dead. Iterates each chunk backwards so removals queue in descending
    /// index order - the engine removal-order workaround (see DeconstructionSystem).
    /// </summary>
    public sealed class DamageSystem(SpaceLayer space) : IUpdateSystem
    {
        private static readonly ComponentRef[] required = [ComponentRef.Of<SpaceShip>(), ComponentRef.Of<Body>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => required;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            if (space.Damage.Count == 0)
            {
                return;
            }

            foreach (var chunk in archetypeChunks)
            {
                var ships = chunk.GetSpan<SpaceShip>();
                var bodies = chunk.GetSpan<Body>();
                for (var i = chunk.EntityCount - 1; i >= 0; i--)
                {
                    ref var ship = ref ships[i];
                    if (!space.Damage.TryGetValue(ship.Serial, out var damage))
                    {
                        continue;
                    }

                    ship.Hp -= damage;
                    if (ship.Hp <= 0f)
                    {
                        chunk.QueueEntityForRemovalByIndex(i);
                        space.Deaths.Add((ship.Serial, ship.Faction, ship.Role, bodies[i].Position));
                    }
                }
            }

            space.Damage.Clear();
        }
    }

    /// <summary>Publishes every body for the next tick's targeting, rendering, picking and the HUD (the Sandbox snapshot pattern).</summary>
    public sealed class SpaceSnapshotSystem(SpaceLayer space) : IUpdateSystem
    {
        private static readonly ComponentRef[] required =
            [ComponentRef.Of<SpaceShip>(), ComponentRef.Of<Body>(), ComponentRef.Of<Helm>(), ComponentRef.Of<Gunnery>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => required;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var snapshot = space.WritableSnapshot;
            snapshot.Clear();
            foreach (var chunk in archetypeChunks)
            {
                var ships = chunk.GetSpan<SpaceShip>();
                var bodies = chunk.GetSpan<Body>();
                var helms = chunk.GetSpan<Helm>();
                var guns = chunk.GetSpan<Gunnery>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var ship = ref ships[i];
                    if (ship.Hp <= 0f)
                    {
                        continue; // removed at the end of this tick
                    }

                    ref var body = ref bodies[i];
                    ref var gun = ref guns[i];
                    snapshot.Add(new ShipView(ship.Serial, ship.Faction, ship.Role, ship.DesignKey, body.Position, body.Rotation, body.Velocity,
                        ship.Hp, ship.MaxHp, gun.Firing, gun.TargetPosition, gun.Range, helms[i].Order, helms[i].Target));
                }
            }

            snapshot.Sort((a, b) => a.Serial.CompareTo(b.Serial));
        }
    }
}
