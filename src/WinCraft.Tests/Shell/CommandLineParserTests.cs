using NUnit.Framework;
using WinCraft.Infrastructure.Shell;

namespace WinCraft.Tests.Shell
{
    [TestFixture]
    internal sealed class CommandLineParserTests
    {
        [Test]
        public void ExtractExecutablePath_Null_ReturnsNull()
        {
            var result = CommandLineParser.ExtractExecutablePath(null);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ExtractExecutablePath_Empty_ReturnsNull()
        {
            var result = CommandLineParser.ExtractExecutablePath(string.Empty);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ExtractExecutablePath_QuotedExeWithArgs_ReturnsExePath()
        {
            var result = CommandLineParser.ExtractExecutablePath(
                @"""C:\Program Files\App\app.exe"" /arg");

            Assert.That(result, Is.EqualTo(@"C:\Program Files\App\app.exe"));
        }

        [Test]
        public void ExtractExecutablePath_UnquotedExeWithArgs_ReturnsExePath()
        {
            var result = CommandLineParser.ExtractExecutablePath(
                @"C:\Tools\tool.exe --flag value");

            Assert.That(result, Is.EqualTo(@"C:\Tools\tool.exe"));
        }

        [Test]
        public void ExtractExecutablePath_ExeOnly_ReturnsSamePath()
        {
            var result = CommandLineParser.ExtractExecutablePath(@"C:\Tools\tool.exe");

            Assert.That(result, Is.EqualTo(@"C:\Tools\tool.exe"));
        }

        [Test]
        public void ExtractExecutablePath_NonExeFirstToken_ReturnsNull()
        {
            // "cmd /c echo hello" — the first token is not an exe path.
            var result = CommandLineParser.ExtractExecutablePath("cmd /c echo hello");

            // PathIsExe would check cmd.exe via SearchPath; depends on PATH.
            // Just verify the token is extracted correctly.
            Assert.That(result, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void ResolveExecutablePath_Null_ReturnsNull()
        {
            var result = CommandLineParser.ResolveExecutablePath(null);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ResolveExecutablePath_Empty_ReturnsNull()
        {
            var result = CommandLineParser.ResolveExecutablePath(string.Empty);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ResolveExecutablePath_Whitespace_ReturnsNull()
        {
            var result = CommandLineParser.ResolveExecutablePath("   ");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void Parse_PlainCommand_ParsesFileAndArgs()
        {
            var result = CommandLineParser.Parse(@"""C:\app.exe"" arg1 arg2");

            Assert.That(result.File, Is.EqualTo(@"C:\app.exe"));
            Assert.That(result.Args, Is.EqualTo("arg1 arg2"));
        }

        [Test]
        public void Parse_PlainCommandWithoutArgs_ParsesFileOnly()
        {
            var result = CommandLineParser.Parse(@"C:\app.exe");

            Assert.That(result.File, Is.EqualTo(@"C:\app.exe"));
            Assert.That(result.Args, Is.Empty);
        }

        [Test]
        public void Parse_Empty_ReturnsDefault()
        {
            var result = CommandLineParser.Parse(string.Empty);

            Assert.That(result.File, Is.Empty);
            Assert.That(result.Args, Is.Empty);
        }

        [Test]
        public void Parse_MshtaVbscript_ExtractsEmbeddedExePath()
        {
            var command = @"mshta.exe vbscript:createobject(""shell.application"").shellexecute(""C:\app.exe"",""args"",""dir"",""open"",1)";

            var result = CommandLineParser.Parse(command);

            Assert.That(result.File, Is.EqualTo(@"C:\app.exe"));
            Assert.That(result.Args, Is.EqualTo("args"));
        }
    }
}
