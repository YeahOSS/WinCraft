using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class FocusAdornerTests
    {
        [Test]
        public void CreateFocusOutlineGeometry_UsesEllipseWhenRequested()
        {
            var button = new Button();
            FocusVisual.SetOutlineShape(button, FocusOutlineShape.Ellipse);

            var geometry = FocusAdorner.CreateFocusOutlineGeometry(button, new Size(20, 16));

            Assert.That(geometry, Is.TypeOf<EllipseGeometry>());
            Assert.That(geometry.Bounds, Is.EqualTo(new Rect(0, 0, 20, 16)));
        }

        [Test]
        public void CreateFocusOutlineGeometry_ScalesCustomNormalizedGeometry()
        {
            var button = new Button();
            FocusVisual.SetNormalizedOutlineGeometry(button, new RectangleGeometry(new Rect(0, 0, 1, 1)));

            var geometry = FocusAdorner.CreateFocusOutlineGeometry(button, new Size(20, 16));

            Assert.That(geometry, Is.TypeOf<GeometryGroup>());
            Assert.That(geometry.Bounds, Is.EqualTo(new Rect(0, 0, 20, 16)));
        }

        [Test]
        public void IconButton_Circular_UsesEllipseFocusOutline()
        {
            var dictionary = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Controls.Buttons.xaml", UriKind.Relative));
            var button = new IconButton
            {
                IsCircular = true,
                Style = (Style)dictionary[typeof(IconButton)],
            };

            Assert.That(FocusVisual.GetOutlineShape(button), Is.EqualTo(FocusOutlineShape.Ellipse));
        }

        [TestCase(ControlVariant.Solid, VisualRole.Primary, "FocusRingOnAccent")]
        [TestCase(ControlVariant.Outline, VisualRole.Primary, "FocusRingOnAccent")]
        [TestCase(ControlVariant.Outline, VisualRole.Base, "FocusRing")]
        public void GetBrushKey_UsesAContrastingRingForAccentSurfaces(
            ControlVariant variant,
            VisualRole role,
            string expectedBrushKey)
        {
            var button = new Button();
            Design.SetVariant(button, variant);
            Design.SetVisualRole(button, role);

            Assert.That(FocusAdornerManager.GetBrushKey(button), Is.EqualTo(expectedBrushKey));
        }
    }
}
