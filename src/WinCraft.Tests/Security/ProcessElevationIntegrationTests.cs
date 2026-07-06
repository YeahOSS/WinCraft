using NUnit.Framework;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Tests.Security
{
    [TestFixture]
    internal sealed class ProcessElevationIntegrationTests
    {
        [Test]
        public void GetCurrentProcessId_MatchesDotNetProcessId()
        {
            var pid = ProcessElevation.GetCurrentProcessId();
            var expected = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;

            Assert.That(pid, Is.EqualTo(expected));
        }
    }
}
