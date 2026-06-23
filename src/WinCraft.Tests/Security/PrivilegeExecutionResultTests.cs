using NUnit.Framework;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Tests.Security
{
    [TestFixture]
    internal sealed class PrivilegeExecutionResultTests
    {
        [Test]
        public void Success_SetsStatusToSucceeded()
        {
            var result = PrivilegeExecutionResult.Success();

            Assert.That(result.Status, Is.EqualTo(PrivilegeExecutionStatus.Succeeded));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ErrorCode, Is.Null);
            Assert.That(result.ErrorMessage, Is.Null);
        }

        [Test]
        public void Cancelled_SetsFieldsCorrectly()
        {
            var result = PrivilegeExecutionResult.Cancelled("code", "cancelled by user");

            Assert.That(result.Status, Is.EqualTo(PrivilegeExecutionStatus.Cancelled));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("code"));
            Assert.That(result.ErrorMessage, Is.EqualTo("cancelled by user"));
        }

        [Test]
        public void Unavailable_SetsFieldsCorrectly()
        {
            var result = PrivilegeExecutionResult.Unavailable("code", "not available");

            Assert.That(result.Status, Is.EqualTo(PrivilegeExecutionStatus.Unavailable));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("code"));
            Assert.That(result.ErrorMessage, Is.EqualTo("not available"));
        }

        [Test]
        public void Failure_SetsFieldsCorrectly()
        {
            var result = PrivilegeExecutionResult.Failure("code", "failed");

            Assert.That(result.Status, Is.EqualTo(PrivilegeExecutionStatus.Failed));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("code"));
            Assert.That(result.ErrorMessage, Is.EqualTo("failed"));
        }
    }
}
