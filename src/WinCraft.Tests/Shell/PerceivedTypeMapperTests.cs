using NUnit.Framework;
using WinCraft.Infrastructure.RegistryAccess;
using WinCraft.Infrastructure.Shell;
using Windows.Win32.UI.Shell.Common;

namespace WinCraft.Tests.Shell
{
    [TestFixture]
    internal sealed class PerceivedTypeMapperTests
    {
        [TestCase(PERCEIVED.PERCEIVED_TYPE_TEXT, "Text")]
        [TestCase(PERCEIVED.PERCEIVED_TYPE_IMAGE, "Image")]
        [TestCase(PERCEIVED.PERCEIVED_TYPE_AUDIO, "Audio")]
        [TestCase(PERCEIVED.PERCEIVED_TYPE_VIDEO, "Video")]
        [TestCase(PERCEIVED.PERCEIVED_TYPE_COMPRESSED, "Compressed")]
        [TestCase(PERCEIVED.PERCEIVED_TYPE_DOCUMENT, "Document")]
        [TestCase(PERCEIVED.PERCEIVED_TYPE_SYSTEM, "System")]
        [TestCase(PERCEIVED.PERCEIVED_TYPE_APPLICATION, "Application")]
        [TestCase(PERCEIVED.PERCEIVED_TYPE_GAMEMEDIA, "GameMedia")]
        [TestCase(PERCEIVED.PERCEIVED_TYPE_CONTACTS, "Contacts")]
        public void TryMap_KnownPerceivedType_ReturnsRegistryName(PERCEIVED perceived, string expected)
        {
            var found = PerceivedTypeMapper.TryMap(perceived, out string name);

            Assert.That(found, Is.True);
            Assert.That(name, Is.EqualTo(expected));
        }

        [TestCase(PERCEIVED.PERCEIVED_TYPE_FOLDER)]
        [TestCase(PERCEIVED.PERCEIVED_TYPE_UNKNOWN)]
        public void TryMap_UnmappedPerceivedType_ReturnsFalse(PERCEIVED perceived)
        {
            var found = PerceivedTypeMapper.TryMap(perceived, out string name);

            Assert.That(found, Is.False);
            Assert.That(name, Is.Null);
        }

        [Test]
        public void FromClassesRootSystemFileAssociations_BuildsCorrectPath()
        {
            var path = PerceivedTypeMapper.FromClassesRootSystemFileAssociations("Image");

            Assert.That(path.Location, Is.EqualTo(RegistryValueLocation.ClassesRoot));
            Assert.That(path.SubKeyPath, Is.EqualTo(@"SystemFileAssociations\Image"));
        }
    }
}
