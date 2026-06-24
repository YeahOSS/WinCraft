using NUnit.Framework;
using WinCraft.Infrastructure.Security;
using WinCraft.Startup;

namespace WinCraft.Tests
{
    [TestFixture]
    internal sealed class StartupTests
    {
        [Test]
        public void TryParsePipeOwnerProcessId_Null_ReturnsNull()
        {
            var result = ElevatedHostStartup.TryParsePipeOwnerProcessId(null);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryParsePipeOwnerProcessId_Empty_ReturnsNull()
        {
            var result = ElevatedHostStartup.TryParsePipeOwnerProcessId(string.Empty);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryParsePipeOwnerProcessId_NonMatchingPrefix_ReturnsNull()
        {
            var result = ElevatedHostStartup.TryParsePipeOwnerProcessId("SomeOther.Foo.1234");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryParsePipeOwnerProcessId_ValidWithGuid_ReturnsPid()
        {
            var pipeName = "WinCraft.ElevatedAgent.1234.abc123def456";

            var result = ElevatedHostStartup.TryParsePipeOwnerProcessId(pipeName);

            Assert.That(result, Is.EqualTo(1234));
        }

        [Test]
        public void TryParsePipeOwnerProcessId_ValidWithoutGuid_ReturnsPid()
        {
            var pipeName = "WinCraft.ElevatedAgent.5678";

            var result = ElevatedHostStartup.TryParsePipeOwnerProcessId(pipeName);

            Assert.That(result, Is.EqualTo(5678));
        }

        [Test]
        public void TryParsePipeOwnerProcessId_NonNumericPid_ReturnsNull()
        {
            var pipeName = "WinCraft.ElevatedAgent.abc.def";

            var result = ElevatedHostStartup.TryParsePipeOwnerProcessId(pipeName);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryParsePipeOwnerProcessId_ZeroPid_ReturnsNull()
        {
            var pipeName = "WinCraft.ElevatedAgent.0.guid";

            var result = ElevatedHostStartup.TryParsePipeOwnerProcessId(pipeName);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryParsePipeOwnerProcessId_NegativePid_ReturnsNull()
        {
            var pipeName = "WinCraft.ElevatedAgent.-1.guid";

            var result = ElevatedHostStartup.TryParsePipeOwnerProcessId(pipeName);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryParsePipeOwnerProcessId_CaseInsensitivePrefix_ReturnsPid()
        {
            var pipeName = "wincraft.elevatedagent.9999.guid";

            var result = ElevatedHostStartup.TryParsePipeOwnerProcessId(pipeName);

            Assert.That(result, Is.EqualTo(9999));
        }

        [Test]
        public void AppendArguments_BothNonNull_CombinesArrays()
        {
            var leading = new[] { "a", "b" };
            var trailing = new[] { "c", "d" };

            var result = ElevatedHostStartup.AppendArguments(leading, trailing);

            Assert.That(result, Is.EqualTo(new[] { "a", "b", "c", "d" }));
        }

        [Test]
        public void AppendArguments_NullLeading_UsesTrailing()
        {
            var trailing = new[] { "x", "y" };

            var result = ElevatedHostStartup.AppendArguments(null, trailing);

            Assert.That(result, Is.EqualTo(new[] { "x", "y" }));
        }

        [Test]
        public void AppendArguments_NullTrailing_UsesLeading()
        {
            var leading = new[] { "x", "y" };

            var result = ElevatedHostStartup.AppendArguments(leading, null);

            Assert.That(result, Is.EqualTo(new[] { "x", "y" }));
        }

        [Test]
        public void AppendArguments_BothNull_ReturnsEmpty()
        {
            var result = ElevatedHostStartup.AppendArguments(null, null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void AppendArguments_EmptyArrays_ReturnsEmpty()
        {
            var result = ElevatedHostStartup.AppendArguments(new string[0], new string[0]);

            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void SelectStartupProcessMode_SplitTokenElevated_ReturnsElevatedBootstrap()
        {
            var result = StartupModeSelector.Select(ProcessElevationState.SplitTokenElevated);

            Assert.That(result, Is.EqualTo(StartupProcessMode.ElevatedBootstrap));
        }

        [Test]
        public void SelectStartupProcessMode_FullAdministrator_ReturnsUserInterface()
        {
            var result = StartupModeSelector.Select(ProcessElevationState.FullAdministrator);

            Assert.That(result, Is.EqualTo(StartupProcessMode.UserInterface));
        }

        [Test]
        public void SelectStartupProcessMode_Standard_ReturnsUserInterface()
        {
            var result = StartupModeSelector.Select(ProcessElevationState.Standard);

            Assert.That(result, Is.EqualTo(StartupProcessMode.UserInterface));
        }
    }
}
