using System.IO;
using System.Text;
using NUnit.Framework;
using WinCraft.Lzma;

namespace WinCraft.Tests.Lzma
{
    [TestFixture]
    internal sealed class LzmaCodecTests
    {
        [Test]
        public void CompressDecompress_RepeatedPayload_RoundTrips()
        {
            var input = Encoding.UTF8.GetBytes(new string('A', 4096) + "WinCraft-LZMA");

            var compressed = LzmaCodec.Compress(input);
            var decompressed = LzmaCodec.Decompress(compressed);

            CollectionAssert.AreEqual(input, decompressed);
        }

        [Test]
        public void CompressDecompress_EmptyPayload_RoundTrips()
        {
            var compressed = LzmaCodec.Compress([]);
            var decompressed = LzmaCodec.Decompress(compressed);

            Assert.That(LzmaCodec.GetDecompressedLength(compressed), Is.EqualTo(0));
            Assert.That(decompressed, Is.Empty);
        }

        [Test]
        public void CompressDecompress_StreamPayload_RoundTrips()
        {
            var input = Encoding.UTF8.GetBytes("alpha beta beta beta gamma");
            var compressed = new MemoryStream();

            LzmaCodec.Compress(new MemoryStream(input), compressed);
            compressed.Position = 0;

            var decompressed = new MemoryStream();
            LzmaCodec.Decompress(compressed, decompressed);

            CollectionAssert.AreEqual(input, decompressed.ToArray());
        }
    }
}
