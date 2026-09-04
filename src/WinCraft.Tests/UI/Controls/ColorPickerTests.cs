using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using NUnit.Framework;
using WinCraft.UI;
using ColorConverter = WinCraft.UI.ColorConverter;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class ColorPickerTests
    {
        [TestCase(0, 255, 0, 0)]
        [TestCase(120, 0, 255, 0)]
        [TestCase(240, 0, 0, 255)]
        public void FromHsv_MapsPrimaryHueToExpectedColor(double hue, byte red, byte green, byte blue)
        {
            var color = ColorConverter.FromHsv(hue, 1, 1, 128);

            Assert.That(color, Is.EqualTo(Color.FromArgb(128, red, green, blue)));
        }

        [Test]
        public void ToHsv_PreservesSaturationAndValueForAnArbitraryColor()
        {
            double hue;
            double saturation;
            double value;

            ColorConverter.ToHsv(Color.FromRgb(20, 116, 242), out hue, out saturation, out value);

            Assert.That(hue, Is.EqualTo(214.05).Within(0.01));
            Assert.That(saturation, Is.EqualTo(0.917).Within(0.001));
            Assert.That(value, Is.EqualTo(0.949).Within(0.001));
        }

        [TestCase("#80402010", 128, 64, 32, 16)]
        [TestCase("#3AF", 255, 51, 170, 255)]
        public void TryParse_AcceptsArgbAndRgbHexValues(string text, byte alpha, byte red, byte green, byte blue)
        {
            Color color;
            var parsed = ColorConverter.TryParse(text, out color);

            Assert.That(parsed, Is.True);
            Assert.That(color, Is.EqualTo(Color.FromArgb(alpha, red, green, blue)));
        }

        [Test]
        public void HexTextBox_RejectsInputThatIsNotHexOrTheLeadingHash()
        {
            CreatePickerWithHexTextBox(out var hexTextBox);
            hexTextBox.Text = "#FF0000";
            hexTextBox.SelectionStart = hexTextBox.Text.Length;

            Assert.That(RaiseHexTextInput(hexTextBox, "G"), Is.True);
            Assert.That(RaiseHexTextInput(hexTextBox, " "), Is.True);
            Assert.That(RaiseHexTextInput(hexTextBox, "#"), Is.True);
            Assert.That(RaiseHexTextInput(hexTextBox, "A"), Is.False);
            Assert.That(RaiseHexTextInput(hexTextBox, "a"), Is.False);
            Assert.That(RaiseHexTextInput(hexTextBox, "3"), Is.False);
        }

        [Test]
        public void HexTextBox_AcceptsHashOnlyWhileItStaysTheLeadingCharacter()
        {
            CreatePickerWithHexTextBox(out var hexTextBox);

            hexTextBox.Text = "#FF";
            hexTextBox.SelectAll();
            Assert.That(RaiseHexTextInput(hexTextBox, "#"), Is.False);

            hexTextBox.Text = string.Empty;
            Assert.That(RaiseHexTextInput(hexTextBox, "#"), Is.False);

            hexTextBox.Text = "#FF";
            hexTextBox.SelectionStart = 0;
            hexTextBox.SelectionLength = 0;
            Assert.That(RaiseHexTextInput(hexTextBox, "#"), Is.True);
        }

        [TestCase("zz#A 9g", "#A9")]
        [TestCase("F", "#F")]
        [TestCase("##FF00", "#FF00")]
        [TestCase("AB#CD", "#ABCD")]
        [TestCase("", "")]
        public void HexTextBox_TextChangedNormalizesToASingleLeadingHash(string text, string expected)
        {
            CreatePickerWithHexTextBox(out var hexTextBox);

            hexTextBox.Text = text;

            Assert.That(hexTextBox.Text, Is.EqualTo(expected));
        }

        [Test]
        public void HexTextBox_RejectsDigitsBeyondTheMaximumDigitCount()
        {
            CreatePickerWithHexTextBox(out var hexTextBox);
            hexTextBox.Text = "#12345678";
            hexTextBox.SelectionStart = hexTextBox.Text.Length;
            Assert.That(RaiseHexTextInput(hexTextBox, "9"), Is.True);

            hexTextBox.Text = "#1234567";
            hexTextBox.SelectionStart = hexTextBox.Text.Length;
            Assert.That(RaiseHexTextInput(hexTextBox, "9"), Is.False);
        }

        [TestCase(true, "1234567890AB", "#12345678")]
        [TestCase(false, "1234567890AB", "#123456")]
        public void HexTextBox_TruncatesDigitsBeyondTheMaximumDigitCount(bool isAlphaEnabled, string text, string expected)
        {
            CreatePickerWithHexTextBox(out var hexTextBox, isAlphaEnabled);

            hexTextBox.Text = text;

            Assert.That(hexTextBox.Text, Is.EqualTo(expected));
        }

        private static ColorPicker CreatePickerWithHexTextBox(out TextBox hexTextBox, bool isAlphaEnabled = true)
        {
            var root = new FrameworkElementFactory(typeof(Grid));
            root.AppendChild(new FrameworkElementFactory(typeof(TextBox), "PART_HexTextBox"));
            var picker = new ColorPicker
            {
                IsAlphaEnabled = isAlphaEnabled,
                Template = new ControlTemplate(typeof(ColorPicker)) { VisualTree = root },
            };
            picker.ApplyTemplate();
            hexTextBox = (TextBox)picker.Template.FindName("PART_HexTextBox", picker);
            return picker;
        }

        private static bool RaiseHexTextInput(TextBox textBox, string text)
        {
            var args = new TextCompositionEventArgs(
                InputManager.Current.PrimaryKeyboardDevice,
                new TextComposition(InputManager.Current, textBox, text))
            {
                RoutedEvent = UIElement.PreviewTextInputEvent,
            };
            textBox.RaiseEvent(args);
            return args.Handled;
        }

        [Test]
        public void SelectPresetCommand_AppliesColorAndKeepsTheDropDownOpen()
        {
            var picker = new ColorPicker { IsDropDownOpen = true };

            ColorPicker.SelectPresetCommand.Execute("#1474F2", picker);

            Assert.That(picker.Color, Is.EqualTo(Color.FromRgb(20, 116, 242)));
            Assert.That(picker.IsDropDownOpen, Is.True);
        }

        [Test]
        public void IsAlphaEnabled_FalseCoercesColorsToOpaque()
        {
            var picker = new ColorPicker
            {
                Color = Color.FromArgb(128, 20, 116, 242),
                IsAlphaEnabled = false,
            };

            Assert.That(picker.Color, Is.EqualTo(Color.FromRgb(20, 116, 242)));

            picker.Color = Color.FromArgb(64, 220, 40, 70);

            Assert.That(picker.Color, Is.EqualTo(Color.FromRgb(220, 40, 70)));
        }

        [Test]
        public void HueSlider_MaximumValueRemainsAtTheRightEdge()
        {
            var initialColor = Color.FromRgb(20, 116, 242);
            double hue;
            double saturation;
            double value;
            ColorConverter.ToHsv(initialColor, out hue, out saturation, out value);

            var root = new FrameworkElementFactory(typeof(Grid));
            var hueSliderFactory = new FrameworkElementFactory(typeof(Slider), "PART_HueSlider");
            hueSliderFactory.SetValue(Slider.MaximumProperty, 360.0);
            root.AppendChild(hueSliderFactory);

            var picker = new ColorPicker
            {
                Color = initialColor,
                Template = new ControlTemplate(typeof(ColorPicker)) { VisualTree = root },
            };

            Assert.That(picker.ApplyTemplate(), Is.True);
            var hueSlider = (Slider)picker.Template.FindName("PART_HueSlider", picker);

            hueSlider.Value = hueSlider.Maximum;

            Assert.That(hueSlider.Value, Is.EqualTo(hueSlider.Maximum));
            Assert.That(picker.Color, Is.EqualTo(ColorConverter.FromHsv(360, saturation, value)));
        }

        [Test]
        public void MouseWheel_BubblingToTheDropDownRoot_IsSwallowed()
        {
            var template = (ControlTemplate)XamlReader.Parse(
                "<ControlTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"" +
                " xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"" +
                " xmlns:ui=\"clr-namespace:WinCraft.UI;assembly=WinCraft\">" +
                "<Grid><Popup x:Name=\"PART_DropDownPopup\"><ui:SuperellipseBorder/></Popup></Grid>" +
                "</ControlTemplate>");

            var picker = new ColorPicker { Template = template };
            Assert.That(picker.ApplyTemplate(), Is.True);

            var popup = (Popup)picker.Template.FindName("PART_DropDownPopup", picker);
            var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, 120)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
            };
            ((FrameworkElement)popup.Child).RaiseEvent(args);

            Assert.That(args.Handled, Is.True);
        }
    }
}
