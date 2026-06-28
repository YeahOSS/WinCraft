using System;
using NUnit.Framework;
using WinCraft.Infrastructure.RegistryAccess;

namespace WinCraft.Tests.RegistryAccess
{
    [TestFixture]
    internal sealed class RegistryWriterTests
    {
        [TearDown]
        public void TearDown()
        {
            // Clean up any test artifacts left under the test root key.
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\WinCraft\Tests", writable: true);
            if (key != null)
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(@"Software\WinCraft\Tests", throwOnMissingSubKey: false);
        }
        [Test]
        public void WriteValue_NullRequest_ThrowsArgumentNullException()
        {
            Assert.That(
                () => RegistryWriter.WriteValue(null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void WriteValue_EmptySubKeyPath_ThrowsArgumentException()
        {
            Assert.That(
                () => RegistryWriter.WriteValue(new RegistryValueWriteRequest
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
                () => RegistryWriter.DeleteValue(null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void DeleteValue_EmptySubKeyPath_ThrowsArgumentException()
        {
            Assert.That(
                () => RegistryWriter.DeleteValue(new RegistryValueWriteRequest
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

            Assert.That(() => RegistryWriter.WriteValue(request), Throws.Nothing);
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
            RegistryWriter.WriteValue(request);

            Assert.That(() => RegistryWriter.DeleteValue(request), Throws.Nothing);
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

            Assert.That(() => RegistryWriter.DeleteValue(request), Throws.Nothing);
        }

        [Test]
        public void MoveKey_Hkcu_MovesValuesAndSubKeys()
        {
            string id = Guid.NewGuid().ToString("N");
            string sourcePath = @"Software\WinCraft\Tests\MoveSource_" + id;
            string destinationPath = @"Software\WinCraft\Tests\MoveDestination_" + id;

            RegistryWriter.WriteValue(new RegistryValueWriteRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SubKeyPath = sourcePath,
                ValueName = "RootValue",
                ValueData = "root",
                ValueKind = Microsoft.Win32.RegistryValueKind.String
            });
            RegistryWriter.WriteValue(new RegistryValueWriteRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SubKeyPath = sourcePath + @"\Child",
                ValueName = "ChildValue",
                ValueData = "child",
                ValueKind = Microsoft.Win32.RegistryValueKind.String
            });

            RegistryWriter.MoveKey(new RegistryKeyOperationRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SourceSubKeyPath = sourcePath,
                DestinationSubKeyPath = destinationPath,
                Recursive = true
            });

            Assert.That(Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\" + sourcePath, "RootValue", null), Is.Null);
            Assert.That(Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\" + destinationPath, "RootValue", null), Is.EqualTo("root"));
            Assert.That(Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\" + destinationPath + @"\Child", "ChildValue", null), Is.EqualTo("child"));

            RegistryWriter.DeleteKey(new RegistryKeyOperationRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SourceSubKeyPath = destinationPath,
                Recursive = true
            });
        }
    }
}
