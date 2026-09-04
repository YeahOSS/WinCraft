using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using NUnit.Framework;
using WinCraft.Infrastructure.Shell;
using WinCraft.Infrastructure.Shell.Shortcuts;

namespace WinCraft.Tests.Shell.Shortcuts
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class ShellLinkTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "WinCraftTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (_tempDir != null && Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, recursive: true); }
                catch { /* best-effort cleanup */ }
            }
            _tempDir = null;
        }

        private string GetTempLnkPath()
        {
            return Path.Combine(_tempDir, "test.lnk");
        }

        // ── Dispose ───────────────────────────────────────────────

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var link = new ShellLink();
            link.Dispose();

            Assert.That(() => link.Dispose(), Throws.Nothing);
        }

        [Test]
        public void Dispose_CanBeCalledWithoutInit()
        {
            var link = new ShellLink();

            Assert.That(() => link.Dispose(), Throws.Nothing);
        }

        // ── Load ──────────────────────────────────────────────────

        [Test]
        public void Load_NonExistentFile_ReturnsFalse()
        {
            var link = new ShellLink();

            bool result = link.Load(@"C:\nonexistent\fake.lnk", writable: false);

            Assert.That(result, Is.False);
            link.Dispose();
        }

        // ── Save / SaveAs ─────────────────────────────────────────

        [Test]
        public void SaveAndLoad_RoundTripsAllProperties()
        {
            string targetPath = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\notepad.exe";
            string lnkPath = GetTempLnkPath();

            using (var link = new ShellLink())
            {
                link.TargetPath = targetPath;
                link.Arguments = "/test";
                link.WorkingDirectory = @"C:\Windows";
                link.Description = "Test description";
                link.WindowState = WindowState.Maximized;
                link.HotKey = (Key.F, ModifierKeys.Control);
                link.SaveAs(lnkPath);
            }

            using (var loaded = new ShellLink())
            {
                loaded.Load(lnkPath, writable: false);

                Assert.That(loaded.TargetPath, Is.EqualTo(targetPath).IgnoreCase);
                Assert.That(loaded.Arguments, Is.EqualTo("/test"));
                Assert.That(loaded.WorkingDirectory, Is.EqualTo(@"C:\Windows"));
                Assert.That(loaded.Description, Is.EqualTo("Test description"));
                Assert.That(loaded.WindowState, Is.EqualTo(WindowState.Maximized));
                Assert.That(loaded.HotKey, Is.EqualTo((Key.F, ModifierKeys.Control)));
            }
        }

        [Test]
        public void SaveAndLoad_RunAsAdminFlag()
        {
            string targetPath = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\notepad.exe";
            string lnkPath = GetTempLnkPath();

            using (var link = new ShellLink())
            {
                link.TargetPath = targetPath;
                link.RunAsAdmin = true;
                link.SaveAs(lnkPath);
            }

            using (var loaded = new ShellLink())
            {
                loaded.Load(lnkPath, writable: false);

                Assert.That(loaded.RunAsAdmin, Is.True);
            }
        }

        // ── SaveToBytes ────────────────────────────────────────────

        [Test]
        public void SaveToBytes_WithTargetPath_ReturnsNonEmptyArray()
        {
            using var link = new ShellLink();
            link.TargetPath = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\notepad.exe";

            byte[] result = link.SaveToBytes();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.GreaterThan(0));
        }

        [Test]
        public void SaveToBytes_EmptyLink_ReturnsNonEmptyArray()
        {
            using var link = new ShellLink();

            byte[] result = link.SaveToBytes();

            // Even an empty link serializes to a non-empty stream.
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.GreaterThan(0));
        }

    }
}
