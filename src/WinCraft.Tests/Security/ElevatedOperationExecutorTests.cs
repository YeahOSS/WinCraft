using NUnit.Framework;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Tests.Security
{
    [TestFixture]
    internal sealed class ElevatedOperationExecutorTests
    {
        [Test]
        public void Execute_NullRequest_ReturnsInvalidRequest()
        {
            var result = ElevatedOperationExecutor.Execute(null);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.InvalidRequest));
        }

        [Test]
        public void Execute_EmptyOperationName_ReturnsInvalidRequest()
        {
            var request = new ElevatedCommandRequest
            {
                OperationName = string.Empty,
                RequestId = "req-1"
            };

            var result = ElevatedOperationExecutor.Execute(request);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.InvalidRequest));
            Assert.That(result.RequestId, Is.EqualTo("req-1"));
        }

        [Test]
        public void Execute_StandardPrivilegeLevel_ReturnsPrivilegeLevelRequired()
        {
            var request = new ElevatedCommandRequest
            {
                OperationName = "some.operation",
                PrivilegeLevel = PrivilegeLevel.Standard,
                RequestId = "req-2"
            };

            var result = ElevatedOperationExecutor.Execute(request);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.PrivilegeLevelRequired));
        }

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
        public void Execute_SystemPrivilegeLevel_UsesSystemExecutor()
        {
            var request = new ElevatedCommandRequest
            {
                OperationName = ElevatedOperations.Ping,
                PrivilegeLevel = PrivilegeLevel.System,
                RequestId = "req-system"
            };

            var result = ElevatedOperationExecutor.Execute(
                request,
                systemRequest => CommandResult.Success(systemRequest.RequestId),
                trustedInstallerRequest => CommandResult.Failure(
                    "wrong_route",
                    "TrustedInstaller should not be used.",
                    trustedInstallerRequest.RequestId));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestId, Is.EqualTo("req-system"));
        }

        [Test]
        public void ExecuteLocal_NullRequest_ReturnsInvalidRequest()
        {
            var result = ElevatedOperationExecutor.ExecuteLocal(null);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.InvalidRequest));
        }

        [Test]
        public void ExecuteLocal_Ping_ReturnsSuccess()
        {
            var request = new ElevatedCommandRequest
            {
                OperationName = ElevatedOperations.Ping,
                RequestId = "req-ping"
            };

            var result = ElevatedOperationExecutor.ExecuteLocal(request);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestId, Is.EqualTo("req-ping"));
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

        [Test]
        public void ExecuteLocal_UnknownOperation_ReturnsUnsupportedOperation()
        {
            var request = new ElevatedCommandRequest
            {
                OperationName = "unknown.op",
                RequestId = "req-unk"
            };

            var result = ElevatedOperationExecutor.ExecuteLocal(request);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.UnsupportedOperation));
        }
    }
}
