using NUnit.Framework;
using WinCraft.Infrastructure.RegistryAccess;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Tests.Security
{
    [TestFixture]
    internal sealed class PrivilegedRegistryWriterTests
    {
        [Test]
        public void WriteString_LocalMachineWithStandardLevel_ReturnsPrivilegeLevelRequired()
        {
            var writer = new PrivilegedRegistryWriter(null);

            var result = writer.WriteString(
                RegistryValueLocation.LocalMachine,
                @"SOFTWARE\Test",
                "Value",
                "data",
                PrivilegeLevel.Standard);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.PrivilegeLevelRequired));
        }

        [Test]
        public void WriteString_LocalMachineWithNullBroker_ReturnsUnavailable()
        {
            var writer = new PrivilegedRegistryWriter(null);

            var result = writer.WriteString(
                RegistryValueLocation.LocalMachine,
                @"SOFTWARE\Test",
                "Value",
                "data",
                PrivilegeLevel.Administrator);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.ElevatedAgentUnavailable));
        }

        [Test]
        public void DeleteString_LocalMachineWithStandardLevel_ReturnsPrivilegeLevelRequired()
        {
            var writer = new PrivilegedRegistryWriter(null);

            var result = writer.DeleteString(
                RegistryValueLocation.LocalMachine,
                @"SOFTWARE\Test",
                "Value",
                PrivilegeLevel.Standard);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.PrivilegeLevelRequired));
        }

        [Test]
        public void DeleteString_LocalMachineWithNullBroker_ReturnsUnavailable()
        {
            var writer = new PrivilegedRegistryWriter(null);

            var result = writer.DeleteString(
                RegistryValueLocation.LocalMachine,
                @"SOFTWARE\Test",
                "Value",
                PrivilegeLevel.Administrator);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(PrivilegeErrorCodes.ElevatedAgentUnavailable));
        }
    }
}
