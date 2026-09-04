using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Theme
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class InputAdornmentTests
    {
        [Test]
        public void TextBox_LeadingIconOnlyOccupiesSpaceWhenConfigured()
        {
            var textBox = new TextBox { Style = GetStyle(typeof(TextBox)) };
            textBox.ApplyTemplate();

            var leadingIcon = (IconBlock)textBox.Template.FindName("LeadingIcon", textBox);
            Assert.That(leadingIcon.Visibility, Is.EqualTo(Visibility.Collapsed));

            Design.SetIcon(textBox, IconGlyph.Search16);
            Assert.That(leadingIcon.Visibility, Is.EqualTo(Visibility.Visible));
        }

        [Test]
        public void TextBox_WatermarkHasCaretClearanceWithinTheContentInset()
        {
            var textBox = new TextBox
            {
                Padding = new Thickness(6, 4, 6, 4),
                Style = GetStyle(typeof(TextBox)),
            };
            textBox.ApplyTemplate();

            var watermark = (Border)textBox.Template.FindName("Watermark", textBox);
            var watermarkContent = (ContentPresenter)watermark.Child;
            var contentHost = (ScrollViewer)textBox.Template.FindName(
                "PART_ContentHost",
                textBox);

            Assert.That(contentHost.Margin, Is.EqualTo(new Thickness()));
            Assert.That(watermark.Padding, Is.EqualTo(textBox.Padding));
            Assert.That(watermarkContent.Margin, Is.EqualTo(new Thickness(2, 0, 2, 0)));
        }

        [TestCase(typeof(TextBox))]
        [TestCase(typeof(RichTextBox))]
        [TestCase(typeof(NumericBox))]
        [TestCase(typeof(PasswordBox))]
        [TestCase(typeof(ComboBox))]
        public void Input_WatermarkRemainsVisibleWhileEmptyAndFocused(Type controlType)
        {
            var input = (Control)Activator.CreateInstance(controlType);
            if (input is ComboBox comboBox)
                comboBox.IsEditable = true;

            input.Style = GetStyle(controlType);
            TextInput.SetWatermark(input, "Watermark");
            var window = CreateHiddenWindow(input);

            try
            {
                window.Show();
                input.Focus();
                window.UpdateLayout();
                ClearInputContent(input);

                Assert.That(input.IsKeyboardFocusWithin, Is.True);
                Assert.That(GetWatermark(input).Opacity, Is.EqualTo(1));

                SetInputContent(input, "42");
                Assert.That(GetWatermark(input).Opacity, Is.Zero);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void EditableComboBox_ReservesOnlyTheArrowForTheDropDownToggle()
        {
            var comboBox = new ComboBox
            {
                IsEditable = true,
                Style = GetStyle(typeof(ComboBox)),
            };
            comboBox.ApplyTemplate();

            var dropDownToggle = (ToggleButton)comboBox.Template.FindName(
                "DropDownToggle",
                comboBox);

            Assert.That(Grid.GetColumn(dropDownToggle), Is.EqualTo(1));
            Assert.That(Grid.GetColumnSpan(dropDownToggle), Is.EqualTo(1));

            var selectionOnlyComboBox = new ComboBox { Style = GetStyle(typeof(ComboBox)) };
            selectionOnlyComboBox.ApplyTemplate();

            var selectionOnlyToggle = (ToggleButton)selectionOnlyComboBox.Template.FindName(
                "DropDownToggle",
                selectionOnlyComboBox);

            Assert.That(Grid.GetColumn(selectionOnlyToggle), Is.Zero);
            Assert.That(Grid.GetColumnSpan(selectionOnlyToggle), Is.EqualTo(2));
        }

        [Test]
        public void PasswordBox_RevealButtonAndTextFollowDesignProperties()
        {
            var passwordBox = new PasswordBox { Style = GetStyle(typeof(PasswordBox)) };
            PasswordReveal.SetIsEnabled(passwordBox, true);
            passwordBox.ApplyTemplate();

            var revealButton = (ToggleButton)passwordBox.Template.FindName(
                "PasswordRevealButton",
                passwordBox);
            var revealedTextBox = (TextBox)passwordBox.Template.FindName(
                "PART_RevealedTextBox",
                passwordBox);
            var contentHost = (ScrollViewer)passwordBox.Template.FindName(
                "PART_ContentHost",
                passwordBox);

            Assert.That(revealButton.Visibility, Is.EqualTo(Visibility.Visible));
            Assert.That(revealedTextBox.Visibility, Is.EqualTo(Visibility.Collapsed));

            PasswordReveal.SetIsRevealed(passwordBox, true);

            Assert.That(revealedTextBox.Visibility, Is.EqualTo(Visibility.Visible));
            Assert.That(contentHost.Visibility, Is.EqualTo(Visibility.Hidden));
        }

        [Test]
        public void RichTextBox_CopyProvidesWpfClipboardFormats()
        {
            TextInputClipboard.Register();

            var richTextBox = new RichTextBox(
                new FlowDocument(new Paragraph(new Bold(new Run("Formatted text")))));
            richTextBox.SelectAll();

            IDataObject copiedData = null;
            DataObjectCopyingEventHandler onCopying = (_, e) =>
            {
                copiedData = e.DataObject;
                e.CancelCommand();
            };
            DataObject.AddCopyingHandler(richTextBox, onCopying);

            try
            {
                ApplicationCommands.Copy.Execute(null, richTextBox);

                Assert.That(copiedData, Is.Not.Null);
                Assert.That(copiedData.GetData(DataFormats.Text), Is.TypeOf<string>());
                Assert.That(copiedData.GetData(DataFormats.UnicodeText), Is.TypeOf<string>());
                Assert.That(copiedData.GetData(DataFormats.Rtf), Is.TypeOf<string>());
                Assert.That(copiedData.GetData(DataFormats.Xaml), Is.TypeOf<string>());
                Assert.That(copiedData.GetData(DataFormats.XamlPackage), Is.Null);
            }
            finally
            {
                DataObject.RemoveCopyingHandler(richTextBox, onCopying);
            }
        }

        [Test]
        public void RichTextBox_CopyIncludesPackageAndBitmapForASingleSelectedImage()
        {
            TextInputClipboard.Register();

            var image = new Image
            {
                Source = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null),
            };
            var container = new InlineUIContainer(image);
            var richTextBox = new RichTextBox(new FlowDocument(new Paragraph(container)));
            richTextBox.Selection.Select(container.ElementStart, container.ElementEnd);

            IDataObject copiedData = null;
            DataObjectCopyingEventHandler onCopying = (_, e) =>
            {
                copiedData = e.DataObject;
                e.CancelCommand();
            };
            DataObject.AddCopyingHandler(richTextBox, onCopying);

            try
            {
                ApplicationCommands.Copy.Execute(null, richTextBox);

                Assert.That(copiedData, Is.Not.Null);
                Assert.That(copiedData.GetData(DataFormats.XamlPackage), Is.InstanceOf<Stream>());
                Assert.That(copiedData.GetData(DataFormats.Bitmap), Is.InstanceOf<BitmapSource>());
            }
            finally
            {
                DataObject.RemoveCopyingHandler(richTextBox, onCopying);
            }
        }

        [Test]
        public void RichTextBox_CopyHonorsSettingDataCancellation()
        {
            TextInputClipboard.Register();

            var richTextBox = new RichTextBox(new FlowDocument(new Paragraph(new Run("Formatted text"))));
            richTextBox.SelectAll();

            IDataObject copiedData = null;
            DataObjectSettingDataEventHandler onSettingData = (_, e) =>
            {
                if (e.Format == DataFormats.Rtf)
                    e.CancelCommand();
            };
            DataObjectCopyingEventHandler onCopying = (_, e) =>
            {
                copiedData = e.DataObject;
                e.CancelCommand();
            };
            DataObject.AddSettingDataHandler(richTextBox, onSettingData);
            DataObject.AddCopyingHandler(richTextBox, onCopying);

            try
            {
                ApplicationCommands.Copy.Execute(null, richTextBox);

                Assert.That(copiedData, Is.Not.Null);
                Assert.That(copiedData.GetData(DataFormats.Rtf), Is.Null);
                Assert.That(copiedData.GetData(DataFormats.Xaml), Is.TypeOf<string>());
            }
            finally
            {
                DataObject.RemoveSettingDataHandler(richTextBox, onSettingData);
                DataObject.RemoveCopyingHandler(richTextBox, onCopying);
            }
        }

        [Test]
        public void Input_MultilineTextBoxShowsVerticalScrollBarWhenContentOverflows()
        {
            var textBox = new TextBox
            {
                Height = 60,
                Width = 200,
                TextWrapping = TextWrapping.Wrap,
                Style = GetStyle(typeof(TextBox)),
            };
            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < 40; i++)
                lines.AppendLine("Line " + i);
            textBox.Text = lines.ToString();

            var window = CreateHiddenWindow(textBox);
            try
            {
                window.Show();
                window.UpdateLayout();

                var contentHost = (ScrollViewer)textBox.Template.FindName(
                    "PART_ContentHost",
                    textBox);

                Assert.That(contentHost.ExtentHeight, Is.GreaterThan(contentHost.ViewportHeight));
                Assert.That(
                    contentHost.ComputedVerticalScrollBarVisibility,
                    Is.EqualTo(Visibility.Visible));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Input_RichTextBoxShowsVerticalScrollBarWhenContentOverflows()
        {
            var document = new FlowDocument();
            for (int i = 0; i < 40; i++)
                document.Blocks.Add(new Paragraph(new Run("Line " + i)));
            var richTextBox = new RichTextBox
            {
                Height = 60,
                Width = 200,
                Document = document,
                Style = GetStyle(typeof(RichTextBox)),
            };

            var window = CreateHiddenWindow(richTextBox);
            try
            {
                window.Show();
                window.UpdateLayout();

                var contentHost = (ScrollViewer)richTextBox.Template.FindName(
                    "PART_ContentHost",
                    richTextBox);

                Assert.That(contentHost.ExtentHeight, Is.GreaterThan(contentHost.ViewportHeight));
                Assert.That(
                    contentHost.ComputedVerticalScrollBarVisibility,
                    Is.EqualTo(Visibility.Visible));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RichTextBox_WatermarkAlignsWithTheFirstTextLine()
        {
            var richTextBox = new RichTextBox
            {
                Height = 120,
                Width = 200,
                Style = GetStyle(typeof(RichTextBox)),
            };
            TextInput.SetWatermark(richTextBox, "Watermark");

            var window = CreateHiddenWindow(richTextBox);
            try
            {
                window.Show();
                window.UpdateLayout();

                var watermark = (Border)richTextBox.Template.FindName("Watermark", richTextBox);
                var watermarkContent = (FrameworkElement)watermark.Child;
                var top = watermarkContent.TranslatePoint(new Point(0, 0), richTextBox).Y;

                Assert.That(top, Is.LessThan(richTextBox.ActualHeight / 2));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void TextBox_TripleClickSelectsAllWhenEnabled()
        {
            var textBox = new TextBox
            {
                Text = "Triple click me",
                Width = 200,
                Style = GetStyle(typeof(TextBox)),
            };

            var window = CreateHiddenWindow(textBox);
            try
            {
                window.Show();
                window.UpdateLayout();

                RaiseMouseLeftButtonDown(textBox, 3);

                Assert.That(textBox.SelectedText, Is.EqualTo(textBox.Text));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void TextBox_TripleClickLeavesSelectionAloneWhenDisabled()
        {
            var textBox = new TextBox { Text = "Triple click me" };
            textBox.ApplyTemplate();

            RaiseMouseLeftButtonDown(textBox, 3);

            Assert.That(textBox.SelectionLength, Is.Zero);
        }

        [Test]
        public void TextBox_TripleClickIgnoresDoubleClick()
        {
            var textBox = new TextBox
            {
                Text = "Triple click me",
                Width = 200,
                Style = GetStyle(typeof(TextBox)),
            };

            var window = CreateHiddenWindow(textBox);
            try
            {
                window.Show();
                window.UpdateLayout();

                RaiseMouseLeftButtonDown(textBox, 2);
                Assert.That(textBox.SelectionLength, Is.Zero);
            }
            finally
            {
                window.Close();
            }
        }

        private static void RaiseMouseLeftButtonDown(TextBox textBox, int clickCount)
        {
            var args = new MouseButtonEventArgs(
                Mouse.PrimaryDevice,
                Environment.TickCount,
                MouseButton.Left)
            {
                RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
            };
            typeof(MouseButtonEventArgs)
                .GetProperty("ClickCount")
                .SetValue(args, clickCount, null);
            textBox.RaiseEvent(args);
        }

        [TestCase(typeof(TextBox))]
        [TestCase(typeof(RichTextBox))]
        public void Input_VerticalScrollBarOverlaysWithoutReservingContentWidth(Type controlType)
        {
            var overflowing = CreateMultilineInput(controlType, 40);
            var fitting = CreateMultilineInput(controlType, 1);

            var panel = new StackPanel();
            panel.Children.Add(overflowing);
            panel.Children.Add(fitting);
            var window = CreateThemedWindow(panel);
            try
            {
                window.Show();
                window.UpdateLayout();

                var overflowingHost = (ScrollViewer)overflowing.Template.FindName(
                    "PART_ContentHost",
                    overflowing);
                var fittingHost = (ScrollViewer)fitting.Template.FindName(
                    "PART_ContentHost",
                    fitting);

                Assert.That(
                    overflowingHost.ComputedVerticalScrollBarVisibility,
                    Is.EqualTo(Visibility.Visible),
                    "overflowing content must show the scrollbar");
                Assert.That(
                    fittingHost.ComputedVerticalScrollBarVisibility,
                    Is.EqualTo(Visibility.Collapsed),
                    "fitting content must hide the scrollbar");

                Assert.That(
                    overflowingHost.ViewportWidth,
                    Is.EqualTo(fittingHost.ViewportWidth).Within(0.5),
                    "the overlay scrollbar must not reserve content width");
            }
            finally
            {
                window.Close();
            }
        }

        private static Control CreateMultilineInput(Type controlType, int lineCount)
        {
            if (controlType == typeof(TextBox))
            {
                var textBox = new TextBox
                {
                    Width = 200,
                    Height = 60,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Style = GetStyle(typeof(TextBox)),
                };
                for (int i = 0; i < lineCount; i++)
                    textBox.AppendText("Input line " + i + Environment.NewLine);
                return textBox;
            }

            var document = new FlowDocument();
            for (int i = 0; i < lineCount; i++)
                document.Blocks.Add(new Paragraph(new Run("Input line " + i)));
            return new RichTextBox
            {
                Width = 200,
                Height = 60,
                Document = document,
                Style = GetStyle(typeof(RichTextBox)),
            };
        }

        private static Window CreateThemedWindow(object content)
        {
            var window = new Window
            {
                Content = content,
                Left = -10000,
                Top = -10000,
                Width = 400,
                Height = 300,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            window.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/WinCraft;component/UI/Theme/Brushes.Light.xaml", UriKind.Relative),
            });
            window.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/WinCraft;component/UI/Theme/Controls.xaml", UriKind.Relative),
            });
            new WindowInteropHelper(window).EnsureHandle();
            return window;
        }

        private static Style GetStyle(Type controlType)
        {
            var resources = new ResourceDictionary
            {
                Source = new Uri(
                    "/WinCraft;component/UI/Theme/Controls.Input.xaml",
                    UriKind.Relative),
            };

            return (Style)resources[controlType];
        }

        private static FrameworkElement GetWatermark(Control input) =>
            (FrameworkElement)input.Template.FindName("Watermark", input);

        private static void ClearInputContent(Control input)
        {
            switch (input)
            {
                case TextBox textBox:
                    textBox.Clear();
                    break;
                case RichTextBox richTextBox:
                    richTextBox.Document = new FlowDocument();
                    break;
                case PasswordBox passwordBox:
                    passwordBox.Clear();
                    break;
                case ComboBox comboBox:
                    comboBox.SelectedItem = null;
                    comboBox.Text = string.Empty;
                    break;
            }
        }

        private static void SetInputContent(Control input, string value)
        {
            switch (input)
            {
                case TextBox textBox:
                    textBox.Text = value;
                    break;
                case RichTextBox richTextBox:
                    richTextBox.Document = new FlowDocument(new Paragraph(new Run(value)));
                    break;
                case PasswordBox passwordBox:
                    passwordBox.Password = value;
                    break;
                case ComboBox comboBox:
                    comboBox.Text = value;
                    break;
            }
        }

        private static Window CreateHiddenWindow(Control input)
        {
            var window = new Window
            {
                Content = input,
                Left = -10000,
                Top = -10000,
                Width = 200,
                Height = 100,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };

            new WindowInteropHelper(window).EnsureHandle();
            return window;
        }
    }
}
