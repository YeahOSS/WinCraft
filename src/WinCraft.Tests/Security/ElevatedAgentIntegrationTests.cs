using System;
using NUnit.Framework;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Tests.Security
{
    [TestFixture]
    internal sealed class ElevatedAgentControllerTests
    {
        [Test]
        public void State_AfterConstruction_IsIdle()
        {
            using var controller = new ElevatedAgentController();

            Assert.That(controller.State, Is.EqualTo(AgentConnectionState.Idle));
        }

        [Test]
        public void Dispose_CanBeCalledWithoutExecute()
        {
            var controller = new ElevatedAgentController();

            Assert.That(() => controller.Dispose(), Throws.Nothing);
        }

        [Test]
        public void DisposedController_Execute_ThrowsObjectDisposedException()
        {
            var controller = new ElevatedAgentController();
            controller.Dispose();

            var request = new ElevatedCommandRequest
            {
                OperationName = ElevatedOperations.Ping,
                RequestId = "req-disposed"
            };

            Assert.That(
                () => controller.Execute(request),
                Throws.InstanceOf<ObjectDisposedException>());
        }
    }

    [TestFixture]
    [Explicit("Requires Administrator privileges and UAC consent")]
    internal sealed class ElevatedAgentPingTests
    {
        [OneTimeSetUp]
        public void CheckPreconditions()
        {
            if (!ProcessElevation.IsCurrentProcessElevated())
                Assert.Ignore("Test requires elevated process. Run the test executable as Administrator.");
        }

        [Test]
        public void Ping_ReturnsSuccess()
        {
            using var controller = new ElevatedAgentController();

            var request = new ElevatedCommandRequest
            {
                OperationName = ElevatedOperations.Ping,
                PrivilegeLevel = PrivilegeLevel.Administrator,
                RequestId = Guid.NewGuid().ToString("N")
            };

            var result = controller.Execute(request);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestId, Is.EqualTo(request.RequestId));
        }
    }
}
