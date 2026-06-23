using NUnit.Framework;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Tests.Security
{
    [TestFixture]
    internal sealed class ProcessElevationTests
    {
        [Test]
        public void ClassifyElevationState_Full_ReturnsSplitTokenElevated()
        {
            var result = ProcessElevation.ClassifyElevationState(true, TokenElevationKind.Full);

            Assert.That(result, Is.EqualTo(ProcessElevationState.SplitTokenElevated));
        }

        [Test]
        public void ClassifyElevationState_DefaultAdministrator_ReturnsFullAdministrator()
        {
            var result = ProcessElevation.ClassifyElevationState(true, TokenElevationKind.Default);

            Assert.That(result, Is.EqualTo(ProcessElevationState.FullAdministrator));
        }

        [Test]
        public void ClassifyElevationState_DefaultNonAdministrator_ReturnsStandard()
        {
            var result = ProcessElevation.ClassifyElevationState(false, TokenElevationKind.Default);

            Assert.That(result, Is.EqualTo(ProcessElevationState.Standard));
        }

        [Test]
        public void ClassifyElevationState_Limited_ReturnsStandard()
        {
            var result = ProcessElevation.ClassifyElevationState(true, TokenElevationKind.Limited);

            Assert.That(result, Is.EqualTo(ProcessElevationState.Standard));
        }

        [Test]
        public void ClassifyElevationState_QueryFailedAdministrator_ReturnsFullAdministrator()
        {
            var result = ProcessElevation.ClassifyElevationState(true, null);

            Assert.That(result, Is.EqualTo(ProcessElevationState.FullAdministrator));
        }

        [Test]
        public void ClassifyElevationState_QueryFailedNonAdministrator_ReturnsStandard()
        {
            var result = ProcessElevation.ClassifyElevationState(false, null);

            Assert.That(result, Is.EqualTo(ProcessElevationState.Standard));
        }
    }
}
