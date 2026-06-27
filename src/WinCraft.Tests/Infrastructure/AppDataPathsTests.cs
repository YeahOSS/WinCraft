using System.IO;
using NUnit.Framework;
using WinCraft.Infrastructure;

namespace WinCraft.Tests.Infrastructure
{
    [TestFixture]
    internal sealed class AppDataPathsTests
    {
        [Test]
        public void Root_IsNotNullOrEmpty()
        {
            Assert.That(AppDataPaths.Root, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Root_ContainsWinCraft()
        {
            Assert.That(AppDataPaths.Root, Does.Contain("WinCraft"));
        }

        [Test]
        public void Logs_IsUnderRoot()
        {
            Assert.That(AppDataPaths.Logs, Does.StartWith(AppDataPaths.Root));
        }

        [Test]
        public void Dumps_IsUnderRoot()
        {
            Assert.That(AppDataPaths.Dumps, Does.StartWith(AppDataPaths.Root));
        }
    }
}
