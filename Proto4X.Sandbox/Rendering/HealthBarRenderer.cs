using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Sandbox.Components;
using Proto4X.Sandbox.Game;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Rendering
{
    /// <summary>
    /// A faction-coloured health bar above anything that has taken damage. Undamaged entities draw
    /// nothing, so a healthy base doesn't look like a UI mockup.
    /// </summary>
    public class HealthBarRenderer() : ISystem, IGeometryDrawSystem
    {
        private const float BarWidth = 30f;
        private const float BarHeight = 3f;
        private const float BarOffsetY = -26f;

        private static readonly ComponentRef[] requiredComponentProviders =
            [ComponentRef.Of<Position>(), ComponentRef.Of<Health>(), ComponentRef.Of<Faction>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;
        public void Draw(GeometryBatch batch, in GeometryDrawContext context, List<ArchetypeChunk> archetypeChunks)
        {
            foreach (var archetypeChunk in archetypeChunks)
            {
                var positionSpan = archetypeChunk.GetSpan<Position>();
                var healthSpan = archetypeChunk.GetSpan<Health>();
                var factionSpan = archetypeChunk.GetSpan<Faction>();

                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    var health = healthSpan[i];
                    if (health.Current >= health.Maximum || health.Current <= 0f)
                    {
                        continue;
                    }

                    var fraction = MathHelper.Clamp(health.Current / health.Maximum, 0f, 1f);
                    var topLeft = positionSpan[i].Location + new Vector2(-BarWidth / 2f, BarOffsetY);

                    GroundShapes.FillRectangle(batch.Immediate, topLeft, new Vector2(BarWidth, BarHeight), new Color(30, 30, 30));
                    GroundShapes.FillRectangle(batch.Immediate, topLeft, new Vector2(BarWidth * fraction, BarHeight), ModelSet.FactionColour(factionSpan[i].Id));
                }
            }
        }
    }
}
