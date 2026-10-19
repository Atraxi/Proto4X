using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using Proto4X.Factory.Rendering.Software;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System;

namespace Proto4X.Factory.Rendering.Models
{
    /// <summary>The 2D projections a sprite style can bake for.</summary>
    public enum SpriteProjection
    {
        /// <summary>Straight down. Height only shows through lighting and shadow.</summary>
        TopDown,
        /// <summary>Ground stays square; height is drawn screen-up, so south faces show. The classic factory-game look.</summary>
        Oblique,
        /// <summary>2:1 diamond grid, viewed from the south-east corner.</summary>
        Isometric,
    }

    public static class Projections
    {
        /// <summary>Oblique: tiles of screen-up per tile of height.</summary>
        public const float ObliqueLift = 0.75f;

        /// <summary>Isometric: screen-up per unit height, relative to the half-width of a tile's diamond.</summary>
        public const float IsoHeight = 1.2f;

        private const float DepthScale = 1f / 16f;

        /// <summary>
        /// Tile space (x east, y south, z up) to sprite pixels (x right, y down) at
        /// <paramref name="pixelsPerTile"/>, with z out as depth (larger nearer the viewer). For
        /// isometric, <paramref name="pixelsPerTile"/> is the half-width of a tile's diamond.
        /// </summary>
        public static Matrix ToPixels(SpriteProjection projection, float pixelsPerTile)
        {
            var d = pixelsPerTile;
            var s = DepthScale;
            return projection switch
            {
                SpriteProjection.TopDown => new Matrix(
                    d, 0, 0, 0,
                    0, d, 0, 0,
                    0, 0, s, 0,
                    0, 0, 0, 1),
                // Viewer along (0, lift, 1): south and up.
                SpriteProjection.Oblique => new Matrix(
                    d, 0, 0, 0,
                    0, d, ObliqueLift * s, 0,
                    0, -ObliqueLift * d, s, 0,
                    0, 0, 0, 1),
                // Viewer along (1, 1, 1 / IsoHeight): south-east and up.
                _ => new Matrix(
                    d, d * 0.5f, s, 0,
                    -d, d * 0.5f, s, 0,
                    0, -IsoHeight * d, s / IsoHeight, 0,
                    0, 0, 0, 1),
            };
        }

        /// <summary>Flattens tile-space geometry onto the ground along the sun, for shadow bakes.</summary>
        public static Matrix SunShadow()
        {
            var sun = ModelMesher.Sun;
            return new Matrix(
                1, 0, 0, 0,
                0, 1, 0, 0,
                -sun.X / sun.Z, -sun.Y / sun.Z, 0, 0,
                0, 0, 0, 1);
        }
    }

    /// <summary>A sprite in an atlas, plus where its anchor (the footprint's origin corner on the ground) sits in it.</summary>
    public readonly record struct SpriteFrame(AtlasRegion Region, Vector2 Anchor, float PixelsPerTile)
    {
        public int Width => Region.PixelWidth;
        public int Height => Region.PixelHeight;
    }

    /// <summary>
    /// Pre-renders block models into sprites, the way sprite-based games make their art from 3D
    /// renders: one image per facing, lit, outlined, with a separate ground shadow. Everything a
    /// sprite style shows of a building comes out of here at startup.
    /// </summary>
    public static class SpriteBaker
    {
        private const int Margin = 3;
        private const int Supersample = 4;

        public static (CpuImage Image, Vector2 Anchor) Bake(BlockModel model, Direction facing, SpriteProjection projection, float pixelsPerTile, Color outline)
        {
            var place = BlockModels.Facing(facing, model.Size);
            var toPixels = Projections.ToPixels(projection, pixelsPerTile);
            var batch = new GeometryBatch();
            ModelMesher.EmitModel(batch, GeometryLayer.Opaque, model, place, Color.White);
            var (image, anchor) = Rasterize(batch, model, place, toPixels);
            AddOutline(image, outline);
            return (image, anchor);
        }

        /// <summary>The model flattened onto the ground along the sun, as a soft black mask.</summary>
        public static (CpuImage Image, Vector2 Anchor) BakeShadow(BlockModel model, Direction facing, SpriteProjection projection, float pixelsPerTile, float opacity)
        {
            var place = BlockModels.Facing(facing, model.Size) * Projections.SunShadow();
            var toPixels = Projections.ToPixels(projection, pixelsPerTile);
            var batch = new GeometryBatch();
            ModelMesher.EmitModel(batch, GeometryLayer.Opaque, model, place, Color.Black);
            var (image, anchor) = Rasterize(batch, model, place, toPixels);
            for (var i = 0; i < image.Pixels.Length; i++)
            {
                var alpha = image.Pixels[i].A / 255f * opacity;
                image.Pixels[i] = new Color(0, 0, 0, (int)(alpha * 255));
            }

            return (image, anchor);
        }

