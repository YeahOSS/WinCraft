using System;
using NUnit.Framework;
using Windows.Win32;

namespace WinCraft.Tests.Interop
{
    [TestFixture]
    internal sealed class WindowsxTests
    {
        [TestCase(0x00010002, 2, 1)]
        [TestCase(unchecked((int)0xFFFE8001), -32767, -2)]
        public void GetLParamCoordinates_PreservesSignedValues(int packed, int expectedX, int expectedY)
        {
            var lParam = new IntPtr(packed);

            Assert.That(Windowsx.GetXLParam(lParam), Is.EqualTo(expectedX));
            Assert.That(Windowsx.GetYLParam(lParam), Is.EqualTo(expectedY));
        }

        [Test]
        public void GetSystemCommand_IgnoresTheLowOrderSourceBits()
        {
            var wParam = new IntPtr((int)PInvoke.SC_CLOSE + 1);

            Assert.That(Windowsx.GetSystemCommand(wParam), Is.EqualTo(PInvoke.SC_CLOSE));
        }
    }
}
