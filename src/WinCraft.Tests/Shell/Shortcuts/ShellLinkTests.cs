using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using NUnit.Framework;
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

        // ── Construction ──────────────────────────────────────────

        [Test]
        public void Construct_Default_DoesNotThrow()
        {
            Assert.That(() => new ShellLink(), Throws.Nothing);
        }

        [Test]
        public void Construct_WithNonExistentPath_DoesNotThrow()
        {
            Assert.That(() => new ShellLink(GetTempLnkPath()), Throws.Nothing);
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

        // ── TargetPath ────────────────────────────────────────────

        [Test]
        public void TargetPath_Default_ReturnsEmpty()
        {
            using var link = new ShellLink();

            var result = link.TargetPath;

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void TargetPath_SetAndGet_RoundTrips()
        {
            using var link = new ShellLink();
            const string expected = @"C:\Windows\System32\notepad.exe";

            link.TargetPath = expected;

            Assert.That(link.TargetPath, Is.EqualTo(expected));
        }

        // ── Arguments ─────────────────────────────────────────────

        [Test]
        public void Arguments_Default_ReturnsEmpty()
        {
            using var link = new ShellLink();

            var result = link.Arguments;

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Arguments_SetAndGet_RoundTrips()
        {
            using var link = new ShellLink();

            link.Arguments = "/a /b /c";

            Assert.That(link.Arguments, Is.EqualTo("/a /b /c"));
        }

        // ── WorkingDirectory ──────────────────────────────────────

        [Test]
        public void WorkingDirectory_Default_ReturnsEmpty()
        {
            using var link = new ShellLink();

            var result = link.WorkingDirectory;

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void WorkingDirectory_SetAndGet_RoundTrips()
        {
            using var link = new ShellLink();

            link.WorkingDirectory = @"C:\Windows";

            Assert.That(link.WorkingDirectory, Is.EqualTo(@"C:\Windows"));
        }

        // ── Description ───────────────────────────────────────────

        [Test]
        public void Description_Default_ReturnsEmpty()
        {
            using var link = new ShellLink();

            var result = link.Description;

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Description_SetAndGet_RoundTrips()
        {
            using var link = new ShellLink();

            link.Description = "Test shortcut";

            Assert.That(link.Description, Is.EqualTo("Test shortcut"));
        }

        // ── WindowState ───────────────────────────────────────────

        [Test]
        public void WindowState_Default_IsNormal()
        {
            using var link = new ShellLink();

            var style = link.WindowState;

            Assert.That(style, Is.EqualTo(WindowState.Normal));
        }

        [Test]
        public void WindowState_Maximized_RoundTrips()
        {
            using var link = new ShellLink();

            link.WindowState = WindowState.Maximized;

            Assert.That(link.WindowState, Is.EqualTo(WindowState.Maximized));
        }

        [Test]
        public void WindowState_Minimized_RoundTrips()
        {
            using var link = new ShellLink();

            link.WindowState = WindowState.Minimized;

            Assert.That(link.WindowState, Is.EqualTo(WindowState.Minimized));
        }

        // ── HotKey ────────────────────────────────────────────────

        [Test]
        public void HotKey_Default_IsNone()
        {
            using var link = new ShellLink();

            var (key, modifiers) = link.HotKey;

            Assert.That(key, Is.EqualTo(Key.None));
            Assert.That(modifiers, Is.EqualTo(ModifierKeys.None));
        }

        [Test]
        public void HotKey_SetAndGet_RoundTrips()
        {
            using var link = new ShellLink();

            link.HotKey = (Key.F, ModifierKeys.Control);

            var (key, modifiers) = link.HotKey;
            Assert.That(key, Is.EqualTo(Key.F));
            Assert.That(modifiers, Is.EqualTo(ModifierKeys.Control));
        }

        [Test]
        public void HotKey_SetShift_RoundTrips()
        {
            using var link = new ShellLink();

            link.HotKey = (Key.F, ModifierKeys.Shift);

            var (key, modifiers) = link.HotKey;
            Assert.That(key, Is.EqualTo(Key.F));
            Assert.That(modifiers, Is.EqualTo(ModifierKeys.Shift));
        }

        [Test]
        public void HotKey_SetAlt_RoundTrips()
        {
            using var link = new ShellLink();

            link.HotKey = (Key.F, ModifierKeys.Alt);

            var (key, modifiers) = link.HotKey;
            Assert.That(key, Is.EqualTo(Key.F));
            Assert.That(modifiers, Is.EqualTo(ModifierKeys.Alt));
        }

        [Test]
        public void HotKey_SetShiftControl_RoundTrips()
        {
            using var link = new ShellLink();

            link.HotKey = (Key.F, ModifierKeys.Shift | ModifierKeys.Control);

            var (key, modifiers) = link.HotKey;
            Assert.That(key, Is.EqualTo(Key.F));
            Assert.That(modifiers, Is.EqualTo(ModifierKeys.Shift | ModifierKeys.Control));
        }

        // ── RunAsAdmin ────────────────────────────────────────────

        [Test]
        public void RunAsAdmin_Default_IsFalse()
        {
            using var link = new ShellLink();

            Assert.That(link.RunAsAdmin, Is.False);
        }

        [Test]
        public void RunAsAdmin_SetTrue_ReadsBack()
        {
            using var link = new ShellLink();

            link.RunAsAdmin = true;

            Assert.That(link.RunAsAdmin, Is.True);
        }

        [Test]
        public void RunAsAdmin_SetFalseAfterTrue_ReadsBack()
        {
            using var link = new ShellLink();
            link.RunAsAdmin = true;

            link.RunAsAdmin = false;

            Assert.That(link.RunAsAdmin, Is.False);
        }

        // ── IconLocation ──────────────────────────────────────────

        [Test]
        public void IconLocation_Default_IsEmpty()
        {
            using var link = new ShellLink();

            var (file, index) = link.IconLocation;

            Assert.That(file, Is.EqualTo(string.Empty));
            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void IconLocation_SetAndGet_RoundTrips()
        {
            using var link = new ShellLink();

            link.IconLocation = (@"C:\Windows\System32\shell32.dll", 42);

            var (file, index) = link.IconLocation;
            Assert.That(file, Is.EqualTo(@"C:\Windows\System32\shell32.dll"));
            Assert.That(index, Is.EqualTo(42));
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
        public void SaveAs_CreatesLnkFile()
        {
            using var link = new ShellLink();
            link.TargetPath = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\notepad.exe";
            string lnkPath = GetTempLnkPath();

            Assert.That(() => link.SaveAs(lnkPath), Throws.Nothing);
            Assert.That(File.Exists(lnkPath));
        }


        [Test]
        public void SaveAndLoad_RoundTripsTargetPath()
        {
            string targetPath = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\notepad.exe";
            string lnkPath = GetTempLnkPath();

            using (var link = new ShellLink())
            {
                link.TargetPath = targetPath;
                link.SaveAs(lnkPath);
            }

            using (var loaded = new ShellLink())
            {
                bool loadResult = loaded.Load(lnkPath, writable: false);
                Assert.That(loadResult, Is.True);
                Assert.That(loaded.TargetPath, Is.EqualTo(targetPath).IgnoreCase);
            }
        }

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

        // ── TryComputeHash ─────────────────────────────────────────

        [Test]
        public void TryComputeHash_WithTargetPath_ReturnsNonZero()
        {
            using var link = new ShellLink();
            link.TargetPath = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\notepad.exe";

            bool result = WinxHash.TryComputeHash(link.TargetPath, link.Arguments, out uint hash);

            Assert.That(result, Is.True);
            Assert.That(hash, Is.Not.EqualTo(0));
        }

        [Test]
        public void TryComputeHash_WithoutTargetPath_ReturnsNonZero()
        {
            using var link = new ShellLink();

            bool result = WinxHash.TryComputeHash(link.TargetPath, link.Arguments, out uint hash);

            // Hash is computed from the salt string even without a target path,
            // so the result is non-zero.
            Assert.That(result, Is.True);
            Assert.That(hash, Is.Not.EqualTo(0));
        }

        // ── TryReadFromLink ────────────────────────────────────────

        [Test]
        public void TryReadFromLink_NewLink_ReturnsFalse()
        {
            using var link = new ShellLink();

            bool result = WinxHash.TryReadFromLink(link, out uint hash);

            Assert.That(result, Is.False);
            Assert.That(hash, Is.EqualTo(0));
        }

        [Test]
        public void TryReadFromLink_SavedLinkWithoutHash_ReturnsFalse()
        {
            string lnkPath = GetTempLnkPath();
            using (var link = new ShellLink())
            {
                link.TargetPath = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\notepad.exe";
                link.SaveAs(lnkPath);
            }

            using (var loaded = new ShellLink())
            {
                loaded.Load(lnkPath, writable: false);

                bool result = WinxHash.TryReadFromLink(loaded, out uint hash);

                Assert.That(result, Is.False);
                Assert.That(hash, Is.EqualTo(0));
            }
        }

    }
}
