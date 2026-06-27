using System;
using System.Collections.Generic;
using NUnit.Framework;
using WinCraft.Infrastructure;

namespace WinCraft.Tests.Infrastructure
{
    [TestFixture]
    internal sealed class WindowsVersionTests
    {
        [Test]
        public void GetVersion_XP_Returns_5_1()
        {
            var version = WindowsVersion.GetVersion(WindowsRelease.XP);

            Assert.That(version, Is.EqualTo(new Version(5, 1)));
        }

        [Test]
        public void GetVersion_Win7_Returns_6_1()
        {
            var version = WindowsVersion.GetVersion(WindowsRelease.Win7);

            Assert.That(version, Is.EqualTo(new Version(6, 1)));
        }

        [Test]
        public void GetVersion_Win8_1_Returns_6_3()
        {
            var version = WindowsVersion.GetVersion(WindowsRelease.Win8_1);

            Assert.That(version, Is.EqualTo(new Version(6, 3)));
        }

        [Test]
        public void GetVersion_Win10_1507_Returns_10_0_10240()
        {
            var version = WindowsVersion.GetVersion(WindowsRelease.Win10_1507);

            Assert.That(version, Is.EqualTo(new Version(10, 0, 10240)));
        }

        [Test]
        public void GetVersion_Win10_22H2_Returns_10_0_19045()
        {
            var version = WindowsVersion.GetVersion(WindowsRelease.Win10_22H2);

            Assert.That(version, Is.EqualTo(new Version(10, 0, 19045)));
        }

        [Test]
        public void GetVersion_Win11_21H2_Returns_10_0_22000()
        {
            var version = WindowsVersion.GetVersion(WindowsRelease.Win11_21H2);

            Assert.That(version, Is.EqualTo(new Version(10, 0, 22000)));
        }

        [Test]
        public void GetVersion_Win11_24H2_Returns_10_0_26100()
        {
            var version = WindowsVersion.GetVersion(WindowsRelease.Win11_24H2);

            Assert.That(version, Is.EqualTo(new Version(10, 0, 26100)));
        }

        [Test]
        public void GetVersion_Win11_26H1_Returns_10_0_28000()
        {
            var version = WindowsVersion.GetVersion(WindowsRelease.Win11_26H1);

            Assert.That(version, Is.EqualTo(new Version(10, 0, 28000)));
        }

        [Test]
        public void GetVersion_VersionsAreStrictlyIncreasing()
        {
            Version previous = null;
            var releases = new List<WindowsRelease>();
            foreach (WindowsRelease release in Enum.GetValues(typeof(WindowsRelease)))
            {
                if (release == WindowsRelease.Unknown)
                    continue;
                releases.Add(release);
            }

            foreach (var release in releases)
            {
                var current = WindowsVersion.GetVersion(release);
                if (previous != null)
                    Assert.That(current, Is.GreaterThan(previous),
                        $"{release} ({current}) should be greater than previous ({previous})");
                previous = current;
            }
        }

        [Test]
        public void GetVersion_Unknown_ThrowsInvalidOperationException()
        {
            Assert.That(
                () => WindowsVersion.GetVersion(WindowsRelease.Unknown),
                Throws.InvalidOperationException);
        }

        [Test]
        public void GetDisplayName_Unknown_ReturnsUnknown()
        {
            var name = WindowsVersion.GetDisplayName(WindowsRelease.Unknown);

            Assert.That(name, Is.EqualTo("Unknown"));
        }

        [Test]
        public void GetDisplayName_Win8_1_ReturnsWindows8_1()
        {
            var name = WindowsVersion.GetDisplayName(WindowsRelease.Win8_1);

            Assert.That(name, Is.EqualTo("Windows 8.1"));
        }

        [Test]
        public void GetDisplayName_Win11_24H2_Contains24H2()
        {
            var name = WindowsVersion.GetDisplayName(WindowsRelease.Win11_24H2);

            Assert.That(name, Does.Contain("24H2"));
        }

        [Test]
        public void GetDisplayName_StartsWithWindows()
        {
            foreach (WindowsRelease release in Enum.GetValues(typeof(WindowsRelease)))
            {
                if (release == WindowsRelease.Unknown)
                    continue;

                var name = WindowsVersion.GetDisplayName(release);
                Assert.That(name, Does.StartWith("Windows "),
                    $"Display name for {release} should start with 'Windows '");
            }
        }

        [Test]
        public void IsAtLeast_OlderRelease_ReturnsTrue()
        {
            Assert.That(WindowsVersion.IsAtLeast(WindowsRelease.XP), Is.True);
        }

        [Test]
        public void IsBelow_OlderRelease_ReturnsFalse()
        {
            Assert.That(WindowsVersion.IsBelow(WindowsRelease.XP), Is.False);
        }

        [Test]
        public void IsAtLeastAndIsBelow_AreConsistent()
        {
            Assert.That(
                WindowsVersion.IsAtLeast(WindowsRelease.XP) != WindowsVersion.IsBelow(WindowsRelease.XP),
                Is.True);
        }

        [Test]
        public void IsAtLeast_WithExplicitVersion_ReturnsTrueForVeryOldVersion()
        {
            Assert.That(WindowsVersion.IsAtLeast(5, 1), Is.True);
        }

        [Test]
        public void IsBelow_WithExplicitVersion_ReturnsFalseForVeryOldVersion()
        {
            Assert.That(WindowsVersion.IsBelow(5, 1), Is.False);
        }

        [Test]
        public void Current_IsNotNull()
        {
            Assert.That(WindowsVersion.Current, Is.Not.Null);
        }

        [Test]
        public void Current_MajorIsAtLeast5()
        {
            Assert.That(WindowsVersion.Current.Major, Is.AtLeast(5));
        }

        [Test]
        public void ServicePackMajor_IsNotNegative()
        {
            Assert.That(WindowsVersion.ServicePackMajor, Is.AtLeast(0));
        }

        [Test]
        public void GetCurrentRelease_DoesNotThrow()
        {
            Assert.That(() => WindowsVersion.GetCurrentRelease(), Throws.Nothing);
        }

        [Test]
        public void GetCurrentRelease_ReturnsKnownReleaseOnModernWindows()
        {
            var release = WindowsVersion.GetCurrentRelease();

            Assert.That(release, Is.Not.EqualTo(WindowsRelease.Unknown),
                "GetCurrentRelease should return a known release on a modern Windows system");
        }
    }
}
