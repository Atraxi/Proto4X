using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace Proto4X.Factory.Rendering.Software
{
    /// <summary>Minimal RGBA8 PNG encoder, so headless snapshots need no imaging library.</summary>
    public static class PngWriter
    {
        private static readonly uint[] crcTable = BuildCrcTable();

        /// <summary>Writes <paramref name="image"/> un-premultiplied, as PNG expects straight alpha.</summary>
        public static void Write(string path, CpuImage image)
        {
            var raw = new byte[(image.Width * 4 + 1) * image.Height];
            var o = 0;
            for (var y = 0; y < image.Height; y++)
            {
                raw[o++] = 0;
                for (var x = 0; x < image.Width; x++)
                {
                    ref var c = ref image.At(x, y);
                    var a = c.A;
                    raw[o++] = a == 0 ? (byte)0 : (byte)Math.Min(255, c.R * 255 / a);
                    raw[o++] = a == 0 ? (byte)0 : (byte)Math.Min(255, c.G * 255 / a);
                    raw[o++] = a == 0 ? (byte)0 : (byte)Math.Min(255, c.B * 255 / a);
                    raw[o++] = a;
                }
            }

            using var compressed = new MemoryStream();
            using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(raw);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            using var file = File.Create(path);
            file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

            Span<byte> header = stackalloc byte[13];
            BinaryPrimitives.WriteInt32BigEndian(header, image.Width);
            BinaryPrimitives.WriteInt32BigEndian(header[4..], image.Height);
            header[8] = 8;  // bit depth
            header[9] = 6;  // RGBA
            header[10] = 0; // deflate
            header[11] = 0; // adaptive filtering
            header[12] = 0; // no interlace
            WriteChunk(file, "IHDR", header);
            WriteChunk(file, "IDAT", compressed.ToArray());
            WriteChunk(file, "IEND", []);
        }

        private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(buffer, data.Length);
            stream.Write(buffer);

            Span<byte> typeBytes = [(byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3]];
            stream.Write(typeBytes);
            stream.Write(data);

            var crc = Crc(0xFFFFFFFFu, typeBytes);
            crc = Crc(crc, data) ^ 0xFFFFFFFFu;
            BinaryPrimitives.WriteUInt32BigEndian(buffer, crc);
            stream.Write(buffer);
        }

        private static uint Crc(uint crc, ReadOnlySpan<byte> data)
        {
            foreach (var b in data)
            {
                crc = crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }

            return crc;
        }

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                }

                table[n] = c;
            }

            return table;
        }
    }
}
