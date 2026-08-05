using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.UI;
using Proto4X.Sandbox.Components;
using Proto4X.Sandbox.Systems;
using System;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Rendering
{
    /// <summary>
    /// Draws a ring under every selected unit, plus the drag rectangle while a box-select is in
    /// progress. Registered after the engine's <c>Renderer</c>, so it lands on top of the sprites.
    /// </summary>
    public class SelectionOverlayRenderer(Texture2D pixel, Camera camera, SelectionSystem selectionSystem) : ISystem, IDrawSystem
    {
        private static readonly Color SelectionColour = new(140, 255, 160);

        private static readonly ComponentRef[] requiredComponentProviders =
            [ComponentRef.Of<Position>(), ComponentRef.Of<Selectable>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        // World, for the per-entity rings. The drag rectangle drawn here is really screen-space and
        // wants splitting into its own DrawSpace.Screen system once the scheduler honours DrawSpace.
        public DrawSpace DrawSpace => DrawSpace.World;

        public void Draw(SpriteBatch spriteBatch, Rectangle cullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            foreach (var archetypeChunk in archetypeChunks)
            {
                var positionSpan = archetypeChunk.GetSpan<Position>();
                var selectableSpan = archetypeChunk.GetSpan<Selectable>();

                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    if (!selectableSpan[i].IsSelected)
                    {
                        continue;
                    }

                    Primitives.DrawCircleOutline(
                        spriteBatch,
                        pixel,
                        positionSpan[i].Location,
                        selectableSpan[i].PickRadius + 4f,
                        SelectionColour,
                        thickness: 1.5f);
                }
            }

            DrawDragRectangle(spriteBatch);
        }

        private void DrawDragRectangle(SpriteBatch spriteBatch)
        {
            if (selectionSystem.DragScreenRectangle is not { } dragRectangle)
            {
                return;
            }

            var (start, current) = dragRectangle;

            // The drag rectangle is defined in screen space, but everything here draws through the
            // camera transform, so each corner is converted back into the world individually. Under
            // a rotated camera that gives a correctly rotated quad rather than a wrong AABB.
            var screenToWorld = Matrix.Invert(camera.GetCameraTransform());
            Span<Vector2> corners =
            [
                Vector2.Transform(new Vector2(start.X, start.Y), screenToWorld),
                Vector2.Transform(new Vector2(current.X, start.Y), screenToWorld),
                Vector2.Transform(new Vector2(current.X, current.Y), screenToWorld),
                Vector2.Transform(new Vector2(start.X, current.Y), screenToWorld),
            ];

            Primitives.DrawRectangleOutline(spriteBatch, pixel, corners, SelectionColour);
        }
    }
}
