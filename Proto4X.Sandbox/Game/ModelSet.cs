using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using Proto4X.Sandbox.Components;
using System;

namespace Proto4X.Sandbox.Game
{
    /// <summary>
    /// The game's art: every ship, station and rock as a handful of boxes, registered with
    /// <see cref="ModelLibrary"/>. Faction colour is a per-entity tint, so there is one model per
    /// kind rather than a recoloured copy per faction.
    /// </summary>
    /// <remarks>
    /// Models face +x, are centred on the origin and stand on z = 0. Parts that should keep their own
    /// colour whatever the owner (engine glow, canopies) are untinted.
    /// </remarks>
    public class ModelSet
    {
        private static readonly Color[] FactionTints = BuildFactionTints();

        public int MinerModel { get; }
        public int FighterModel { get; }
        public int StationModel { get; }
        public int[] AsteroidModelIds { get; }

        /// <summary>A 1x1 white texture, the drawing primitive every screen-space overlay stretches into lines and bars.</summary>
        public Texture2D Pixel { get; }

        public ModelSet(GraphicsDevice graphicsDevice)
        {
            Pixel = new Texture2D(graphicsDevice, 1, 1);
            Pixel.SetData([Color.White]);

            var hull = new Color(200, 204, 212);
            var plate = new Color(150, 154, 164);
            var dark = new Color(96, 100, 110);
            var glow = new Color(255, 170, 70);
            var glass = new Color(120, 200, 255);
            var yellow = new Color(230, 190, 60);

            MinerModel = ModelLibrary.Add(new Model3D(
            [
                Box(-14, -7, 0, 8, 7, 7, hull),
                Box(8, -5, 1, 15, 5, 6, plate),
                Box(-12, -11, 1, -2, -7, 6, dark),
                Box(-12, 7, 1, -2, 11, 6, dark),
                Box(15, -2, 2, 19, 2, 4, yellow, tinted: false),
                Box(-17, -4, 2, -14, 4, 5, glow, emissive: true, tinted: false),
            ]));

            FighterModel = ModelLibrary.Add(new Model3D(
            [
                Box(-12, -3, 0, 14, 3, 5, hull),
                Box(14, -1.5f, 1, 18, 1.5f, 4, plate),
                Box(-10, -14, 0, 0, 14, 2, plate),
                Box(2, -14, 0.5f, 8, -11, 3, dark),
                Box(2, 11, 0.5f, 8, 14, 3, dark),
                Box(2, -1.5f, 5, 8, 1.5f, 7, glass, tinted: false),
                Box(-15, -2.5f, 1, -12, 2.5f, 4, glow, emissive: true, tinted: false),
            ]));

            StationModel = ModelLibrary.Add(new Model3D(
            [
                Box(-16, -16, 0, 16, 16, 14, hull),
                Box(-30, -4, 4, 30, 4, 9, plate),
                Box(-4, -30, 4, 4, 30, 9, plate),
                Box(-34, -7, 2, -28, 7, 11, dark),
                Box(28, -7, 2, 34, 7, 11, dark),
                Box(-7, -34, 2, 7, -28, 11, dark),
                Box(-7, 28, 2, 7, 34, 11, dark),
                Box(-8, -8, 14, 8, 8, 20, plate),
                Box(-1, -1, 20, 1, 1, 30, dark),
                Box(-4, -4, 20, 4, 4, 22, glow, emissive: true, tinted: false),
            ]));

            var rock = new Color(120, 112, 104);
            var rockDark = new Color(92, 86, 80);
            AsteroidModelIds =
            [
                ModelLibrary.Add(new Model3D(
                [
                    Box(-9, -8, 0, 9, 8, 7, rock, tinted: false),
                    Box(-6, -5, 7, 5, 6, 11, rockDark, tinted: false),
                    Box(4, 2, 0, 12, 9, 5, rockDark, tinted: false),
                ])),
                ModelLibrary.Add(new Model3D(
                [
                    Box(-13, -11, 0, 12, 12, 9, rock, tinted: false),
                    Box(-8, -7, 9, 6, 5, 14, rockDark, tinted: false),
                    Box(8, -15, 0, 15, -4, 6, rockDark, tinted: false),
                ])),
                ModelLibrary.Add(new Model3D(
                [
                    Box(-10, -10, 0, 10, 9, 8, rock, tinted: false),
                    Box(-5, -4, 8, 6, 7, 12, rockDark, tinted: false),
                    Box(-14, 2, 0, -8, 9, 5, rockDark, tinted: false),
                ])),
            ];
        }

        public int ModelFor(UnitKind kind) => kind switch
        {
            UnitKind.Miner => MinerModel,
            UnitKind.Fighter => FighterModel,
            UnitKind.Station => StationModel,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Has no model"),
        };

        public static Color FactionColour(FactionId faction) => FactionTints[(int)faction];

        private static ModelBox Box(float x0, float y0, float z0, float x1, float y1, float z1, Color colour, bool emissive = false, bool tinted = true) =>
            new(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1), colour, emissive, tinted);

        private static Color[] BuildFactionTints()
        {
            var tints = new Color[Enum.GetValues<FactionId>().Length];
            for (var i = 0; i < tints.Length; i++)
            {
                tints[i] = Color.Gray;
            }

            tints[(int)FactionId.Player] = new Color(120, 190, 255);
            tints[(int)FactionId.Enemy] = new Color(255, 120, 110);
            tints[(int)FactionId.Rival] = new Color(240, 200, 110);
            return tints;
        }
    }
}
