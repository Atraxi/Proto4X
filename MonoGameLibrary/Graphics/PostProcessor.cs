using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MonoGameLibrary.Graphics
{
    /// <summary>
    /// Draws one screen-space quad through a pixel shader: backgrounds, post-processing, transitions.
    /// </summary>
    /// <remarks>
    /// Rides on <see cref="SpriteBatch"/> for the quad, with the source texture in sampler register 0.
    /// The effect should bring its own vertex shader taking SpriteBatch's vertex
    /// <c>(POSITION0, COLOR0, TEXCOORD0)</c> through <c>MatrixTransform</c>: a pixel-only effect relying
    /// on SpriteBatch's vertex shader got a constant uv on DesktopGL (a flat-colour frame).
    /// Sets <c>MatrixTransform</c> (pixels to clip space), <c>Resolution</c> (float2, pixels) and
    /// <c>Time</c> when the effect declares them.
    /// </remarks>
    public sealed class FullscreenPass(GraphicsDevice device) : IDisposable
    {
        private Texture2D? white;

        /// <summary>Draws <paramref name="source"/> (or white, for generators like a starfield) over <paramref name="destination"/> through <paramref name="effect"/>.</summary>
        public void Draw(SpriteBatch spriteBatch, Effect? effect, Rectangle destination, Texture2D? source = null, float time = 0f, BlendState? blend = null)
        {
            if (source is null && white is null)
            {
                white = new Texture2D(device, 1, 1);
                white.SetData([Color.White]);
            }

            var viewport = device.Viewport;
            // SpriteBatch's own projection: pixel coordinates, y down.
            effect?.Parameters["MatrixTransform"]?.SetValue(Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, -1));
            effect?.Parameters["Resolution"]?.SetValue(new Vector2(destination.Width, destination.Height));
            effect?.Parameters["Time"]?.SetValue(time);
            spriteBatch.Begin(SpriteSortMode.Immediate, blend ?? BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, effect);
            spriteBatch.Draw(source ?? white!, destination, Color.White);
            spriteBatch.End();
        }

        public void Dispose() => white?.Dispose();
    }

    /// <summary>
    /// Redirects drawing into an off-screen scene target, then draws that target to the backbuffer
    /// through an effect. Whatever draws after <see cref="End"/> (a HUD) is not processed.
    /// </summary>
    public sealed class PostProcessor(GraphicsDevice device) : IDisposable
    {
        private readonly FullscreenPass pass = new(device);
        private RenderTarget2D? scene;

        /// <summary>The scene target, sized to the backbuffer and with a depth buffer, so the geometry pass depth-tests inside it.</summary>
        public void Begin()
        {
            var parameters = device.PresentationParameters;
            if (scene is null || scene.Width != parameters.BackBufferWidth || scene.Height != parameters.BackBufferHeight)
            {
                scene?.Dispose();
                scene = new RenderTarget2D(device, parameters.BackBufferWidth, parameters.BackBufferHeight, false,
                    parameters.BackBufferFormat, DepthFormat.Depth24Stencil8);
            }

            device.SetRenderTarget(scene);
        }

        /// <summary>Back to the backbuffer, then the scene through <paramref name="effect"/> (null draws it unprocessed).</summary>
        public void End(SpriteBatch spriteBatch, Effect? effect, float time = 0f)
        {
            device.SetRenderTarget(null);
            if (scene is not null)
            {
                pass.Draw(spriteBatch, effect, new Rectangle(0, 0, scene.Width, scene.Height), scene, time);
            }
        }

        public void Dispose()
        {
            scene?.Dispose();
            pass.Dispose();
        }
    }
}
