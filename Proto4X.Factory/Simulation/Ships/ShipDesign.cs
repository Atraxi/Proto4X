using Microsoft.Xna.Framework;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proto4X.Factory.Simulation.Ships
{
    public enum PartKind : byte
    {
        Hull,
        Armor,
        /// <summary>Supplies control: the automation stand-in for Cosmoteer's crew (plans/03).</summary>
        Core,
        Reactor,
        Thruster,
        Gyro,
        Laser,
    }

    /// <summary>
    /// Static per-part tuning. Sizes are for the part facing north (the ship's nose); east/west swap
    /// them. Power and control are signed: positive supplies, negative draws.
    /// </summary>
    public sealed record PartSpec(PartKind Kind, string Name, int Width, int Height, int Mass, int Hp, int Power, int Control,
        (ItemId Item, int Count)[] Cost, float Thrust = 0, float Torque = 0, float Dps = 0, float Range = 0)
    {
        public bool Directional => Kind is PartKind.Thruster or PartKind.Laser;
    }

    public static class Parts
    {
        public static readonly PartSpec[] All =
        [
            new(PartKind.Hull, "Hull", 1, 1, 1, 30, 0, 0, [(ItemId.Frame, 1)]),
            new(PartKind.Armor, "Armor", 1, 1, 3, 140, 0, 0, [(ItemId.ArmorPlate, 1)]),
            new(PartKind.Core, "Autonomy core", 2, 2, 6, 120, -2, 12, [(ItemId.Frame, 2), (ItemId.Processor, 2)]),
            new(PartKind.Reactor, "Reactor", 2, 2, 6, 100, 16, -1, [(ItemId.Frame, 2), (ItemId.PowerCell, 2)]),
            new(PartKind.Thruster, "Thruster", 1, 2, 2, 50, -2, -1, [(ItemId.Frame, 1), (ItemId.Engine, 1)], Thrust: 80f),
            new(PartKind.Gyro, "Gyro", 1, 1, 2, 40, -2, -1, [(ItemId.Frame, 1), (ItemId.Engine, 1)], Torque: 60f),
            new(PartKind.Laser, "Laser", 1, 2, 2, 50, -4, -2, [(ItemId.Frame, 1), (ItemId.Emitter, 1)], Dps: 18f, Range: 22f),
        ];

        public static PartSpec Spec(PartKind kind) => All[(int)kind];
    }

    /// <summary>One placed part: grid cell of its top-left corner, and its facing.</summary>
    public readonly record struct PartPlacement(PartKind Kind, int X, int Y, Direction Facing)
    {
        public (int Width, int Height) Size
        {
            get
            {
                var spec = Parts.Spec(Kind);
                return Facing is Direction.East or Direction.West ? (spec.Height, spec.Width) : (spec.Width, spec.Height);
            }
        }

        public Vector2 Centre
        {
            get
            {
                var (w, h) = Size;
                return new Vector2(X + w * 0.5f, Y + h * 0.5f);
            }
        }

        public bool Covers(int x, int y)
        {
            var (w, h) = Size;
            return x >= X && y >= Y && x < X + w && y < Y + h;
        }
    }

    /// <summary>A player-made (or preset) ship: a name and its parts on a grid of up to 16×16. Immutable.</summary>
    public sealed class ShipDesign
    {
        public const int GridSize = 16;

        public ShipDesign(string name, IEnumerable<PartPlacement> parts)
        {
            Name = name;
            Parts = [.. parts];
            Stats = ShipStats.Compute(this);
        }

        public string Name { get; }
        public IReadOnlyList<PartPlacement> Parts { get; }
        public ShipStats Stats { get; }

        public ShipDesign With(string? name = null, IEnumerable<PartPlacement>? parts = null) => new(name ?? Name, parts ?? Parts);

        /// <summary>The part covering a cell, or -1.</summary>
        public int PartAt(int x, int y)
        {
            for (var i = 0; i < Parts.Count; i++)
            {
                if (Parts[i].Covers(x, y))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Whether <paramref name="part"/> fits inside the grid without overlapping anything.</summary>
        public bool CanAdd(PartPlacement part)
        {
            var (w, h) = part.Size;
            if (part.X < 0 || part.Y < 0 || part.X + w > GridSize || part.Y + h > GridSize)
            {
                return false;
            }

            for (var y = part.Y; y < part.Y + h; y++)
            {
                for (var x = part.X; x < part.X + w; x++)
                {
                    if (PartAt(x, y) >= 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Everything derived from a design: validity, budgets, flight and combat numbers, cost.
    /// Pure, so it's checked headless. Rules are Cosmoteer's, with crew replaced by control (plans/03).
    /// </summary>
    public sealed class ShipStats
    {
        /// <summary>Linear drag per second: top speed = acceleration ÷ drag.</summary>
        public const float Drag = 0.6f;
        public const float MaxTurnRate = 2.2f;

        public List<string> Errors { get; } = [];
        public bool IsValid => Errors.Count == 0;

        public int Mass;
        public Vector2 CentreOfMass;
        public float Inertia;
        public int Hp;
        public int PowerSupply;
        public int PowerDraw;
        public int ControlSupply;
        public int ControlLoad;

        /// <summary>power × control, each min(1, supply ÷ demand). Every active part runs at this.</summary>
        public float Efficiency;

        /// <summary>Acceleration (tiles/s²) pushing toward each local direction, indexed by <see cref="Direction"/>.</summary>
        public float[] Acceleration { get; } = new float[4];

        /// <summary>Angular acceleration (rad/s²) available both ways - the weaker of clockwise and anticlockwise.</summary>
        public float AngularAcceleration;
        public float Dps;
        public float Range;
        public int BuildTicks;

        /// <summary>Thrusters whose exhaust is blocked, by part index; they contribute nothing.</summary>
        public HashSet<int> BlockedThrusters { get; } = [];

        public (ItemId Item, int Count)[] Cost { get; private set; } = [];

        public float TopSpeed => Acceleration[(int)Direction.North] / Drag;

        public static ShipStats Compute(ShipDesign design)
        {
            var stats = new ShipStats();
            var parts = design.Parts;
            if (parts.Count == 0)
            {
                stats.Errors.Add("No parts.");
                return stats;
            }

            var cost = new Dictionary<ItemId, int>();
            var weighted = Vector2.Zero;
            foreach (var part in parts)
            {
                var spec = Parts.Spec(part.Kind);
                stats.Mass += spec.Mass;
                stats.Hp += spec.Hp;
                weighted += part.Centre * spec.Mass;
                if (spec.Power > 0) stats.PowerSupply += spec.Power; else stats.PowerDraw -= spec.Power;
                if (spec.Control > 0) stats.ControlSupply += spec.Control; else stats.ControlLoad -= spec.Control;
                foreach (var (item, count) in spec.Cost)
                {
                    cost[item] = cost.GetValueOrDefault(item) + count;
                }
            }

            stats.Cost = [.. cost.OrderBy(p => p.Key).Select(p => (p.Key, p.Value))];
            stats.CentreOfMass = weighted / stats.Mass;
            stats.BuildTicks = 120 + 30 * parts.Count;

            var power = stats.PowerDraw == 0 ? 1f : Math.Min(1f, stats.PowerSupply / (float)stats.PowerDraw);
            var control = stats.ControlLoad == 0 ? 1f : Math.Min(1f, stats.ControlSupply / (float)stats.ControlLoad);
            stats.Efficiency = power * control;

            if (!parts.Any(p => p.Kind == PartKind.Core))
            {
                stats.Errors.Add("Needs an autonomy core.");
            }

            if (!IsConnected(design))
            {
                stats.Errors.Add("Parts are not all connected.");
            }

            var torqueClockwise = 0f;
            var torqueAnticlockwise = 0f;
            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                var spec = Parts.Spec(part.Kind);
                var offset = part.Centre - stats.CentreOfMass;
                var (w, h) = part.Size;
                stats.Inertia += spec.Mass * (offset.LengthSquared() + (w * w + h * h) / 12f);

                switch (part.Kind)
                {
                    case PartKind.Thruster:
                    {
                        if (IsExhaustBlocked(design, part))
                        {
                            stats.BlockedThrusters.Add(i);
                            continue;
                        }

                        var thrust = spec.Thrust * stats.Efficiency;
                        stats.Acceleration[(int)part.Facing] += thrust;
                        var force = new Vector2(part.Facing.Dx(), part.Facing.Dy()) * thrust;
                        // y points down, so a positive 2D cross product turns clockwise on screen.
                        var torque = offset.X * force.Y - offset.Y * force.X;
                        if (torque > 0) torqueClockwise += torque; else torqueAnticlockwise -= torque;
                        break;
                    }

                    case PartKind.Gyro:
                        torqueClockwise += spec.Torque * stats.Efficiency;
                        torqueAnticlockwise += spec.Torque * stats.Efficiency;
                        break;

                    case PartKind.Laser:
                        stats.Dps += spec.Dps * stats.Efficiency;
                        stats.Range = Math.Max(stats.Range, spec.Range);
                        break;
                }
            }

            for (var d = 0; d < 4; d++)
            {
                stats.Acceleration[d] /= stats.Mass;
            }

            stats.AngularAcceleration = stats.Inertia > 0 ? Math.Min(torqueClockwise, torqueAnticlockwise) / stats.Inertia : 0f;

            if (stats.Acceleration[(int)Direction.North] <= 0f)
            {
                stats.Errors.Add("No working forward thrust.");
            }

            if (stats.AngularAcceleration <= 0f)
            {
                stats.Errors.Add("Can't turn: needs a gyro or thrusters off-centre.");
            }

            return stats;
        }

        /// <summary>Cosmoteer's rule: a thruster's exhaust must run clear to the edge of the grid.</summary>
        public static bool IsExhaustBlocked(ShipDesign design, PartPlacement thruster)
        {
            // Exhaust leaves opposite to the push.
            var dx = -thruster.Facing.Dx();
            var dy = -thruster.Facing.Dy();
            var (w, h) = thruster.Size;
            for (var lane = 0; lane < (dx != 0 ? h : w); lane++)
            {
                var x = dx > 0 ? thruster.X + w : dx < 0 ? thruster.X - 1 : thruster.X + lane;
                var y = dy > 0 ? thruster.Y + h : dy < 0 ? thruster.Y - 1 : thruster.Y + lane;
                while (x >= 0 && y >= 0 && x < ShipDesign.GridSize && y < ShipDesign.GridSize)
                {
                    if (design.PartAt(x, y) >= 0)
                    {
                        return true;
                    }

                    x += dx;
                    y += dy;
                }
            }

            return false;
        }

        private static bool IsConnected(ShipDesign design)
        {
            var parts = design.Parts;
            var seen = new bool[parts.Count];
            var queue = new Queue<int>();
            queue.Enqueue(0);
            seen[0] = true;
            var reached = 1;
            while (queue.Count > 0)
            {
                var a = parts[queue.Dequeue()];
                for (var j = 0; j < parts.Count; j++)
                {
                    if (!seen[j] && Touches(a, parts[j]))
                    {
                        seen[j] = true;
                        reached++;
                        queue.Enqueue(j);
                    }
                }
            }

            return reached == parts.Count;
        }

        /// <summary>Whether two parts share an edge.</summary>
        private static bool Touches(PartPlacement a, PartPlacement b)
        {
            var (aw, ah) = a.Size;
            var (bw, bh) = b.Size;
            var overlapX = Math.Min(a.X + aw, b.X + bw) - Math.Max(a.X, b.X);
            var overlapY = Math.Min(a.Y + ah, b.Y + bh) - Math.Max(a.Y, b.Y);
            return (overlapX > 0 && overlapY == 0) || (overlapY > 0 && overlapX == 0);
        }
    }
}
