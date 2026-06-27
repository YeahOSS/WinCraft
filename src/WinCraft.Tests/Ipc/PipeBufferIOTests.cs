using System;
using NUnit.Framework;
using WinCraft.Infrastructure.Ipc;

namespace WinCraft.Tests.Ipc
{
    [TestFixture]
    internal sealed class PipeBufferIOTests
    {
        [Test]
        public void BuildFullPipeName_SimpleName_ReturnsPrefixedPath()
        {
            var result = PipeBufferIO.BuildFullPipeName("MyPipe");

            Assert.That(result, Is.EqualTo("\\\\.\\pipe\\MyPipe"));
        }

        [Test]
        public void BuildFullPipeName_GuidName_ReturnsPrefixedPath()
        {
            var pipeName = "WinCraft.Test." + Guid.NewGuid().ToString("N");

            var result = PipeBufferIO.BuildFullPipeName(pipeName);

            Assert.That(result, Is.EqualTo("\\\\.\\pipe\\" + pipeName));
        }

        [Test]
        public void BuildFullPipeName_EmptyName_ReturnsOnlyPrefix()
        {
            var result = PipeBufferIO.BuildFullPipeName(string.Empty);

            Assert.That(result, Is.EqualTo("\\\\.\\pipe\\"));
        }

        [Test]
        public void BuildFullPipeName_AllowsUnderscore()
        {
            var result = PipeBufferIO.BuildFullPipeName("my_pipe_name");

            Assert.That(result, Is.EqualTo("\\\\.\\pipe\\my_pipe_name"));
        }
    }
}
