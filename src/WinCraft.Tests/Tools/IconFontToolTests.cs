using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media;
using NUnit.Framework;
using WinCraft.IconFontTool;

namespace WinCraft.Tests.Tools
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    internal sealed class IconFontToolTests
    {
        [TestCase("ic_fluent_settings_24_regular", "Settings24")]
        [TestCase("ic_fluent_3d_rotation_20_filled", "Icon3dRotation20")]
        public void ConvertToEnumName_FluentMetadataName_ReturnsCSharpName(string source, string expected)
        {
            Assert.That(IconFontPipeline.ConvertToEnumName(source), Is.EqualTo(expected));
        }

        [Test]
        public void FindUsedNames_CSharpAndXamlReferences_ReturnsKnownIcons()
        {
            var root = Path.Combine(Path.GetTempPath(), "WinCraft.FontTool.Tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                File.WriteAllText(
                    Path.Combine(root, "Icons.cs"),
                    "var icon = IconGlyph.Warning24; var none = IconGlyph.None;");
                File.WriteAllText(
                    Path.Combine(root, "View.xaml"),
                    "<Root><ui:IconBlock Icon=\"Settings24\" /><Setter Property=\"ui:Design.Icon\" Value=\"Home24\" /><Other Icon=\"Unknown24\" /></Root>");
                var excludedPath = Path.Combine(root, "Generated.cs");
                File.WriteAllText(excludedPath, "var generated = IconGlyph.Search24;");

                var result = IconFontPipeline.FindUsedNames(
                    root,
                    excludedPath,
                    new HashSet<string>(new[] { "Settings24", "Warning24", "Home24", "Search24" }, StringComparer.Ordinal));

                CollectionAssert.AreEqual(new[] { "Home24", "Settings24", "Warning24" }, result);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void SubsetFont_FluentIcons_ProducesLoadableDeterministicTrueTypeFont()
        {
            var repositoryRoot = FindRepositoryRoot();
            var inputPath = Path.Combine(repositoryRoot, "assets", "iconfont", "FluentSystemIcons-Regular.ttf");
            var input = File.ReadAllBytes(inputPath);
            var first = TrueTypeSubsetter.SubsetFont(input, new[] { 63146, 63594, 984972 });
            var second = TrueTypeSubsetter.SubsetFont(input, new[] { 984972, 63594, 63146 });

            CollectionAssert.AreEqual(first, second);
            Assert.That(first.Length, Is.LessThan(input.Length));
            Assert.That(ReadHeadMagic(first), Is.EqualTo(0x5F0F3CF5u));
            Assert.That(CalculateChecksum(first), Is.EqualTo(0xB1B0AFBAu));

            var outputPath = Path.Combine(Path.GetTempPath(), "WinCraft.FontTool." + Guid.NewGuid().ToString("N") + ".ttf");
            try
            {
                File.WriteAllBytes(outputPath, first);
                var glyphTypeface = new GlyphTypeface(new Uri(outputPath));
                Assert.That(glyphTypeface.CharacterToGlyphMap.ContainsKey(63146), Is.True);
                Assert.That(glyphTypeface.CharacterToGlyphMap.ContainsKey(63594), Is.True);
                Assert.That(glyphTypeface.CharacterToGlyphMap.ContainsKey(984972), Is.True);
                Assert.That(glyphTypeface.CharacterToGlyphMap.Count, Is.EqualTo(3));
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }

        [Test]
        public void SubsetFont_MissingCodePoint_ReportsCodePoint()
        {
            var repositoryRoot = FindRepositoryRoot();
            var input = File.ReadAllBytes(Path.Combine(
                repositoryRoot,
                "assets",
                "iconfont",
                "FluentSystemIcons-Regular.ttf"));

            var exception = Assert.Throws<InvalidOperationException>(
                () => TrueTypeSubsetter.SubsetFont(input, new[] { 0x10FFFF }));

            Assert.That(exception.Message, Does.Contain("U+10FFFF"));
        }

        [Test]
        public void Generate_UsedMode_TrimsEnumToCompleteSourceClosure()
        {
            var repositoryRoot = FindRepositoryRoot();
            var root = Path.Combine(Path.GetTempPath(), "WinCraft.FontTool.Tests." + Guid.NewGuid().ToString("N"));
            var hostRoot = Path.Combine(Path.GetTempPath(), "WinCraft.FontTool.Host.Tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(hostRoot);
            try
            {
                File.WriteAllText(Path.Combine(root, "Icons.cs"), "var icon = IconGlyph.Settings24;");
                File.WriteAllText(Path.Combine(hostRoot, "HostIcons.cs"), "var hostIcon = IconGlyph.Search24;");
                var enumOutput = Path.Combine(root, "IconGlyph.g.cs");
                var mapOutput = Path.Combine(root, "IconGlyphMap.g.cs");
                var options = CommandLineOptions.Parse(new[]
                {
                    "--regular-metadata", Path.Combine(repositoryRoot, "assets", "iconfont", "FluentSystemIcons-Regular.json"),
                    "--filled-metadata", Path.Combine(repositoryRoot, "assets", "iconfont", "FluentSystemIcons-Filled.json"),
                    "--mode", "used",
                    "--source-roots", root + ";" + hostRoot,
                    "--enum-output", enumOutput,
                    "--map-output", mapOutput,
                    "--regular-font", Path.Combine(repositoryRoot, "assets", "iconfont", "FluentSystemIcons-Regular.ttf"),
                    "--regular-font-output", Path.Combine(root, "regular.ttf"),
                    "--filled-font", Path.Combine(repositoryRoot, "assets", "iconfont", "FluentSystemIcons-Filled.ttf"),
                    "--filled-font-output", Path.Combine(root, "filled.ttf"),
                }, 0);

                IconFontPipeline.Run(options);

                var enumSource = File.ReadAllText(enumOutput);
                Assert.That(enumSource, Does.Contain("Settings24 = 63146"));
                Assert.That(enumSource, Does.Contain("Search24 = 63120"));
                Assert.That(enumSource, Does.Not.Contain("ColorBackgroundAccent20"));
                Assert.That(File.ReadAllText(mapOutput), Does.Not.Contain("{ 58301,"));
            }
            finally
            {
                Directory.Delete(root, true);
                Directory.Delete(hostRoot, true);
            }
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "assets", "iconfont", "FluentSystemIcons-Regular.ttf")))
                    return directory.FullName;
                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the repository root.");
        }

        private static uint ReadHeadMagic(byte[] font)
        {
            var tableCount = ReadU16(font, 4);
            for (var index = 0; index < tableCount; index++)
            {
                var recordOffset = 12 + index * 16;
                if (Encoding.ASCII.GetString(font, recordOffset, 4) != "head")
                    continue;
                var tableOffset = checked((int)ReadU32(font, recordOffset + 8));
                return ReadU32(font, tableOffset + 12);
            }

            throw new InvalidDataException("The subset font has no head table.");
        }

        private static uint CalculateChecksum(byte[] font)
        {
            uint sum = 0;
            for (var offset = 0; offset < font.Length; offset += 4)
                sum += ReadU32(font, offset);
            return sum;
        }

        private static ushort ReadU16(byte[] data, int offset) =>
            (ushort)((data[offset] << 8) | data[offset + 1]);

        private static uint ReadU32(byte[] data, int offset) =>
            ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) |
            ((uint)data[offset + 2] << 8) | data[offset + 3];
    }
}
