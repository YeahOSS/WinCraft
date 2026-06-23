using NUnit.Framework;
using WinCraft.Infrastructure.Shell;

namespace WinCraft.Tests.Shell
{
    [TestFixture]
    internal sealed class ShellCommandLineTests
    {
        [Test]
        public void BuildArgumentString_Null_ReturnsEmpty()
        {
            var result = ShellCommandLine.BuildArgumentString(null);

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void BuildArgumentString_Empty_ReturnsEmpty()
        {
            var result = ShellCommandLine.BuildArgumentString(new string[0]);

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void BuildArgumentString_SinglePlainArg_ReturnsUnchanged()
        {
            var result = ShellCommandLine.BuildArgumentString(new[] { "hello" });

            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void BuildArgumentString_MultipleArgs_JoinsWithSpaces()
        {
            var result = ShellCommandLine.BuildArgumentString(new[] { "a", "b", "c" });

            Assert.That(result, Is.EqualTo("a b c"));
        }

        [Test]
        public void QuoteArgument_Empty_ReturnsQuotedEmpty()
        {
            var result = ShellCommandLine.QuoteArgument(string.Empty);

            Assert.That(result, Is.EqualTo("\"\""));
        }

        [Test]
        public void QuoteArgument_NoSpecialCharacters_ReturnsUnchanged()
        {
            var result = ShellCommandLine.QuoteArgument("simple");

            Assert.That(result, Is.EqualTo("simple"));
        }

        [Test]
        public void QuoteArgument_ContainsSpace_WrapsInQuotes()
        {
            var result = ShellCommandLine.QuoteArgument("has space");

            Assert.That(result, Is.EqualTo("\"has space\""));
        }

        [Test]
        public void QuoteArgument_ContainsTab_WrapsInQuotes()
        {
            var result = ShellCommandLine.QuoteArgument("has\ttab");

            Assert.That(result, Is.EqualTo("\"has\ttab\""));
        }

        [Test]
        public void QuoteArgument_ContainsQuote_EscapesWithBackslash()
        {
            var result = ShellCommandLine.QuoteArgument("say \"hello\"");

            Assert.That(result, Is.EqualTo("\"say \\\"hello\\\"\""));
        }

        [Test]
        public void QuoteArgument_TrailingBackslashWithoutSpace_ReturnsUnchanged()
        {
            var result = ShellCommandLine.QuoteArgument("path\\");

            Assert.That(result, Is.EqualTo("path\\"));
        }

        [Test]
        public void QuoteArgument_TrailingBackslashWithSpace_DoublesBeforeClose()
        {
            var result = ShellCommandLine.QuoteArgument("C:\\Program Files\\");

            Assert.That(result, Is.EqualTo("\"C:\\Program Files\\\\\""));
        }

        [Test]
        public void QuoteArgument_BackslashBeforeQuote_Doubles()
        {
            var result = ShellCommandLine.QuoteArgument("a\\\\\"b");

            Assert.That(result, Is.EqualTo("\"a\\\\\\\\\\\"b\""));
        }

        [Test]
        public void QuoteArgument_NullElement_QuotesEmpty()
        {
            var args = new string[] { null };
            var result = ShellCommandLine.BuildArgumentString(args);

            Assert.That(result, Is.EqualTo("\"\""));
        }
    }
}
