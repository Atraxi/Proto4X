using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.UI;
using System;
using System.Diagnostics;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// The game's three compiled shaders (Content/Shaders) and the engine passes that draw them.
    /// See plans/02-shaders.md.
    /// </summary>
    /// <remarks>
    /// Every effect is optional. If one fails to load (missing content, an MGFX version mismatch),
    /// its feature falls back to the stock path - BasicEffect ghosts, a plain clear colour, an
    /// ungraded frame - and the reason goes to the debug output, rather than the game failing to
    /// start over a cosmetic.
    /// </remarks>
    public sealed class Shaders : IDisposable
    {
        private readonly Effect? starfield;
        private readonly Effect? grade;
        private readonly FullscreenPass fullscreen;
        private readonly PostProcessor post;

        public Shaders(ContentManager content, GraphicsDevice device)
        {
            var hologram = TryLoad(content, "Shaders/Hologram");
            Hologram = hologram is null ? null : new Material(hologram, BlendState.AlphaBlend);
            starfield = TryLoad(content, "Shaders/Starfield");
            grade = TryLoad(content, "Shaders/Grade");
            fullscreen = new FullscreenPass(device);
            post = new PostProcessor(device);
        }

        /// <summary>Unlit, blended, scanlined: build ghosts and blueprint previews. Null when it failed to load.</summary>
        public Material? Hologram { get; }

        /// <summary>F4: draw the world through the grade shader.</summary>
        public bool GradeEnabled { get; set; } = true;

        private bool Grading => GradeEnabled && grade is not null;

        /// <summary>Starts the frame: into the post target if grading, then the background.</summary>
        public void BeginFrame(SpriteBatch spriteBatch, GraphicsDevice device, bool space, Color clear, OrbitCamera camera, float time)
        {
            if (Grading)
            {
                post.Begin();
            }

            device.Clear(clear);
            if (space && starfield is not null)
            {
                // Ground position in "pixels" for parallax: the layers scale it again.
                var target = camera.Target;
                starfield.Parameters["CameraOffset"]?.SetValue(new Vector2(target.X, target.Y) * 14f);
                fullscreen.Draw(spriteBatch, starfield, device.Viewport.Bounds, null, time);
            }
        }

        /// <summary>Ends the world: grades it onto the backbuffer. The HUD draws after this, ungraded.</summary>
        public void EndFrame(SpriteBatch spriteBatch, float time)
        {
            if (Grading)
            {
                post.End(spriteBatch, grade, time);
            }
        }

        public void Dispose()
        {
            fullscreen.Dispose();
            post.Dispose();
        }

        private static Effect? TryLoad(ContentManager content, string name)
        {
            try
            {
                return content.Load<Effect>(name);
            }
            catch (Exception exception) when (exception is ContentLoadException or InvalidOperationException or NotSupportedException)
            {
                Debug.WriteLine($"Shader {name} unavailable, using the fallback path: {exception.Message}");
                return null;
            }
        }
    }
}
