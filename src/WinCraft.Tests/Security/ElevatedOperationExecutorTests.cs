using NUnit.Framework;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Tests.Security
{
    [TestFixture]
    internal sealed class ElevatedOperationExecutorTests
    {
        [Test]
        public void Execute_AdministratorPing_ReturnsSuccess()
        {
            var request = new ElevatedCommandRequest
            {
                OperationName = ElevatedOperations.Ping,
                PrivilegeLevel = PrivilegeLevel.Administrator,
                RequestId = "req-3"
            };

            var result = ElevatedOperationExecutor.Execute(request);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestId, Is.EqualTo("req-3"));
        }

        [Test]
        public void Execute_AdministratorUnknownOperation_ReturnsUnsupportedOperation()
        {
            var request = new ElevatedCommandRequest
            {
                OperationName = "unknown.op",
                PrivilegeLevel = PrivilegeLevel.Administrator,
                RequestId = "req-4"
            };

            var result = ElevatedOperationExecutor.Execute(request);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.UnsupportedOperation));
        }

        [Test]
        public void ExecuteLocal_RegistryWriteWithInvalidPayload_ReturnsFailure()
        {
            var request = new ElevatedCommandRequest
            {
                OperationName = ElevatedOperations.RegistryWrite,
                Payload = "not-valid-xml",
                RequestId = "req-rw"
            };

            var result = ElevatedOperationExecutor.ExecuteLocal(request);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.RegistryWriteFailed));
            Assert.That(result.RequestId, Is.EqualTo("req-rw"));
        }

        [Test]
        public void ExecuteLocal_RegistryDeleteWithInvalidPayload_ReturnsFailure()
        {
            var request = new ElevatedCommandRequest
            {
                OperationName = ElevatedOperations.RegistryDelete,
                Payload = "not-valid-xml",
                RequestId = "req-rd"
            };

            var result = ElevatedOperationExecutor.ExecuteLocal(request);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.RegistryDeleteFailed));
            Assert.That(result.RequestId, Is.EqualTo("req-rd"));
        }
    }
}