        private static (CpuImage, Vector2) Rasterize(GeometryBatch batch, BlockModel model, in Matrix place, in Matrix toPixels)
        {
            var min = new Vector2(float.MaxValue);
            var max = new Vector2(float.MinValue);
            foreach (var box in model.Boxes)
            {
                for (var corner = 0; corner < 8; corner++)
                {
                    var p = new Vector3(
                        (corner & 1) == 0 ? box.Min.X : box.Max.X,
                        (corner & 2) == 0 ? box.Min.Y : box.Max.Y,
                        (corner & 4) == 0 ? box.Min.Z : box.Max.Z);
                    var projected = Vector3.Transform(Vector3.Transform(p, place), toPixels);
                    min = Vector2.Min(min, new Vector2(projected.X, projected.Y));
                    max = Vector2.Max(max, new Vector2(projected.X, projected.Y));
                }
            }

            var width = (int)MathF.Ceiling(max.X - min.X) + 2 * Margin;
            var height = (int)MathF.Ceiling(max.Y - min.Y) + 2 * Margin;
            var offset = new Vector2(Margin - MathF.Floor(min.X), Margin - MathF.Floor(min.Y));
            var view = toPixels * Matrix.CreateTranslation(offset.X, offset.Y, 0);
            var projection = Matrix.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);

            var rasterizer = new SoftwareRasterizer(width * Supersample, height * Supersample);
            rasterizer.Clear(Color.Transparent);
            rasterizer.Draw(batch, Matrix.Identity, view, projection, texture: null, linear: false);
            var image = rasterizer.Resolve(Supersample);

            var origin = Vector3.Transform(Vector3.Zero, view);
            return (image, new Vector2(origin.X, origin.Y));
        }

        /// <summary>A one-pixel dark rim around the silhouette: what makes a render read as a sprite.</summary>
        private static void AddOutline(CpuImage image, Color outline)
        {
            var source = (Color[])image.Pixels.Clone();
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var index = y * image.Width + x;
                    if (source[index].A >= 200)
                    {
                        continue;
                    }

                    var neighbour = 0;
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            var nx = x + dx;
                            var ny = y + dy;
                            if ((uint)nx < (uint)image.Width && (uint)ny < (uint)image.Height)
                            {
                                neighbour = Math.Max(neighbour, source[ny * image.Width + nx].A);
                            }
                        }
                    }

                    if (neighbour >= 128)
                    {
                        // Existing (anti-aliased) pixel over the rim colour.
                        var pixel = source[index];
                        var under = outline * (neighbour / 255f);
                        var inverse = 1f - pixel.A / 255f;
                        image.Pixels[index] = new Color(
                            (int)(pixel.R + under.R * inverse),
                            (int)(pixel.G + under.G * inverse),
                            (int)(pixel.B + under.B * inverse),
                            (int)(pixel.A + under.A * inverse));
                    }
                }
            }
        }
    }

    /// <summary>Every building kind baked for one projection, packed into an atlas.</summary>
    public sealed class BuildingSprites
    {
        private readonly SpriteFrame[,] bodies = new SpriteFrame[Buildings.Count, 4];
        private readonly SpriteFrame[,] shadows = new SpriteFrame[Buildings.Count, 4];

        public BuildingSprites(AtlasBuilder atlas, SpriteProjection projection, float pixelsPerTile, Color outline, float shadowOpacity = 0.38f)
        {
            Projection = projection;
            for (var kind = BuildingKind.Inserter; kind <= BuildingKind.Collector; kind++)
            {
                var model = BlockModels.For(kind);
                for (var facing = 0; facing < 4; facing++)
                {
                    if (facing > 0 && BlockModels.IsSymmetric(kind))
                    {
                        bodies[(int)kind, facing] = bodies[(int)kind, 0];
                        shadows[(int)kind, facing] = shadows[(int)kind, 0];
                        continue;
                    }

                    var (body, bodyAnchor) = SpriteBaker.Bake(model, (Direction)facing, projection, pixelsPerTile, outline);
                    bodies[(int)kind, facing] = new SpriteFrame(atlas.Add(body), bodyAnchor, pixelsPerTile);
                    var (shadow, shadowAnchor) = SpriteBaker.BakeShadow(model, (Direction)facing, projection, pixelsPerTile, shadowOpacity);
                    shadows[(int)kind, facing] = new SpriteFrame(atlas.Add(shadow), shadowAnchor, pixelsPerTile);
                }
            }
        }

        public SpriteProjection Projection { get; }

        public SpriteFrame Body(BuildingKind kind, Direction facing) => bodies[(int)kind, (int)facing];

        public SpriteFrame Shadow(BuildingKind kind, Direction facing) => shadows[(int)kind, (int)facing];
    }

    /// <summary>
    /// Draws a sprite upright on screen, whatever the surface's transform: the quad is built from
    /// two local-space vectors that the full transform maps to one sprite pixel right and one down.
    /// This is how sprite styles survive a rotating ship - the sprite stays upright while the deck
    /// turns under it.
    /// </summary>
    public static class Billboards
    {
        public static void Draw(GeometryBatch batch, GeometryLayer layer, in SpriteFrame frame, Vector2 anchorLocal, Vector2 pixelRight, Vector2 pixelDown, float z, Color tint)
        {
            var topLeft = anchorLocal - pixelRight * frame.Anchor.X - pixelDown * frame.Anchor.Y;
            var right = pixelRight * frame.Width;
            var down = pixelDown * frame.Height;
            var region = frame.Region;
            batch.AddQuad(layer,
                Shapes.At(topLeft, z), Shapes.At(topLeft + right, z), Shapes.At(topLeft + right + down, z), Shapes.At(topLeft + down, z),
                tint, region.TopLeft, region.TopRight, region.BottomRight, region.BottomLeft);
        }
    }
}
