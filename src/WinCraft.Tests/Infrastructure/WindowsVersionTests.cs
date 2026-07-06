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
        public void ReleaseComparison_WithOlderRelease_ReturnsExpectedClassification()
        {
            Assert.That(WindowsVersion.IsAtLeast(WindowsRelease.XP), Is.True);
            Assert.That(WindowsVersion.IsBelow(WindowsRelease.XP), Is.False);
        }

        [Test]
        public void ExplicitVersionComparison_WithVeryOldVersion_ReturnsExpectedClassification()
        {
            Assert.That(WindowsVersion.IsAtLeast(5, 1), Is.True);
            Assert.That(WindowsVersion.IsBelow(5, 1), Is.False);
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
