using Microsoft.Xna.Framework;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System;

namespace Proto4X.Factory.Rendering.Models
{
    /// <summary>One axis-aligned box of a model, in footprint-local tiles: x east, y south, z up.</summary>
    /// <param name="Emissive">Drawn at full brightness, unlit - glow panels, screens.</param>
    public readonly record struct ModelBox(Vector3 Min, Vector3 Max, Color Colour, bool Emissive = false);

    /// <summary>A building as a handful of coloured boxes, modelled facing east.</summary>
    public sealed class BlockModel(int size, ModelBox[] boxes)
    {
        public int Size { get; } = size;
        public ModelBox[] Boxes { get; } = boxes;

        public float Height
        {
            get
            {
                var height = 0f;
                foreach (var box in Boxes)
                {
                    height = MathF.Max(height, box.Max.Z);
                }

                return height;
            }
        }
    }

    /// <summary>
    /// The art, such as it is: every building as blocks. The same models feed every style - drawn
    /// directly as 3D, or baked into sprites for the 2D projections - so the styles differ only in
    /// how they get the models to the screen.
    /// </summary>
    /// <remarks>
    /// Moving parts (inserter arms, belt items, the rocket) are not here; each style's renderers
    /// draw those live.
    /// </remarks>
    public static class BlockModels
    {
        private static readonly BlockModel[] models = Build();

        public static BlockModel For(BuildingKind kind) => models[(int)kind];

        /// <summary>
        /// Model space to footprint-local space for a facing: models face east, and a quarter turn
        /// clockwise on screen (y down) is +90 degrees about z.
        /// </summary>
        public static Matrix Facing(Direction direction, int size)
        {
            var half = size * 0.5f;
            return Matrix.CreateTranslation(-half, -half, 0) *
                Matrix.CreateRotationZ(((int)direction - (int)Direction.East) * MathHelper.PiOver2) *
                Matrix.CreateTranslation(half, half, 0);
        }

        /// <summary>Whether every facing looks the same, so a sprite style can bake just one.</summary>
        public static bool IsSymmetric(BuildingKind kind) => kind is BuildingKind.Chest or BuildingKind.Pole or BuildingKind.Receiver or BuildingKind.LaunchSilo;

        private static ModelBox Box(float x0, float y0, float z0, float x1, float y1, float z1, Color colour, bool emissive = false) =>
            new(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1), colour, emissive);

        private static Color Shade(Color colour, float factor) => new((int)(colour.R * factor), (int)(colour.G * factor), (int)(colour.B * factor), 255);

