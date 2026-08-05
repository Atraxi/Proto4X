using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using Proto4X.Sandbox.Components;

namespace Proto4X.Sandbox.Game
{
    /// <summary>
    /// Loads the game's art and registers it with <see cref="SpriteLibrary"/>, including a
    /// per-faction tinted copy of every ship and station.
    /// </summary>
    /// <remarks>
    /// The tinting happens here, at load, by baking recoloured copies of the texture, because
    /// <c>Drawable</c> carries no colour and <c>Renderer</c> always draws <c>Color.White</c>.
    /// Baking copies costs a little VRAM but needs no engine change - see "Drawable has no tint,
    /// scale or layer" in ENGINE_REQUESTS.md.
    /// </remarks>
    public class SpriteSet
    {
        private static readonly Color PlayerTint = new(120, 190, 255);
        private static readonly Color EnemyTint = new(255, 120, 110);

        private readonly int playerMiner;
        private readonly int enemyMiner;
        private readonly int playerFighter;
        private readonly int enemyFighter;
        private readonly int playerStation;
        private readonly int enemyStation;

        public int[] AsteroidSpriteIds { get; }

        /// <summary>A 1x1 white texture, the drawing primitive every overlay renderer stretches into lines and bars.</summary>
        public Texture2D Pixel { get; }

        public SpriteSet(ContentManager content, GraphicsDevice graphicsDevice)
        {
            Pixel = new Texture2D(graphicsDevice, 1, 1);
            Pixel.SetData([Color.White]);

            var miner = content.Load<Texture2D>("Ships/ship_freighter_32x32");
            var fighter = content.Load<Texture2D>("Ships/ship_fighter_32x32");
            var station = content.Load<Texture2D>("Stations/station_core_64x64");

            playerMiner = SpriteLibrary.Add(Tint(graphicsDevice, miner, PlayerTint));
            enemyMiner = SpriteLibrary.Add(Tint(graphicsDevice, miner, EnemyTint));
            playerFighter = SpriteLibrary.Add(Tint(graphicsDevice, fighter, PlayerTint));
            enemyFighter = SpriteLibrary.Add(Tint(graphicsDevice, fighter, EnemyTint));
            playerStation = SpriteLibrary.Add(Tint(graphicsDevice, station, PlayerTint));
            enemyStation = SpriteLibrary.Add(Tint(graphicsDevice, station, EnemyTint));

            AsteroidSpriteIds =
            [
                SpriteLibrary.Add(content.Load<Texture2D>("Resources/rock1_24x24")),
                SpriteLibrary.Add(content.Load<Texture2D>("Resources/rock2_32x32")),
                SpriteLibrary.Add(content.Load<Texture2D>("Resources/rock3_28x28")),
            ];
        }

        public int MinerSpriteFor(FactionId faction) => faction == FactionId.Player ? playerMiner : enemyMiner;

        public int FighterSpriteFor(FactionId faction) => faction == FactionId.Player ? playerFighter : enemyFighter;

        public int StationSpriteFor(FactionId faction) => faction == FactionId.Player ? playerStation : enemyStation;

        public static Color FactionColour(FactionId faction) => faction switch
        {
            FactionId.Player => PlayerTint,
            FactionId.Enemy => EnemyTint,
            _ => Color.Gray,
        };

        private static Texture2D Tint(GraphicsDevice graphicsDevice, Texture2D source, Color tint)
        {
            var pixels = new Color[source.Width * source.Height];
            source.GetData(pixels);

            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                pixels[i] = new Color(
                    (byte)(pixel.R * tint.R / 255),
                    (byte)(pixel.G * tint.G / 255),
                    (byte)(pixel.B * tint.B / 255),
                    pixel.A);
            }

            var tinted = new Texture2D(graphicsDevice, source.Width, source.Height);
            tinted.SetData(pixels);
            return tinted;
        }
    }
}
