using System;
using NUnit.Framework;
using WinCraft.Infrastructure.RegistryAccess;

namespace WinCraft.Tests.RegistryAccess
{
    [TestFixture]
    internal sealed class RegistryWriterTests
    {
        private const string TestRootPrefix = @"Software\WinCraft\Tests_";

        private string _testRootPath;

        [SetUp]
        public void SetUp()
        {
            _testRootPath = TestRootPrefix + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_testRootPath))
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(_testRootPath, throwOnMissingSubKey: false);
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
        public void WriteValue_Hkcu_WritesValue()
        {
            var request = new RegistryValueWriteRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SubKeyPath = _testRootPath,
                ValueName = "TestValue",
                ValueData = "hello"
            };

            RegistryWriter.WriteValue(request);

            Assert.That(
                Microsoft.Win32.Registry.GetValue(ToHkcuPath(_testRootPath), "TestValue", null),
                Is.EqualTo("hello"));
        }

        [Test]
        public void DeleteValue_Hkcu_RemovesValue()
        {
            var request = new RegistryValueWriteRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SubKeyPath = _testRootPath,
                ValueName = "TestValue"
            };

            RegistryWriter.WriteValue(request);
            RegistryWriter.DeleteValue(request);

            Assert.That(
                Microsoft.Win32.Registry.GetValue(ToHkcuPath(_testRootPath), "TestValue", null),
                Is.Null);
        }

        [Test]
        public void DeleteValue_NonExistentKey_DoesNotThrow()
        {
            var request = new RegistryValueWriteRequest
            {
                Location = RegistryValueLocation.CurrentUser,
                SubKeyPath = CombineTestPath("NonExistent"),
                ValueName = "Missing"
            };

            Assert.That(() => RegistryWriter.DeleteValue(request), Throws.Nothing);
        }

        [Test]
        public void MoveKey_Hkcu_MovesValuesAndSubKeys()
        {
            string sourcePath = CombineTestPath("MoveSource");
            string destinationPath = CombineTestPath("MoveDestination");

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

            Assert.That(Microsoft.Win32.Registry.GetValue(ToHkcuPath(sourcePath), "RootValue", null), Is.Null);
            Assert.That(Microsoft.Win32.Registry.GetValue(ToHkcuPath(destinationPath), "RootValue", null), Is.EqualTo("root"));
            Assert.That(Microsoft.Win32.Registry.GetValue(ToHkcuPath(destinationPath + @"\Child"), "ChildValue", null), Is.EqualTo("child"));
        }

        private string CombineTestPath(string childPath)
        {
            return _testRootPath + @"\" + childPath;
        }

        private static string ToHkcuPath(string subKeyPath)
        {
            return @"HKEY_CURRENT_USER\" + subKeyPath;
        }
    }
}
