using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Theme
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class BackdropThemeScopeTests
    {
        [Test]
        public void BackdropThemeScope_AddsAndRemovesTheBackdropPalette()
        {
            var scope = new BackdropThemeScope();

            scope.IsActive = true;

            Assert.That(scope.Resources.MergedDictionaries.Single().Source.OriginalString,
                Does.EndWith(".Backdrop.xaml"));
            Assert.That(((SolidColorBrush)scope.FindResource("BgDefault")).Color.A,
                Is.LessThan(byte.MaxValue));

            scope.IsActive = false;

            Assert.That(scope.Resources.MergedDictionaries, Is.Empty);
        }
    }
}
