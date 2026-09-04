using System.Threading;
using System.Windows.Media;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class IconBlockTests
    {
        [Test]
        public void FilledIcon_WithMatchingGlyph_UsesFilledFontAndCodePoint()
        {
            var regularFont = new FontFamily("Arial");
            var filledFont = new FontFamily("Courier New");
            var icon = CreateIconBlock(regularFont, filledFont);

            icon.Icon = IconGlyph.Settings24;
            icon.ApplyIsIconFilled(true);

            Assert.That(icon.FontFamily, Is.SameAs(filledFont));
            Assert.That(icon.Text, Is.EqualTo(char.ConvertFromUtf32(63155)));
        }

        [Test]
        public void FilledIcon_WithoutMatchingGlyph_FallsBackToRegularFont()
        {
            var regularFont = new FontFamily("Arial");
            var filledFont = new FontFamily("Courier New");
            var icon = CreateIconBlock(regularFont, filledFont);

            icon.Icon = IconGlyph.ColorBackgroundAccent20;
            icon.ApplyIsIconFilled(true);

            Assert.That(icon.FontFamily, Is.SameAs(regularFont));
            Assert.That(icon.Text, Is.EqualTo(char.ConvertFromUtf32(58301)));
        }

        private static IconBlock CreateIconBlock(FontFamily regularFont, FontFamily filledFont)
        {
            var icon = new IconBlock();
            icon.Resources.Add("IconFontFamily", regularFont);
            icon.Resources.Add("IconFontFamilyFilled", filledFont);
            return icon;
        }
    }
}
