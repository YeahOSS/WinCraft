using System.Collections.Generic;
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

        [Test]
        public void WriteString_DefaultAuto_StopsAtFirstSuccessfulPrivilege()
        {
            var attempts = new List<PrivilegeLevel>();
            var writer = new PrivilegedRegistryWriter(null, (request, operationName, privilegeLevel) =>
            {
                attempts.Add(privilegeLevel);
                return privilegeLevel == PrivilegeLevel.System
                    ? PrivilegeExecutionResult.Success()
                    : PrivilegeExecutionResult.Failure(PrivilegeErrorCodes.RegistryAccessDenied, "denied");
            });

            var result = writer.WriteString(
                RegistryValueLocation.LocalMachine,
                @"SOFTWARE\Test",
                "Value",
                "data");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.EffectivePrivilegeLevel, Is.EqualTo(PrivilegeLevel.System));
            Assert.That(result.AttemptedPrivilegeLevels, Is.EqualTo(new[]
            {
                PrivilegeLevel.Standard,
                PrivilegeLevel.Administrator,
                PrivilegeLevel.System
            }));
            Assert.That(attempts, Is.EqualTo(result.AttemptedPrivilegeLevels));
        }

        [Test]
        public void WriteString_NonPermissionFailure_DoesNotEscalate()
        {
            var attempts = new List<PrivilegeLevel>();
            var writer = new PrivilegedRegistryWriter(null, (request, operationName, privilegeLevel) =>
            {
                attempts.Add(privilegeLevel);
                return PrivilegeExecutionResult.Failure(
                    PrivilegeErrorCodes.RegistryWriteFailed,
                    "invalid path");
            });

            var result = writer.WriteString(
                RegistryValueLocation.LocalMachine,
                @"SOFTWARE\Test",
                "Value",
                "data");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.RegistryWriteFailed));
            Assert.That(result.EffectivePrivilegeLevel, Is.Null);
            Assert.That(attempts, Is.EqualTo(new[] { PrivilegeLevel.Standard }));
        }

        [Test]
        public void WriteString_AutoWithoutTI_DoesNotAttemptTrustedInstaller()
        {
            var attempts = new List<PrivilegeLevel>();
            var writer = new PrivilegedRegistryWriter(null, (request, operationName, privilegeLevel) =>
            {
                attempts.Add(privilegeLevel);
                return PrivilegeExecutionResult.Failure(PrivilegeErrorCodes.RegistryAccessDenied, "denied");
            });

            var result = writer.WriteString(
                RegistryValueLocation.LocalMachine,
                @"SOFTWARE\Test",
                "Value",
                "data",
                RegistryPrivilegePolicy.AutoWithoutTI);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.RegistryAccessDenied));
            Assert.That(result.AttemptedPrivilegeLevels, Is.EqualTo(new[]
            {
                PrivilegeLevel.Standard,
                PrivilegeLevel.Administrator,
                PrivilegeLevel.System
            }));
            Assert.That(attempts.Contains(PrivilegeLevel.TrustedInstaller), Is.False);
        }

        [Test]
        public void DeleteString_CurrentUserOnly_UsesCurrentOnly()
        {
            var attempts = new List<PrivilegeLevel>();
            var writer = new PrivilegedRegistryWriter(null, (request, operationName, privilegeLevel) =>
            {
                attempts.Add(privilegeLevel);
                return PrivilegeExecutionResult.Success();
            });

            var result = writer.DeleteString(
                RegistryValueLocation.LocalMachine,
                @"SOFTWARE\Test",
                "Value",
                RegistryPrivilegePolicy.CurrentUserOnly);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.EffectivePrivilegeLevel, Is.EqualTo(PrivilegeLevel.Standard));
            Assert.That(attempts, Is.EqualTo(new[] { PrivilegeLevel.Standard }));
        }
    }
}
