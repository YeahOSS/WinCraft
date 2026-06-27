using NUnit.Framework;
using WinCraft.Compatibility;

namespace WinCraft.Tests.Compatibility
{
    [TestFixture]
    internal sealed class StringCompatTests
    {
        [Test]
        public void IsNullOrWhiteSpace_Null_ReturnsTrue()
        {
            Assert.That(StringCompat.IsNullOrWhiteSpace(null), Is.True);
        }

        [Test]
        public void IsNullOrWhiteSpace_Empty_ReturnsTrue()
        {
            Assert.That(StringCompat.IsNullOrWhiteSpace(string.Empty), Is.True);
        }

        [Test]
        public void IsNullOrWhiteSpace_SpacesOnly_ReturnsTrue()
        {
            Assert.That(StringCompat.IsNullOrWhiteSpace("   "), Is.True);
        }

        [Test]
        public void IsNullOrWhiteSpace_TabOnly_ReturnsTrue()
        {
            Assert.That(StringCompat.IsNullOrWhiteSpace("\t"), Is.True);
        }

        [Test]
        public void IsNullOrWhiteSpace_MixedWhitespace_ReturnsTrue()
        {
            Assert.That(StringCompat.IsNullOrWhiteSpace(" \t \r\n "), Is.True);
        }

        [Test]
        public void IsNullOrWhiteSpace_NonEmpty_ReturnsFalse()
        {
            Assert.That(StringCompat.IsNullOrWhiteSpace("hello"), Is.False);
        }

        [Test]
        public void IsNullOrWhiteSpace_WhitespaceWithText_ReturnsFalse()
        {
            Assert.That(StringCompat.IsNullOrWhiteSpace("  a  "), Is.False);
        }
    }
}
