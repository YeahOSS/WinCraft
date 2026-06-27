using System;
using NUnit.Framework;
using WinCraft.Infrastructure.Ipc;

namespace WinCraft.Tests.Ipc
{
    [TestFixture]
    internal sealed class ElevatedAgentPipeServerTests
    {
        [Test]
        public void Create_ValidPipeName_ReturnsValidHandle()
        {
            var pipeName = "WinCraft.Test.Server." + Guid.NewGuid().ToString("N");

            using var handle = ElevatedAgentPipeServer.Create(pipeName);

            Assert.That(handle, Is.Not.Null);
            Assert.That(handle.IsInvalid, Is.False);
        }

        [Test]
        public void Create_ThenDispose_DoesNotThrow()
        {
            var pipeName = "WinCraft.Test.Server." + Guid.NewGuid().ToString("N");
            var handle = ElevatedAgentPipeServer.Create(pipeName);

            Assert.That(() => handle.Dispose(), Throws.Nothing);
        }

        [Test]
        public void Create_TwoPipesWithDifferentNames_Succeeds()
        {
            var name1 = "WinCraft.Test.Server." + Guid.NewGuid().ToString("N");
            var name2 = "WinCraft.Test.Server." + Guid.NewGuid().ToString("N");

            using var handle1 = ElevatedAgentPipeServer.Create(name1);
            using var handle2 = ElevatedAgentPipeServer.Create(name2);

            Assert.That(handle1.IsInvalid, Is.False);
            Assert.That(handle2.IsInvalid, Is.False);
        }
    }
}
