using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Sandbox.Components;
using Proto4X.Sandbox.Game;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Rendering
{
    /// <summary>
    /// Shows what a station is building and how far along it is, without writing anything down: a
    /// radial progress ring around the station, a shrunken sprite of the ship itself in the middle
    /// of that ring, and one pip per item still queued behind it.
    /// </summary>
    /// <remarks>
    /// The sprite is the label. A player who has seen a freighter mine and a fighter shoot already
    /// knows what those two silhouettes mean, so the icon carries the same information the word
    /// "Miner" would, in less space and in every language.
    /// </remarks>
    public class BuildProgressRenderer(Texture2D pixel, SpriteSet sprites) : ISystem, IDrawSystem
    {
        private const float RingRadius = 46f;
        private const float IconScreenSize = 18f;
        private const float IconOffsetY = -62f;
        private const float PipSize = 4f;
        private const float PipSpacing = 7f;
        private const float PipOffsetY = 52f;

        /// <summary>Progress starts at 12 o'clock; sprite rotation is measured from +X.</summary>
        private const float RingStartAngle = -MathHelper.PiOver2;

        private static readonly Color TrackColour = new(40, 55, 75, 160);

        private static readonly ComponentRef[] requiredComponentProviders =
        [
            ComponentRef.Of<Position>(), ComponentRef.Of<Faction>(), ComponentRef.Of<Health>(),
            ComponentRef.Of<Timer>(), ComponentRef.Of<Station>(),
        ];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public DrawSpace DrawSpace => DrawSpace.World;

        public void Draw(SpriteBatch spriteBatch, Rectangle cullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            foreach (var archetypeChunk in archetypeChunks)
            {
                var positionSpan = archetypeChunk.GetSpan<Position>();
                var factionSpan = archetypeChunk.GetSpan<Faction>();
                var healthSpan = archetypeChunk.GetSpan<Health>();
                var timerSpan = archetypeChunk.GetSpan<Timer>();
                var stationSpan = archetypeChunk.GetSpan<Station>();

                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    var station = stationSpan[i];
                    if (station.InProgress == UnitKind.None || healthSpan[i].Current <= 0f)
                    {
                        continue;
                    }

                    var faction = factionSpan[i].Id;
                    var location = positionSpan[i].Location;
                    var colour = SpriteSet.FactionColour(faction);

                    Primitives.DrawCircleOutline(spriteBatch, pixel, location, RingRadius, TrackColour, thickness: 3f);
                    Primitives.DrawArc(
                        spriteBatch,
                        pixel,
                        location,
                        RingRadius,
                        RingStartAngle,
                        MathHelper.TwoPi * ProgressOf(timerSpan[i]),
                        colour,
                        thickness: 3f);

                    DrawShipIcon(spriteBatch, station.InProgress, faction, location + new Vector2(0f, IconOffsetY));
                    DrawQueuePips(spriteBatch, station, colour, location + new Vector2(0f, PipOffsetY));
                }
            }
        }

        private static float ProgressOf(Timer timer)
        {
            if (timer.DurationTicks <= 0)
            {
                return 1f;
            }

            var elapsedTicks = timer.DurationTicks - timer.RemainingTicks;
            return MathHelper.Clamp((float)elapsedTicks / timer.DurationTicks, 0f, 1f);
        }

        private void DrawShipIcon(SpriteBatch spriteBatch, UnitKind kind, FactionId faction, Vector2 centre)
        {
            var texture = SpriteLibrary.GetTexture(ShipBlueprints.SpriteIdFor(kind, faction, sprites));
            var scale = IconScreenSize / texture.Width;

            spriteBatch.Draw(
                texture,
                centre,
                null,
                Color.White,
                0f,
                new Vector2(texture.Width / 2f, texture.Height / 2f),
                scale,
                SpriteEffects.None,
                0f);
        }

        /// <summary>One pip per item still waiting, so a deep queue reads as a longer row.</summary>
        private void DrawQueuePips(SpriteBatch spriteBatch, Station station, Color colour, Vector2 centre)
        {
            var queued = station.QueuedMiners + station.QueuedFighters;
            if (queued <= 0)
            {
                return;
            }

            var rowWidth = (queued - 1) * PipSpacing;
            for (var pip = 0; pip < queued; pip++)
            {
                var topLeft = centre + new Vector2(pip * PipSpacing - rowWidth / 2f - PipSize / 2f, 0f);
                Primitives.FillRectangle(spriteBatch, pixel, topLeft, new Vector2(PipSize), colour);
            }
        }
    }
}
