using System;
using System.IO;
using NUnit.Framework;
using WinCraft.Infrastructure.FileSystem;

namespace WinCraft.Tests.Infrastructure.FileSystem
{
    [TestFixture]
    internal sealed class DesktopIniFileTests
    {
        private string _folderPath;

        [SetUp]
        public void SetUp()
        {
            _folderPath = Path.Combine(
                Path.GetTempPath(),
                "WinCraft_DesktopIniTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folderPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folderPath))
            {
                File.SetAttributes(_folderPath, FileAttributes.Normal);
                Directory.Delete(_folderPath, recursive: true);
            }
        }

        [Test]
        public void Icon_ReadWrite_RoundTrips()
        {
            var desktopIni = new DesktopIniFile(_folderPath);
            var icon = new WinCraft.Infrastructure.Shell.IconLocation(
                Path.Combine(_folderPath, "folder.ico"), 2);

            desktopIni.Icon = icon;

            var readBack = desktopIni.Icon;
            Assert.That(readBack, Is.Not.Null);
            Assert.That(readBack.Value.Index, Is.EqualTo(2));
        }

        [Test]
        public void Icon_SetNull_DoesNotThrow()
        {
            var desktopIni = new DesktopIniFile(_folderPath);

            Assert.That(() => { desktopIni.Icon = null; }, Throws.Nothing);
        }

        [Test]
        public void Icon_Get_WhenNotSet_ReturnsNull()
        {
            var desktopIni = new DesktopIniFile(_folderPath);

            var icon = desktopIni.Icon;

            Assert.That(icon, Is.Null);
        }

        [Test]
        public void InfoTip_ReadWrite_RoundTrips()
        {
            var desktopIni = new DesktopIniFile(_folderPath);

            desktopIni.InfoTip = "Custom tip text";

            var readBack = desktopIni.InfoTip;
            Assert.That(readBack, Is.EqualTo("Custom tip text"));
        }

        [Test]
        public void InfoTip_SetNull_WritesEmptyString()
        {
            var desktopIni = new DesktopIniFile(_folderPath);

            Assert.That(() => { desktopIni.InfoTip = null; }, Throws.Nothing);
        }

        [Test]
        public void InfoTip_Get_WhenNotSet_ReturnsNull()
        {
            var desktopIni = new DesktopIniFile(_folderPath);

            var tip = desktopIni.InfoTip;

            Assert.That(tip, Is.Null);
        }

        [Test]
        public void Logo_ReadWrite_RoundTrips()
        {
            var desktopIni = new DesktopIniFile(_folderPath);
            string logoPath = Path.Combine(_folderPath, "logo.bmp");

            desktopIni.Logo = logoPath;

            var readBack = desktopIni.Logo;
            Assert.That(readBack, Is.EqualTo(logoPath));
        }

        [Test]
        public void Logo_SetNull_DoesNotThrow()
        {
            var desktopIni = new DesktopIniFile(_folderPath);

            Assert.That(() => { desktopIni.Logo = null; }, Throws.Nothing);
        }

        [Test]
        public void Logo_Get_WhenNotSet_ReturnsNull()
        {
            var desktopIni = new DesktopIniFile(_folderPath);

            var logo = desktopIni.Logo;

            Assert.That(logo, Is.Null);
        }

        [Test]
        public void RefreshFolder_CreatesHiddenSystemDesktopIni()
        {
            DesktopIniFile.RefreshFolder(_folderPath);

            string iniPath = Path.Combine(_folderPath, DesktopIniFile.FileName);
            Assert.That(File.Exists(iniPath), Is.True);
            var attrs = File.GetAttributes(iniPath);
            Assert.That((attrs & FileAttributes.Hidden), Is.EqualTo(FileAttributes.Hidden));
            Assert.That((attrs & FileAttributes.System), Is.EqualTo(FileAttributes.System));
        }

        [Test]
        public void RefreshFolder_NullPath_DoesNotThrow()
        {
            Assert.That(() => { DesktopIniFile.RefreshFolder(null); }, Throws.Nothing);
        }

        [Test]
        public void RefreshFolder_NonExistentPath_DoesNotThrow()
        {
            Assert.That(
                () => { DesktopIniFile.RefreshFolder(@"Z:\DoesNotExist\Folder"); },
                Throws.Nothing);
        }

        [Test]
        public void GetContent_EmptyFolder_ReturnsEmptyString()
        {
            var desktopIni = new DesktopIniFile(_folderPath);

            var content = desktopIni.GetContent();

            Assert.That(content, Is.Empty);
        }

        [Test]
        public void LocalizedNames_SetAndGet_RoundTrips()
        {
            var desktopIni = new DesktopIniFile(_folderPath);

            desktopIni.LocalizedNames["sample.txt"] = "Localized Sample";
            var readBack = desktopIni.LocalizedNames["sample.txt"];

            Assert.That(readBack, Is.EqualTo("Localized Sample"));
        }

        [Test]
        public void LocalizedNames_Remove_RemovesEntry()
        {
            var desktopIni = new DesktopIniFile(_folderPath);
            desktopIni.LocalizedNames["sample.txt"] = "Localized Sample";

            var removed = desktopIni.LocalizedNames.Remove("sample.txt");
            var readBack = desktopIni.LocalizedNames["sample.txt"];

            Assert.That(removed, Is.True);
            Assert.That(readBack, Is.Null);
        }

        [Test]
        public void LocalizedNames_Remove_MissingKey_ReturnsFalse()
        {
            var desktopIni = new DesktopIniFile(_folderPath);

            var removed = desktopIni.LocalizedNames.Remove("nonexistent.txt");

            Assert.That(removed, Is.False);
        }

        [Test]
        public void FileName_IsDesktopIni()
        {
            Assert.That(DesktopIniFile.FileName, Is.EqualTo("desktop.ini"));
        }
    }
}
