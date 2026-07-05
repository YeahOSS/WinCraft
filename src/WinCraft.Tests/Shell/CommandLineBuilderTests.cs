using NUnit.Framework;
using WinCraft.Infrastructure.Shell;

namespace WinCraft.Tests.Shell
{
    [TestFixture]
    internal sealed class CommandLineBuilderTests
    {
        [Test]
        public void BuildArgumentString_Null_ReturnsEmpty()
        {
            var result = CommandLineBuilder.BuildArgumentString(null);

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void BuildArgumentString_Empty_ReturnsEmpty()
        {
            var result = CommandLineBuilder.BuildArgumentString(new string[0]);

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void BuildArgumentString_SinglePlainArg_ReturnsUnchanged()
        {
            var result = CommandLineBuilder.BuildArgumentString(new[] { "hello" });

            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void BuildArgumentString_MultipleArgs_JoinsWithSpaces()
        {
            var result = CommandLineBuilder.BuildArgumentString(new[] { "a", "b", "c" });

            Assert.That(result, Is.EqualTo("a b c"));
        }

        [Test]
        public void QuoteArgument_Empty_ReturnsQuotedEmpty()
        {
            var result = CommandLineBuilder.QuoteArgument(string.Empty);

            Assert.That(result, Is.EqualTo("\"\""));
        }

        [Test]
        public void QuoteArgument_NoSpecialCharacters_ReturnsUnchanged()
        {
            var result = CommandLineBuilder.QuoteArgument("simple");

            Assert.That(result, Is.EqualTo("simple"));
        }

        [Test]
        public void QuoteArgument_ContainsSpace_WrapsInQuotes()
        {
            var result = CommandLineBuilder.QuoteArgument("has space");

            Assert.That(result, Is.EqualTo("\"has space\""));
        }

        [Test]
        public void QuoteArgument_ContainsTab_WrapsInQuotes()
        {
            var result = CommandLineBuilder.QuoteArgument("has\ttab");

            Assert.That(result, Is.EqualTo("\"has\ttab\""));
        }

        [Test]
        public void QuoteArgument_ContainsQuote_EscapesWithBackslash()
        {
            var result = CommandLineBuilder.QuoteArgument("say \"hello\"");

            Assert.That(result, Is.EqualTo("\"say \\\"hello\\\"\""));
        }

        [Test]
        public void QuoteArgument_TrailingBackslashWithoutSpace_ReturnsUnchanged()
        {
            var result = CommandLineBuilder.QuoteArgument("path\\");

            Assert.That(result, Is.EqualTo("path\\"));
        }

        [Test]
        public void QuoteArgument_TrailingBackslashWithSpace_DoublesBeforeClose()
        {
            var result = CommandLineBuilder.QuoteArgument("C:\\Program Files\\");

            Assert.That(result, Is.EqualTo("\"C:\\Program Files\\\\\""));
        }

        [Test]
        public void QuoteArgument_BackslashBeforeQuote_Doubles()
        {
            var result = CommandLineBuilder.QuoteArgument("a\\\\\"b");

            Assert.That(result, Is.EqualTo("\"a\\\\\\\\\\\"b\""));
        }

        [Test]
        public void QuoteArgument_NullElement_QuotesEmpty()
        {
            var args = new string[] { null };
            var result = CommandLineBuilder.BuildArgumentString(args);

            Assert.That(result, Is.EqualTo("\"\""));
        }
    }
}
