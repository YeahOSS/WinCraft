using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    internal sealed class TitleBarPanelTests
    {
        [Test]
        public void Center_WithRoom_AlignsToPanelMidpoint()
        {
            TitleBarPanel.ArrangeSlots(1000, 100, 200, 150,
                out double left, out double width);

            Assert.That(left, Is.EqualTo(400));
            Assert.That(width, Is.EqualTo(200));
        }

        [Test]
        public void Center_LeftGroupOverlapsMidpoint_IsPushedRight()
        {
            TitleBarPanel.ArrangeSlots(1000, 450, 200, 150,
                out double left, out double width);

            Assert.That(left, Is.EqualTo(450));
            Assert.That(width, Is.EqualTo(200));
        }

        [Test]
        public void Center_RightGroupOverlapsMidpoint_IsPushedLeft()
        {
            TitleBarPanel.ArrangeSlots(1000, 100, 200, 450,
                out double left, out double width);

            Assert.That(left, Is.EqualTo(350));
            Assert.That(width, Is.EqualTo(200));
        }

        [Test]
        public void Center_WiderThanGap_ShrinksToGap()
        {
            TitleBarPanel.ArrangeSlots(1000, 300, 800, 300,
                out double left, out double width);

            Assert.That(left, Is.EqualTo(300));
            Assert.That(width, Is.EqualTo(400));
        }

        [Test]
        public void Center_NoGap_Collapses()
        {
            TitleBarPanel.ArrangeSlots(400, 300, 200, 300,
                out double left, out double width);

            Assert.That(width, Is.EqualTo(0));
            Assert.That(left, Is.EqualTo(300));
        }

        [Test]
        public void Center_EmptyNeighbors_FillsMidpoint()
        {
            TitleBarPanel.ArrangeSlots(1000, 0, 200, 0,
                out double left, out double width);

            Assert.That(left, Is.EqualTo(400));
            Assert.That(width, Is.EqualTo(200));
        }

        [Test]
        public void Center_ZeroDesiredWidth_StaysAtMidpoint()
        {
            TitleBarPanel.ArrangeSlots(1000, 100, 0, 100,
                out double left, out double width);

            Assert.That(left, Is.EqualTo(500));
            Assert.That(width, Is.EqualTo(0));
        }
    }
}
