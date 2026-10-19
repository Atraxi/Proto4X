using Microsoft.Xna.Framework;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Rendering.Software;
using Proto4X.Factory.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Renders the game's own draw systems to PNG without a window: the scene runs headless, each
    /// shot's camera is aimed, the scheduler collects the geometry pass, and the software rasterizer
    /// draws it. No HUD (it's SpriteBatch), otherwise what the GPU would get.
    /// </summary>
    public static class Snapshot
    {
        /// <summary>
        /// Every building baked in each sprite projection, written as the raw atlases over grey - a
        /// contact sheet of the art the sprite styles draw from.
        /// </summary>
        public static void WriteSpriteSheets(string directory)
        {
            Directory.CreateDirectory(directory);
            foreach (var projection in (Models.SpriteProjection[])[Models.SpriteProjection.TopDown, Models.SpriteProjection.Oblique, Models.SpriteProjection.Isometric])
            {
                var atlas = new AtlasBuilder(2048);
                var density = projection == Models.SpriteProjection.Isometric ? 48f : 64f;
                _ = new Models.BuildingSprites(atlas, projection, density, new Color(20, 18, 16));
                var sheet = new CpuImage(atlas.Image.Width, atlas.Image.Height);
                sheet.Fill(new Color(96, 96, 100));
                for (var i = 0; i < sheet.Pixels.Length; i++)
                {
                    var pixel = atlas.Image.Pixels[i];
                    var under = sheet.Pixels[i];
                    var inverse = 255 - pixel.A;
                    sheet.Pixels[i] = new Color(pixel.R + under.R * inverse / 255, pixel.G + under.G * inverse / 255, pixel.B + under.B * inverse / 255, 255);
                }

                PngWriter.Write(Path.Combine(directory, $"sprites-{projection}.png"), sheet);
            }
        }

        /// <summary>One picture: which surface, where, how close, and how far the grid is turned from upright.</summary>
        public readonly record struct Shot(string Name, int Surface, Vector2 LocalTile, float Zoom, float ScreenRotation, bool WithStressBlock = false, string Preset = "");

        public static readonly Shot[] Shots =
        [
            new("factory", 0, new Vector2(15f, 8.5f), 1.25f, 0f),
            new("closeup", 0, new Vector2(10f, 3.5f), 3.5f, 0f),
            new("ship", 1, new Vector2(11.5f, 8f), 1.9f, 0.55f),
            new("overview", 0, new Vector2(118f, 40f), 0.2f, 0f, WithStressBlock: true, Preset: "top"),
            new("low-angle", 0, new Vector2(13f, 6f), 1.6f, 0f, Preset: "low"),
            new("iso-ortho", 0, new Vector2(15f, 8.5f), 1.25f, 0f, Preset: "iso"),
        ];

        /// <param name="supersample">1 matches the game, which has no MSAA and no mipmaps; higher flatters every style, sprites most.</param>
        public static void CaptureAll(string directory, int width = 1280, int height = 720, int supersample = 1, int ticks = 900)
        {
            Directory.CreateDirectory(directory);
            var style = new RenderStyle();
            var scheduler = new SystemScheduler();
            var universe = FactoryScene.Create(scheduler, out var planet, out _);
            foreach (var surface in universe.Surfaces)
            {
                style.RegisterSurface(scheduler, surface, controller: null);
            }

            for (var t = 0; t < ticks; t++)
            {
                universe.Step();
            }

            var stressStamped = false;
            var camera = style.CreateCamera();
            camera.Resize(new Rectangle(0, 0, width, height));
            var rasterizer = new SoftwareRasterizer(width * supersample, height * supersample);
            var report = new List<string>();

            foreach (var shot in Shots)
            {
                if (shot.WithStressBlock && !stressStamped)
                {
                    FactoryScene.StampStressBlock(planet, 0);
                    for (var t = 0; t < 600; t++)
                    {
                        universe.Step();
                    }

                    stressStamped = true;
                }

                var surface = universe.Surfaces[shot.Surface];
                style.AimCamera(camera, surface, shot.LocalTile, shot.Zoom, shot.ScreenRotation);
                style.AimSpecial(camera, shot.Preset);
                style.PrepareFrame(camera, surface);

                var world = style.WorldTransform(surface);
                var bounds = camera.GetVisibleLocalBounds(world);
                var timer = Stopwatch.StartNew();
                scheduler.Geometry.Clear();
                scheduler.CollectGeometry(surface.World, scheduler.Geometry, bounds);
                var collectMs = timer.Elapsed.TotalMilliseconds;

                // The projection maps the camera's window pixels to clip space, so a rasterizer
                // with more pixels than the window simply supersamples.
                var view = camera.GetViewMatrix();
                var projection = camera.GetProjectionMatrix();
                rasterizer.Clear(style.Background);
                rasterizer.Draw(scheduler.Geometry, world, view, projection, style.Atlas.Image, style.LinearSampling);
                var image = rasterizer.Resolve(supersample);
                var path = Path.Combine(directory, $"{style.Name}-{shot.Name}.png");
                PngWriter.Write(path, image);

                var line = $"{shot.Name,-9} {scheduler.Geometry.TriangleCount,8:N0} triangles  collect {collectMs,6:F2} ms  -> {path}";
                report.Add(line);
                Console.WriteLine(line);
            }

            File.WriteAllLines(Path.Combine(directory, $"{style.Name}-report.txt"), report);
        }
    }
}
