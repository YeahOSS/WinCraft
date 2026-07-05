using NUnit.Framework;
using WinCraft.Infrastructure.RegistryAccess;
using WinCraft.Infrastructure.Shell;
using Windows.Win32.UI.Shell.Common;

namespace WinCraft.Tests.Shell
{
    [TestFixture]
    internal sealed class PerceivedTypeMapperTests
    {
        [Test]
        public void TryMap_Image_ReturnsImageName()
        {
            var found = PerceivedTypeMapper.TryMap(PERCEIVED.PERCEIVED_TYPE_IMAGE, out string name);

            Assert.That(found, Is.True);
            Assert.That(name, Is.EqualTo("Image"));
        }

        [Test]
        public void TryMap_Text_ReturnsTextName()
        {
            var found = PerceivedTypeMapper.TryMap(PERCEIVED.PERCEIVED_TYPE_TEXT, out string name);

            Assert.That(found, Is.True);
            Assert.That(name, Is.EqualTo("Text"));
        }

        [Test]
        public void TryMap_Audio_ReturnsAudioName()
        {
            var found = PerceivedTypeMapper.TryMap(PERCEIVED.PERCEIVED_TYPE_AUDIO, out string name);

            Assert.That(found, Is.True);
            Assert.That(name, Is.EqualTo("Audio"));
        }

        [Test]
        public void TryMap_Video_ReturnsVideoName()
        {
            var found = PerceivedTypeMapper.TryMap(PERCEIVED.PERCEIVED_TYPE_VIDEO, out string name);

            Assert.That(found, Is.True);
            Assert.That(name, Is.EqualTo("Video"));
        }

        [Test]
        public void TryMap_Compressed_ReturnsCompressedName()
        {
            var found = PerceivedTypeMapper.TryMap(PERCEIVED.PERCEIVED_TYPE_COMPRESSED, out string name);

            Assert.That(found, Is.True);
            Assert.That(name, Is.EqualTo("Compressed"));
        }

        [Test]
        public void TryMap_Document_ReturnsDocumentName()
        {
            var found = PerceivedTypeMapper.TryMap(PERCEIVED.PERCEIVED_TYPE_DOCUMENT, out string name);

            Assert.That(found, Is.True);
            Assert.That(name, Is.EqualTo("Document"));
        }

        [Test]
        public void TryMap_System_ReturnsSystemName()
        {
            var found = PerceivedTypeMapper.TryMap(PERCEIVED.PERCEIVED_TYPE_SYSTEM, out string name);

            Assert.That(found, Is.True);
            Assert.That(name, Is.EqualTo("System"));
        }

        [Test]
        public void TryMap_Application_ReturnsApplicationName()
        {
            var found = PerceivedTypeMapper.TryMap(PERCEIVED.PERCEIVED_TYPE_APPLICATION, out string name);

            Assert.That(found, Is.True);
            Assert.That(name, Is.EqualTo("Application"));
        }

        [Test]
        public void TryMap_GameMedia_ReturnsGameMediaName()
        {
            var found = PerceivedTypeMapper.TryMap(PERCEIVED.PERCEIVED_TYPE_GAMEMEDIA, out string name);

            Assert.That(found, Is.True);
            Assert.That(name, Is.EqualTo("GameMedia"));
        }

        [Test]
        public void TryMap_Contacts_ReturnsContactsName()
        {
            var found = PerceivedTypeMapper.TryMap(PERCEIVED.PERCEIVED_TYPE_CONTACTS, out string name);

            Assert.That(found, Is.True);
            Assert.That(name, Is.EqualTo("Contacts"));
        }

        [Test]
        public void TryMap_Folder_ReturnsFalse()
        {
            // PERCEIVED_TYPE_FOLDER is not in the Entries table — no known SystemFileAssociations mapping.
            var found = PerceivedTypeMapper.TryMap(PERCEIVED.PERCEIVED_TYPE_FOLDER, out string name);

            Assert.That(found, Is.False);
            Assert.That(name, Is.Null);
        }

        [Test]
        public void TryMap_Unknown_ReturnsFalse()
        {
            var found = PerceivedTypeMapper.TryMap(PERCEIVED.PERCEIVED_TYPE_UNKNOWN, out string name);

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

        [Test]
        public void FromClassesRootSystemFileAssociations_EmptyName_BuildsPrefixOnly()
        {
            var path = PerceivedTypeMapper.FromClassesRootSystemFileAssociations(string.Empty);

            Assert.That(path.SubKeyPath, Is.EqualTo(@"SystemFileAssociations\"));
        }

    }
}
