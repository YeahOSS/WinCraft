using NUnit.Framework;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Tests.Security
{
    [TestFixture]
    internal sealed class PrivilegeBrokerTests
    {
        [Test]
        public void MapResult_SuccessCommand_ReturnsSuccess()
        {
            var commandResult = CommandResult.Success("req-1");

            var result = PrivilegeBroker.MapResult(commandResult);

            Assert.That(result.Status, Is.EqualTo(PrivilegeExecutionStatus.Succeeded));
            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void MapResult_NullCommand_ReturnsFailureWithEmptyAgentResponse()
        {
            var result = PrivilegeBroker.MapResult(null);

            Assert.That(result.Status, Is.EqualTo(PrivilegeExecutionStatus.Failed));
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.EmptyAgentResponse));
        }

        [Test]
        public void MapResult_AgentStartCancelled_ReturnsCancelled()
        {
            var commandResult = CommandResult.Failure(
                PrivilegeErrorCodes.AgentStartCancelled, "UAC dismissed");

            var result = PrivilegeBroker.MapResult(commandResult);

            Assert.That(result.Status, Is.EqualTo(PrivilegeExecutionStatus.Cancelled));
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.AgentStartCancelled));
        }

        [Test]
        public void MapResult_AgentUnavailable_ReturnsUnavailable()
        {
            var commandResult = CommandResult.Failure(
                PrivilegeErrorCodes.AgentUnavailable, "pipe error");

            var result = PrivilegeBroker.MapResult(commandResult);

            Assert.That(result.Status, Is.EqualTo(PrivilegeExecutionStatus.Unavailable));
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.AgentUnavailable));
        }

        [Test]
        public void MapResult_GenericFailure_ReturnsFailed()
        {
            var commandResult = CommandResult.Failure("some_other_code", "details");

            var result = PrivilegeBroker.MapResult(commandResult);

            Assert.That(result.Status, Is.EqualTo(PrivilegeExecutionStatus.Failed));
            Assert.That(result.ErrorCode, Is.EqualTo("some_other_code"));
            Assert.That(result.ErrorMessage, Is.EqualTo("details"));
        }
    }
}
