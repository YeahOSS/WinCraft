using System;
using NUnit.Framework;
using WinCraft.Compatibility;

namespace WinCraft.Tests.Compatibility
{
    [TestFixture]
    internal sealed class PathCompatTests
    {
        [Test]
        public void Combine_ThreeSegments_JoinsWithSeparator()
        {
            var result = PathCompat.Combine("a", "b", "c");

            Assert.That(result, Does.EndWith("a\\b\\c").Or.EndWith("a/b/c"));
        }

        [Test]
        public void Combine_FourSegments_JoinsWithSeparator()
        {
            var result = PathCompat.Combine("a", "b", "c", "d");

            Assert.That(result, Does.EndWith("a\\b\\c\\d").Or.EndWith("a/b/c/d"));
        }

        [Test]
        public void Combine_Params_MultipleSegments_JoinsWithSeparator()
        {
            var result = PathCompat.Combine("x", "y", "z");

            Assert.That(result, Does.EndWith("x\\y\\z").Or.EndWith("x/y/z"));
        }

        [Test]
        public void Combine_Params_SingleSegment_ReturnsSameValue()
        {
            var result = PathCompat.Combine("only");

            Assert.That(result, Does.EndWith("only"));
        }

        [Test]
        public void Combine_Params_NullArray_ThrowsArgumentNullException()
        {
            Assert.That(
                () => PathCompat.Combine(null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void Combine_Params_EmptyArray_ThrowsArgumentException()
        {
            Assert.That(
                () => PathCompat.Combine(new string[0]),
                Throws.InstanceOf<ArgumentException>());
        }
    }
}
