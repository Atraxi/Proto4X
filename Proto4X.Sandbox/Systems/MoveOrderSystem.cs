using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Input;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.UI;
using Proto4X.Sandbox.Components;
using System;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Systems
{
    /// <summary>
    /// Right click sends every selected unit to the clicked point, fanned out around it so a group
    /// doesn't pile onto one pixel. There's no collision or flocking - the ring is what stops a
    /// selection of twelve ships looking like one ship.
    /// </summary>
    public class MoveOrderSystem(Camera camera) : ISystem, IUpdateSystem
    {
        private const float RingSpacing = 34f;

        private static readonly ComponentRef[] requiredComponentProviders =
            [ComponentRef.Of<Position>(), ComponentRef.Of<Motion>(), ComponentRef.Of<MoveOrder>(), ComponentRef.Of<Selectable>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var mouse = InputManager.Instance.MouseInfo;
            if (!mouse.WasButtonJustPressed(MouseButton.Right))
            {
                return;
            }

            var destination = Vector2.Transform(mouse.Position.ToVector2(), Matrix.Invert(camera.GetCameraTransform()));
            var orderedCount = 0;

            foreach (var archetypeChunk in archetypeChunks)
            {
                var moveOrderSpan = archetypeChunk.GetSpan<MoveOrder>();
                var selectableSpan = archetypeChunk.GetSpan<Selectable>();

                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    if (!selectableSpan[i].IsSelected)
                    {
                        continue;
                    }

                    moveOrderSpan[i].Destination = destination + FormationOffset(orderedCount);
                    orderedCount++;
                }
            }
        }

        /// <summary>Spreads units over concentric rings: one in the middle, then six, then twelve.</summary>
        private static Vector2 FormationOffset(int slot)
        {
            if (slot == 0)
            {
                return Vector2.Zero;
            }

            var ring = 1;
            var slotsBefore = 1;
            while (slotsBefore + ring * 6 <= slot)
            {
                slotsBefore += ring * 6;
                ring++;
            }

            var indexInRing = slot - slotsBefore;
            var angle = MathHelper.TwoPi * indexInRing / (ring * 6);
            return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (ring * RingSpacing);
        }
    }
}
