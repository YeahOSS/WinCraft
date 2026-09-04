using NUnit.Framework;
using WinCraft.Infrastructure.RegistryAccess;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Tests.Security
{
    [TestFixture]
    internal sealed class PrivilegedRegistryWriterTests
    {
        [Test]
        public void GetAttemptLevels_AutoLocalMachine_IncludesTrustedInstaller()
        {
            var levels = PrivilegedRegistryWriter.GetAttemptLevels(
                RegistryValueLocation.LocalMachine,
                RegistryPrivilegePolicy.Auto);

            Assert.That(levels, Is.EqualTo(new[]
            {
                PrivilegeLevel.Standard,
                PrivilegeLevel.Administrator,
                PrivilegeLevel.System,
                PrivilegeLevel.TrustedInstaller
            }));
        }

        [Test]
        public void GetAttemptLevels_AutoWithoutTI_StopsAtSystem()
        {
            var levels = PrivilegedRegistryWriter.GetAttemptLevels(
                RegistryValueLocation.LocalMachine,
                RegistryPrivilegePolicy.AutoWithoutTI);

            Assert.That(levels, Is.EqualTo(new[]
            {
                PrivilegeLevel.Standard,
                PrivilegeLevel.Administrator,
                PrivilegeLevel.System
            }));
        }

        [Test]
        public void GetAttemptLevels_CurrentUserAlwaysUsesCurrentOnly()
        {
            var levels = PrivilegedRegistryWriter.GetAttemptLevels(
                RegistryValueLocation.CurrentUser,
                RegistryPrivilegePolicy.Auto);

            Assert.That(levels, Is.EqualTo(new[] { PrivilegeLevel.Standard }));
        }

        [Test]
        public void ShouldTryNextPrivilege_AccessDenied_ReturnsTrue()
        {
            var result = PrivilegeExecutionResult.Failure(
                PrivilegeErrorCodes.RegistryAccessDenied,
                "denied");

            Assert.That(PrivilegedRegistryWriter.ShouldTryNextPrivilege(result), Is.True);
        }

        [Test]
        public void ShouldTryNextPrivilege_NonPermissionFailure_ReturnsFalse()
        {
            var result = PrivilegeExecutionResult.Failure(
                PrivilegeErrorCodes.RegistryWriteFailed,
                "invalid path");

            Assert.That(PrivilegedRegistryWriter.ShouldTryNextPrivilege(result), Is.False);
        }
    }
}
