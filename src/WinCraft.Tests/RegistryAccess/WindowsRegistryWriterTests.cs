using System;
using NUnit.Framework;
using WinCraft.Infrastructure.RegistryAccess;

namespace WinCraft.Tests.RegistryAccess
{
    [TestFixture]
    internal sealed class WindowsRegistryWriterTests
    {
        [Test]
        public void WriteValue_NullRequest_ThrowsArgumentNullException()
        {
            Assert.That(
                () => WindowsRegistryWriter.WriteValue(null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void WriteValue_EmptySubKeyPath_ThrowsArgumentException()
        {
            Assert.That(
                () => WindowsRegistryWriter.WriteValue(new RegistryValueWriteRequest
                {
                    SubKeyPath = string.Empty,
                    Location = RegistryValueLocation.CurrentUser
                }),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void DeleteValue_NullRequest_ThrowsArgumentNullException()
        {
            Assert.That(
                () => WindowsRegistryWriter.DeleteValue(null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void DeleteValue_EmptySubKeyPath_ThrowsArgumentException()
        {
            Assert.That(
                () => WindowsRegistryWriter.DeleteValue(new RegistryValueWriteRequest
                {
                    SubKeyPath = string.Empty,
                    Location = RegistryValueLocation.CurrentUser
                }),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void WriteValue_Hkcu_DoesNotThrow()
        {
            var request = new RegistryValueWriteRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SubKeyPath = @"Software\WinCraft\Tests",
                ValueName = "TestValue",
                ValueData = "hello"
            };

            Assert.That(() => WindowsRegistryWriter.WriteValue(request), Throws.Nothing);
        }

        [Test]
        public void DeleteValue_Hkcu_DoesNotThrow()
        {
            var request = new RegistryValueWriteRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SubKeyPath = @"Software\WinCraft\Tests",
                ValueName = "TestValue"
            };

            // Write first so there's something to delete
            WindowsRegistryWriter.WriteValue(request);

            Assert.That(() => WindowsRegistryWriter.DeleteValue(request), Throws.Nothing);
        }

        [Test]
        public void DeleteValue_NonExistentKey_DoesNotThrow()
        {
            var request = new RegistryValueWriteRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SubKeyPath = @"Software\WinCraft\Tests\NonExistent_" + Guid.NewGuid().ToString("N"),
                ValueName = "Missing"
            };

            Assert.That(() => WindowsRegistryWriter.DeleteValue(request), Throws.Nothing);
        }
    }
}
