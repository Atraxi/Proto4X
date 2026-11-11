using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System;

namespace Proto4X.Factory.Rendering.Models
{
    /// <summary>A building as a handful of coloured boxes, modelled facing east, in footprint-local tiles.</summary>
    public sealed class BlockModel(int size, ModelBox[] boxes) : Model3D(boxes)
    {
        public int Size { get; } = size;
    }

    /// <summary>
    /// The art, such as it is: every building as blocks, meshed into the chunk caches and drawn
    /// live for ghosts and pending removals.
    /// </summary>
    /// <remarks>
    /// Moving parts (inserter arms, belt items, the rocket) are not here; the live renderers draw them.
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
                Box(1.93f, 0.55f, 0.4f, 1.995f, 1.45f, 0.8f, new Color(190, 140, 255), emissive: true),
            ]);

            // A Factorissimo factory building: a big shed with a roller door and roof lights.
            var warehouse = Palette.Building(BuildingKind.Warehouse);
            table[(int)BuildingKind.Warehouse] = new(6,
            [
                Box(0.06f, 0.06f, 0f, 5.94f, 5.94f, 0.3f, darkSteel),
                Box(0.2f, 0.2f, 0.3f, 5.8f, 5.8f, 2.2f, warehouse),
                Box(0.1f, 0.1f, 2.2f, 5.9f, 5.9f, 2.4f, Shade(warehouse, 0.75f)),
                Box(5.8f, 2.0f, 0.3f, 5.93f, 4.0f, 1.6f, hazard),
                Box(1.0f, 1.0f, 2.4f, 5.0f, 1.3f, 2.46f, new Color(255, 236, 170), emissive: true),
                Box(1.0f, 4.7f, 2.4f, 5.0f, 5.0f, 2.46f, new Color(255, 236, 170), emissive: true),
            ]);

            foreach (var (kind, light) in ((BuildingKind, Color)[])[(BuildingKind.InputDock, new Color(110, 255, 150)), (BuildingKind.OutputDock, new Color(255, 170, 80))])
            {
                table[(int)kind] = new(1,
                [
                    Box(0.08f, 0.08f, 0f, 0.92f, 0.92f, 0.22f, Palette.Building(kind)),
                    Box(0.3f, 0.3f, 0.22f, 0.7f, 0.7f, 0.4f, darkSteel),
                    Box(0.38f, 0.38f, 0.4f, 0.62f, 0.62f, 0.46f, light, emissive: true),
                ]);
            }

            // A gantry over a cradle.
            var shipyard = Palette.Building(BuildingKind.Shipyard);
            table[(int)BuildingKind.Shipyard] = new(4,
            [
                Box(0.06f, 0.06f, 0f, 3.94f, 3.94f, 0.3f, darkSteel),
                Box(1.1f, 1.1f, 0.3f, 2.9f, 2.9f, 0.6f, shipyard),
                Box(0.2f, 0.2f, 0.3f, 0.6f, 0.6f, 2.4f, steel),
                Box(3.4f, 0.2f, 0.3f, 3.8f, 0.6f, 2.4f, steel),
                Box(0.2f, 3.4f, 0.3f, 0.6f, 3.8f, 2.4f, steel),
                Box(3.4f, 3.4f, 0.3f, 3.8f, 3.8f, 2.4f, steel),
                Box(0.2f, 0.2f, 2.4f, 3.8f, 0.6f, 2.7f, hazard),
                Box(0.2f, 3.4f, 2.4f, 3.8f, 3.8f, 2.7f, hazard),
                Box(1.7f, 0.6f, 2.45f, 2.3f, 3.4f, 2.55f, new Color(140, 220, 255), emissive: true),
            ]);

            table[(int)BuildingKind.SolarPanel] = new(2,
            [
                Box(0.85f, 0.85f, 0f, 1.15f, 1.15f, 0.6f, darkSteel),
                Box(0.06f, 0.06f, 0.6f, 1.94f, 1.94f, 0.68f, Palette.Building(BuildingKind.SolarPanel)),
                Box(0.06f, 0.97f, 0.68f, 1.94f, 1.03f, 0.7f, steel),
                Box(0.97f, 0.06f, 0.68f, 1.03f, 1.94f, 0.7f, steel),
            ]);

            return table;
        }
    }
}
