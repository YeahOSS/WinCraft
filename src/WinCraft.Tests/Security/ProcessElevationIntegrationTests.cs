using NUnit.Framework;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Tests.Security
{
    [TestFixture]
    internal sealed class ProcessElevationIntegrationTests
    {
        [Test]
        public void IsCurrentProcessElevated_DoesNotThrow()
        {
            Assert.That(() => ProcessElevation.IsCurrentProcessElevated(), Throws.Nothing);
        }

        [Test]
        public void IsCurrentProcessElevated_ReturnsBoolean()
        {
            var result = ProcessElevation.IsCurrentProcessElevated();

            Assert.That(result, Is.True.Or.False);
        }

        [Test]
        public void GetCurrentProcessId_ReturnsNonZero()
        {
            var pid = ProcessElevation.GetCurrentProcessId();

            Assert.That(pid, Is.GreaterThan(0u));
        }

        [Test]
        public void GetCurrentProcessId_MatchesDotNetProcessId()
        {
            var pid = ProcessElevation.GetCurrentProcessId();
            var expected = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;

            Assert.That(pid, Is.EqualTo(expected));
        }
    }
}
