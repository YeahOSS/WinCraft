using System;
using System.Threading;
using System.Windows;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class MessageBarTests
    {
        [Test]
        public void Role_SelectsDefaultIconUnlessIconIsExplicit()
        {
            var messageBar = new MessageBar
            {
                Style = LoadMessageBarStyle(),
            };

            Assert.That(Design.GetIcon(messageBar), Is.EqualTo(IconGlyph.Info24));

            Design.SetVisualRole(messageBar, VisualRole.Warning);
            Assert.That(Design.GetIcon(messageBar), Is.EqualTo(IconGlyph.Warning24));

            Design.SetIcon(messageBar, IconGlyph.Document24);
            Assert.That(Design.GetIcon(messageBar), Is.EqualTo(IconGlyph.Document24));
        }

        private static Style LoadMessageBarStyle()
        {
            var resources = new ResourceDictionary
            {
                Source = new Uri(
                    "/WinCraft;component/UI/Theme/Controls.Feedback.xaml",
                    UriKind.Relative),
            };

            return (Style)resources[typeof(MessageBar)];
        }
    }
}
