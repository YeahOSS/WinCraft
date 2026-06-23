using NUnit.Framework;
using WinCraft.Infrastructure.Shell;

namespace WinCraft.Tests.Shell
{
    [TestFixture]
    internal sealed class CommandLineArgumentsTests
    {
        [Test]
        public void Contains_MatchingFlag_ReturnsTrue()
        {
            Assert.That(CommandLineArguments.Contains(new[] { "--flag" }, "--flag"), Is.True);
        }

        [Test]
        public void Contains_CaseInsensitive_ReturnsTrue()
        {
            Assert.That(CommandLineArguments.Contains(new[] { "--Flag" }, "--flag"), Is.True);
        }

        [Test]
        public void Contains_NonMatchingFlag_ReturnsFalse()
        {
            Assert.That(CommandLineArguments.Contains(new[] { "--other" }, "--flag"), Is.False);
        }

        [Test]
        public void Contains_NullArgs_ReturnsFalse()
        {
            Assert.That(CommandLineArguments.Contains(null, "--flag"), Is.False);
        }

        [Test]
        public void Contains_NullName_ReturnsFalse()
        {
            Assert.That(CommandLineArguments.Contains(new[] { "--flag" }, null), Is.False);
        }

        [Test]
        public void Contains_EmptyArgs_ReturnsFalse()
        {
            Assert.That(CommandLineArguments.Contains(new string[0], "--flag"), Is.False);
        }

        [Test]
        public void GetInt32Value_ValidInteger_ReturnsValue()
        {
            var result = CommandLineArguments.GetInt32Value(new[] { "--count", "42" }, "--count");

            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void GetInt32Value_NonInteger_ReturnsZero()
        {
            var result = CommandLineArguments.GetInt32Value(new[] { "--count", "abc" }, "--count");

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void GetInt32Value_MissingFlag_ReturnsZero()
        {
            var result = CommandLineArguments.GetInt32Value(new[] { "--other", "42" }, "--count");

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void TryGetInt32Value_ValidInteger_ReturnsTrueWithValue()
        {
            var success = CommandLineArguments.TryGetInt32Value(
                new[] { "--pid", "1234" }, "--pid", out int value);

            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo(1234));
        }

        [Test]
        public void TryGetInt32Value_NonInteger_ReturnsFalse()
        {
            var success = CommandLineArguments.TryGetInt32Value(
                new[] { "--pid", "xyz" }, "--pid", out int value);

            Assert.That(success, Is.False);
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void TryGetInt32Value_MissingFlag_ReturnsFalse()
        {
            var success = CommandLineArguments.TryGetInt32Value(
                new[] { "--other" }, "--pid", out int value);

            Assert.That(success, Is.False);
        }

        [Test]
        public void GetValue_MatchingFlag_ReturnsNextArg()
        {
            var result = CommandLineArguments.GetValue(new[] { "--name", "test-pipe" }, "--name");

            Assert.That(result, Is.EqualTo("test-pipe"));
        }

        [Test]
        public void GetValue_FlagAtEnd_ReturnsNull()
        {
            var result = CommandLineArguments.GetValue(new[] { "--name" }, "--name");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetValue_MissingFlag_ReturnsNull()
        {
            var result = CommandLineArguments.GetValue(new[] { "--other" }, "--name");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetValue_NullArgs_ReturnsNull()
        {
            var result = CommandLineArguments.GetValue(null, "--name");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetValue_NullName_ReturnsNull()
        {
            var result = CommandLineArguments.GetValue(new[] { "--name", "x" }, null);

            Assert.That(result, Is.Null);
        }
    }
}
