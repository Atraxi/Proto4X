using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Input;
using Proto4X.Factory.Rendering.ThreeD;
using Proto4X.Factory.Simulation.Grid;
using Proto4X.Factory.Simulation.Ships;
using Proto4X.Factory.Simulation.Space;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// The space layer in 3D: ships built from their designs' parts, the bastion, beams, HP bars,
    /// the fleet selection and orders, and explosions. Reads the layer's snapshot, so it never
    /// touches space components and draws exactly what the last tick published.
    /// </summary>
    /// <remarks>Registered on the space layer's own world and drawn after the station surface, with no transform: space coordinates are camera world coordinates.</remarks>
    public sealed class SpaceRenderer(SpaceLayer space, FleetController fleet) : IGeometryDrawSystem
    {
        private const float ShipZ = 1.2f;
        private const float ExplosionSeconds = 0.8f;

        private readonly Dictionary<ShipDesign, Model3D> models = [];
        private readonly List<(Vector2 Position, float Size, float Age)> explosions = [];
        private long lastTickSeen = -1;

        /// <summary>Set each frame by the game: the sim tick (to catch deaths once) and the frame time (to age effects).</summary>
        public long Tick { get; set; }
        public float FrameSeconds { get; set; }

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(GeometryBatch batch, in GeometryDrawContext context, List<ArchetypeChunk> archetypeChunks)
        {
            var buffer = batch.Immediate;
            CollectExplosions();

            foreach (var view in space.Snapshot)
            {
                var colour = view.Faction == Faction.Player ? Palette.Player : Palette.Raider;
                switch (view.Role)
                {
                    case BodyRole.Ship when view.DesignKey >= 0 && view.DesignKey < space.Designs.Count:
                        var design = space.Designs[view.DesignKey];
                        var place = Matrix.CreateTranslation(-design.Stats.CentreOfMass.X, -design.Stats.CentreOfMass.Y, 0f)
                            * Matrix.CreateRotationZ(view.Rotation) * Matrix.CreateTranslation(view.Position.X, view.Position.Y, ShipZ);
                        ModelMesher.EmitModel(buffer, GeometryLayer.Opaque, ModelOf(design, colour), place, Color.White);
                        break;

                    case BodyRole.Bastion:
                        DrawBastion(buffer, view.Position);
                        break;
                }

                if (view.Firing)
                {
                    Mesh3D.Beam(buffer, GeometryLayer.Additive, new Vector3(view.Position, ShipZ + 0.6f), new Vector3(view.BeamTarget, ShipZ + 0.6f), 0.18f, colour * 0.9f);
                }

                DrawHealth(buffer, view);
                if (fleet.Selected.Contains(view.Serial))
                {
                    Ring(buffer, view.Position, 3.2f, 0.12f, Palette.Player);
                    if (view.Order == HelmOrder.Move)
                    {
                        Mesh3D.FlatLine(buffer, GeometryLayer.Emissive, view.Position, view.OrderTarget, 0.08f, 0.1f, Palette.Player * 0.6f);
                        Ring(buffer, view.OrderTarget, 1f, 0.1f, Palette.Player);
                    }
                }
            }

            for (var i = explosions.Count - 1; i >= 0; i--)
            {
                var (position, size, age) = explosions[i];
                var t = age / ExplosionSeconds;
                Mesh3D.Cube(buffer, GeometryLayer.Additive, position, size * (0.4f + t), ShipZ - size * 0.3f, new Color(255, 180, 90) * (1f - t), emissive: true);
                explosions[i] = (position, size, age + FrameSeconds);
                if (age + FrameSeconds > ExplosionSeconds)
                {
                    explosions.RemoveAt(i);
                }
            }
        }

        /// <summary>Deaths are a per-tick list; take each tick's once, whatever the frame rate.</summary>
        private void CollectExplosions()
        {
            if (Tick == lastTickSeen)
            {
                return;
            }

            lastTickSeen = Tick;
            foreach (var death in space.Deaths)
            {
                explosions.Add((death.Position, death.Role == BodyRole.Ship ? 4f : 14f, 0f));
            }
        }

        private Model3D ModelOf(ShipDesign design, Color faction)
        {
            if (models.TryGetValue(design, out var model))
            {
                return model;
            }

            var boxes = new List<ModelBox>();
            foreach (var part in design.Parts)
            {
                var (w, h) = part.Size;
                var (x0, y0, x1, y1) = (part.X + 0.05f, part.Y + 0.05f, part.X + w - 0.05f, part.Y + h - 0.05f);
                switch (part.Kind)
                {
                    case PartKind.Hull:
                        boxes.Add(new(new(x0, y0, 0f), new(x1, y1, 0.5f), new Color(150, 156, 166), false));
                        break;
                    case PartKind.Armor:
                        boxes.Add(new(new(x0, y0, 0f), new(x1, y1, 0.75f), new Color(84, 92, 108), false));
                        break;
                    case PartKind.Core:
                        boxes.Add(new(new(x0, y0, 0f), new(x1, y1, 0.7f), new Color(60, 66, 80), false));
                        boxes.Add(new(new(x0 + 0.5f, y0 + 0.5f, 0.7f), new(x1 - 0.5f, y1 - 0.5f, 0.9f), faction, true));
                        break;
                    case PartKind.Reactor:
                        boxes.Add(new(new(x0, y0, 0f), new(x1, y1, 0.6f), new Color(70, 80, 96), false));
                        boxes.Add(new(new(x0 + 0.4f, y0 + 0.4f, 0.6f), new(x1 - 0.4f, y1 - 0.4f, 0.8f), new Color(120, 230, 255), true));
                        break;
                    case PartKind.Thruster:
                        boxes.Add(new(new(x0 + 0.1f, y0, 0f), new(x1 - 0.1f, y1, 0.55f), new Color(110, 100, 96), false));
                        AddFacingGlow(boxes, part, new Color(255, 150, 60));
                        break;
                    case PartKind.Gyro:
                        boxes.Add(new(new(x0 + 0.15f, y0 + 0.15f, 0f), new(x1 - 0.15f, y1 - 0.15f, 0.6f), new Color(214, 170, 52), false));
                        break;
                    case PartKind.Laser:
                        boxes.Add(new(new(x0 + 0.15f, y0, 0f), new(x1 - 0.15f, y1, 0.5f), new Color(120, 60, 70), false));
                        AddFacingGlow(boxes, part, faction);
                        break;
                }
            }

            model = new Model3D([.. boxes]);
            models[design] = model;
            return model;
        }

        /// <summary>A glowing face on the side a thruster blows out of or a laser points: nozzle or muzzle.</summary>
        private static void AddFacingGlow(List<ModelBox> boxes, PartPlacement part, Color glow)
        {
            var (w, h) = part.Size;
            var (x0, y0, x1, y1) = (part.X + 0.2f, part.Y + 0.2f, part.X + w - 0.2f, part.Y + h - 0.2f);
            // A thruster's nozzle is on the side opposite its facing (thrust pushes towards Facing).
            var side = part.Kind == PartKind.Thruster ? part.Facing.Opposite() : part.Facing;
            var (a, b) = side switch
            {
                Simulation.Grid.Direction.North => (new Vector3(x0, part.Y - 0.02f, 0.1f), new Vector3(x1, part.Y + 0.1f, 0.45f)),
                Simulation.Grid.Direction.South => (new Vector3(x0, part.Y + h - 0.1f, 0.1f), new Vector3(x1, part.Y + h + 0.02f, 0.45f)),
                Simulation.Grid.Direction.East => (new Vector3(part.X + w - 0.1f, y0, 0.1f), new Vector3(part.X + w + 0.02f, y1, 0.45f)),
                _ => (new Vector3(part.X - 0.02f, y0, 0.1f), new Vector3(part.X + 0.1f, y1, 0.45f)),
            };
            boxes.Add(new(a, b, glow, true));
        }

        private static void DrawBastion(GeometryBuffer buffer, Vector2 centre)
        {
            Mesh3D.Box(buffer, GeometryLayer.Opaque, new Vector3(centre.X - 7f, centre.Y - 7f, 0f), new Vector3(centre.X + 7f, centre.Y + 7f, 1.5f), new Color(70, 40, 44));
            Mesh3D.Box(buffer, GeometryLayer.Opaque, new Vector3(centre.X - 4f, centre.Y - 4f, 1.5f), new Vector3(centre.X + 4f, centre.Y + 4f, 3.5f), new Color(96, 50, 54));
            Mesh3D.Box(buffer, GeometryLayer.Opaque, new Vector3(centre.X - 1.5f, centre.Y - 1.5f, 3.5f), new Vector3(centre.X + 1.5f, centre.Y + 1.5f, 4.2f), Palette.Raider, emissive: true);
        }

        private static void DrawHealth(GeometryBuffer buffer, in ShipView view)
        {
            var fraction = view.MaxHp > 0 ? Math.Clamp(view.Hp / view.MaxHp, 0f, 1f) : 0f;
            var width = view.Role == BodyRole.Ship ? 3f : 12f;
            var offset = view.Role == BodyRole.Ship ? 3.5f : view.Role == BodyRole.Station ? 2f : 9f;
            var x = view.Position.X - width / 2f;
            var y = view.Role == BodyRole.Station ? view.Position.Y - 10f : view.Position.Y - offset;
            Mesh3D.Flat(buffer, GeometryLayer.Emissive, x, y, width, 0.35f, ShipZ + 1.4f, new Color(20, 22, 26));
            var colour = fraction > 0.5f ? Palette.PowerGood : fraction > 0.25f ? Palette.PowerLow : Palette.PowerNone;
            Mesh3D.Flat(buffer, GeometryLayer.Emissive, x, y, width * fraction, 0.35f, ShipZ + 1.42f, colour);
        }

        private static void Ring(GeometryBuffer buffer, Vector2 centre, float radius, float thickness, Color colour)
        {
            const int segments = 24;
            for (var s = 0; s < segments; s++)
            {
                var a0 = s * MathF.Tau / segments;
                var a1 = (s + 1) * MathF.Tau / segments;
                Mesh3D.FlatLine(buffer, GeometryLayer.Emissive, centre + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radius,
                    centre + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius, thickness, 0.12f, colour);
            }
        }
    }
}