        private static BlockModel[] Build()
        {
            var table = new BlockModel[Buildings.Count];
            var steel = new Color(150, 156, 166);
            var darkSteel = new Color(82, 88, 98);
            var hazard = new Color(214, 170, 52);

            table[(int)BuildingKind.None] = new(1, []);
            table[(int)BuildingKind.Belt] = new(1, []);

            var inserter = Palette.Building(BuildingKind.Inserter);
            table[(int)BuildingKind.Inserter] = new(1,
            [
                Box(0.18f, 0.18f, 0f, 0.82f, 0.82f, 0.12f, darkSteel),
                Box(0.36f, 0.36f, 0.12f, 0.64f, 0.64f, 0.42f, inserter),
            ]);

            var drill = Palette.Building(BuildingKind.Drill);
            table[(int)BuildingKind.Drill] = new(2,
            [
                Box(0.08f, 0.08f, 0f, 1.92f, 1.92f, 0.55f, drill),
                Box(0.35f, 0.35f, 0.55f, 1.45f, 1.65f, 1.05f, Shade(drill, 1.15f)),
                Box(0.6f, 0.75f, 1.05f, 1.2f, 1.25f, 1.55f, darkSteel),
                Box(1.45f, 0.7f, 0.15f, 1.98f, 1.3f, 0.5f, hazard),
            ]);

            var furnace = Palette.Building(BuildingKind.Furnace);
            table[(int)BuildingKind.Furnace] = new(2,
            [
                Box(0.08f, 0.08f, 0f, 1.92f, 1.92f, 0.85f, furnace),
                Box(0.2f, 0.2f, 0.85f, 1.8f, 1.8f, 1.0f, Shade(furnace, 0.8f)),
                Box(1.2f, 1.2f, 1.0f, 1.65f, 1.65f, 1.75f, new Color(70, 62, 60)),
                Box(1.88f, 0.6f, 0.15f, 1.96f, 1.4f, 0.6f, new Color(255, 150, 60), emissive: true),
            ]);

            var assembler = Palette.Building(BuildingKind.Assembler);
            table[(int)BuildingKind.Assembler] = new(3,
            [
                Box(0.08f, 0.08f, 0f, 2.92f, 2.92f, 0.45f, Shade(assembler, 0.8f)),
                Box(0.3f, 0.3f, 0.45f, 2.7f, 2.7f, 1.15f, assembler),
                Box(0.9f, 0.9f, 1.15f, 2.1f, 2.1f, 1.3f, steel),
                Box(0.1f, 0.1f, 0.45f, 0.5f, 0.5f, 0.9f, hazard),
                Box(2.5f, 0.1f, 0.45f, 2.9f, 0.5f, 0.9f, hazard),
                Box(0.1f, 2.5f, 0.45f, 0.5f, 2.9f, 0.9f, hazard),
                Box(2.5f, 2.5f, 0.45f, 2.9f, 2.9f, 0.9f, hazard),
            ]);

            var chest = Palette.Building(BuildingKind.Chest);
            table[(int)BuildingKind.Chest] = new(1,
            [
                Box(0.14f, 0.14f, 0f, 0.86f, 0.86f, 0.5f, chest),
                Box(0.12f, 0.12f, 0.5f, 0.88f, 0.88f, 0.58f, Shade(chest, 0.7f)),
            ]);

            var pole = Palette.Building(BuildingKind.Pole);
            table[(int)BuildingKind.Pole] = new(1,
            [
                Box(0.3f, 0.3f, 0f, 0.7f, 0.7f, 0.1f, darkSteel),
                Box(0.43f, 0.43f, 0.1f, 0.57f, 0.57f, 1.7f, pole),
                Box(0.2f, 0.45f, 1.55f, 0.8f, 0.55f, 1.65f, pole),
                Box(0.18f, 0.44f, 1.65f, 0.26f, 0.56f, 1.72f, new Color(200, 140, 60)),
                Box(0.74f, 0.44f, 1.65f, 0.82f, 0.56f, 1.72f, new Color(200, 140, 60)),
            ]);

            var generator = Palette.Building(BuildingKind.Generator);
            table[(int)BuildingKind.Generator] = new(2,
            [
                Box(0.08f, 0.08f, 0f, 1.92f, 1.92f, 0.4f, darkSteel),
                Box(0.25f, 0.3f, 0.4f, 1.75f, 1.7f, 0.95f, generator),
                Box(0.4f, 0.2f, 0.95f, 0.8f, 0.6f, 1.6f, steel),
                Box(1.2f, 0.75f, 0.95f, 1.6f, 1.25f, 1.15f, new Color(120, 230, 255), emissive: true),
            ]);

            var silo = Palette.Building(BuildingKind.LaunchSilo);
            table[(int)BuildingKind.LaunchSilo] = new(3,
            [
                Box(0.08f, 0.08f, 0f, 2.92f, 2.92f, 0.25f, darkSteel),
                Box(0.1f, 0.1f, 0.25f, 0.6f, 2.9f, 0.7f, silo),
                Box(2.4f, 0.1f, 0.25f, 2.9f, 2.9f, 0.7f, silo),
                Box(0.6f, 0.1f, 0.25f, 2.4f, 0.6f, 0.7f, silo),
                Box(0.6f, 2.4f, 0.25f, 2.4f, 2.9f, 0.7f, silo),
                Box(1.15f, 1.15f, 0.25f, 1.85f, 1.85f, 2.2f, new Color(235, 235, 240)),
                Box(1.3f, 1.3f, 2.2f, 1.7f, 1.7f, 2.6f, new Color(210, 70, 60)),
            ]);

            var receiver = Palette.Building(BuildingKind.Receiver);
            table[(int)BuildingKind.Receiver] = new(2,
            [
                Box(0.08f, 0.08f, 0f, 1.92f, 1.92f, 0.3f, darkSteel),
                Box(0.25f, 0.25f, 0.3f, 1.75f, 1.75f, 0.45f, receiver),
                Box(0.8f, 0.8f, 0.45f, 1.2f, 1.2f, 0.9f, steel),
                Box(0.35f, 0.35f, 0.9f, 1.65f, 1.65f, 1.0f, Shade(receiver, 1.2f)),
            ]);

            var collector = Palette.Building(BuildingKind.Collector);
            table[(int)BuildingKind.Collector] = new(2,
            [
                Box(0.08f, 0.08f, 0f, 1.92f, 1.92f, 0.5f, collector),
                Box(0.3f, 0.3f, 0.5f, 1.4f, 1.7f, 1.0f, Shade(collector, 1.15f)),
                Box(1.4f, 0.4f, 0.3f, 1.98f, 1.6f, 0.9f, darkSteel),
                Box(1.9f, 0.55f, 0.4f, 1.98f, 1.45f, 0.8f, new Color(190, 140, 255), emissive: true),
            ]);

            return table;
        }
    }
}
