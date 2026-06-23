using NUnit.Framework;
using WinCraft.Infrastructure.Ipc;

namespace WinCraft.Tests.Ipc
{
    [TestFixture]
    internal sealed class CommandResultTests
    {
        [Test]
        public void Success_WithoutRequestId_SetsSucceededTrue()
        {
            var result = CommandResult.Success();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestId, Is.Null);
            Assert.That(result.ErrorCode, Is.Null);
            Assert.That(result.ErrorMessage, Is.Null);
        }

        [Test]
        public void Success_WithRequestId_SetsRequestId()
        {
            var result = CommandResult.Success("req-001");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestId, Is.EqualTo("req-001"));
        }

        [Test]
        public void Failure_WithoutRequestId_SetsAllFields()
        {
            var result = CommandResult.Failure("err_code", "Something went wrong");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("err_code"));
            Assert.That(result.ErrorMessage, Is.EqualTo("Something went wrong"));
            Assert.That(result.RequestId, Is.Null);
        }

        [Test]
        public void Failure_WithRequestId_IncludesRequestId()
        {
            var result = CommandResult.Failure("err_code", "msg", "req-002");

            Assert.That(result.RequestId, Is.EqualTo("req-002"));
        }

        [Test]
        public void Default_RequestId_IsNull()
        {
            var result = CommandResult.Success();

            Assert.That(result.RequestId, Is.Null);
        }
    }
}
