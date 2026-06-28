using NUnit.Framework;
using WinCraft.Infrastructure.RegistryAccess;

namespace WinCraft.Tests.RegistryAccess
{
    [TestFixture]
    internal sealed class RegistryPathTests
    {
        [Test]
        public void TryParse_HkcrShortName_ReturnsClassesRootPath()
        {
            bool parsed = RegistryPath.TryParse(@"HKCR\*\Shell\open", out RegistryPath path);

            Assert.That(parsed, Is.True);
            Assert.That(path.Location, Is.EqualTo(RegistryValueLocation.ClassesRoot));
            Assert.That(path.SubKeyPath, Is.EqualTo(@"*\Shell\open"));
        }

        [Test]
        public void Append_AddsChildPath()
        {
            var path = RegistryPath.ClassesRoot("*").Append("Shell").Append("open");

            Assert.That(path.ToString(), Is.EqualTo(@"HKEY_CLASSES_ROOT\*\Shell\open"));
            Assert.That(path.GetParent().ToString(), Is.EqualTo(@"HKEY_CLASSES_ROOT\*\Shell"));
            Assert.That(path.GetName(), Is.EqualTo("open"));
        }
    }
}
