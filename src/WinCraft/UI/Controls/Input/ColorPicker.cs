using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using WinCraft.Compatibility;

namespace WinCraft.UI
{
    public class ColorPicker : Control
    {
        private const string DropDownPopupPartName = "PART_DropDownPopup";
        private const string ColorPlanePartName = "PART_ColorPlane";
        private const string ColorPlaneThumbPartName = "PART_ColorPlaneThumb";
        private const string HueSliderPartName = "PART_HueSlider";
        private const string AlphaSliderPartName = "PART_AlphaSlider";
        private const string HexTextBoxPartName = "PART_HexTextBox";
        private const string DisplayTextPartName = "PART_DisplayText";
        private const string DisplayPreviewPartName = "PART_DisplayPreview";
        private const string ArgbAPartName = "PART_ArgbA";
        private const string ArgbRPartName = "PART_ArgbR";
        private const string ArgbGPartName = "PART_ArgbG";
        private const string ArgbBPartName = "PART_ArgbB";

        private static readonly LinearGradientBrush HueBrush = CreateHueBrush();

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(
                    Colors.Black,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnColorChanged,
                    CoerceColor));

        public static readonly DependencyProperty IsAlphaEnabledProperty =
            DependencyProperty.Register(
                nameof(IsAlphaEnabled),
                typeof(bool),
                typeof(ColorPicker),
                new PropertyMetadata(true, OnIsAlphaEnabledChanged));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(
                nameof(IsDropDownOpen),
                typeof(bool),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsDropDownOpenChanged));

        public static readonly RoutedCommand SelectPresetCommand =
            new(nameof(SelectPresetCommand), typeof(ColorPicker));

        private Border _colorPlane;
        private FrameworkElement _colorPlaneThumb;
        private Slider _hueSlider;
        private Slider _alphaSlider;
        private TextBox _hexTextBox;
        private TextBlock _displayText;
        private Border _displayPreview;
        private bool _isPickingColorPlane;
        private bool _isPreservingHsv;
        private bool _isUpdatingTemplate;
        private double _hue;
        private double _saturation;
        private double _value;
        private SolidColorBrush _colorPlaneBrush;
        private SolidColorBrush _displayPreviewBrush;
        private readonly DropdownHandler _dropdownHandler;
        private NumericBox _argbA;
        private NumericBox _argbR;
        private NumericBox _argbG;
        private NumericBox _argbB;
        private bool _isUpdatingArgb;
        private bool _isUpdatingHexText;
        private FrameworkElement _dropDownPopupRoot;

        static ColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(typeof(ColorPicker)));
        }

        public ColorPicker()
        {
            CommandBindings.Add(new CommandBinding(SelectPresetCommand, OnSelectPresetCommand));
            _dropdownHandler = new DropdownHandler(this, () => IsDropDownOpen, v => IsDropDownOpen = v);
        }

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public bool IsAlphaEnabled
        {
            get => (bool)GetValue(IsAlphaEnabledProperty);
            set => SetValue(IsAlphaEnabledProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);
            _dropdownHandler.HandleKeyboardFocusChanged(IsKeyboardFocusWithin);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            _dropdownHandler.HandleKeyDown(e);
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // The template ToggleButton is Focusable="False", so clicking it
            // does not automatically move keyboard focus into this control.
            // Explicitly focus on the tunneling phase so IsKeyboardFocusWithin drives the border.
            if (IsEnabled && !IsKeyboardFocusWithin)
                Focus();
            base.OnPreviewMouseLeftButtonDown(e);
        }

        public override void OnApplyTemplate()
        {
            DetachTemplateEvents();
            base.OnApplyTemplate();

            _colorPlane = GetTemplateChild(ColorPlanePartName) as Border;
            _colorPlaneThumb = GetTemplateChild(ColorPlaneThumbPartName) as FrameworkElement;
            _hueSlider = GetTemplateChild(HueSliderPartName) as Slider;
            _alphaSlider = GetTemplateChild(AlphaSliderPartName) as Slider;
            _hexTextBox = GetTemplateChild(HexTextBoxPartName) as TextBox;
            if (_hexTextBox != null)
            {
                // Template values do not suppress TextBoxBase's default menu.
                _hexTextBox.ContextMenu = null;
            }
            _displayText = GetTemplateChild(DisplayTextPartName) as TextBlock;
            _displayPreview = GetTemplateChild(DisplayPreviewPartName) as Border;

            DetachArgbEvents();
            _argbA = GetTemplateChild(ArgbAPartName) as NumericBox;
            _argbR = GetTemplateChild(ArgbRPartName) as NumericBox;
            _argbG = GetTemplateChild(ArgbGPartName) as NumericBox;
            _argbB = GetTemplateChild(ArgbBPartName) as NumericBox;
            AttachArgbEvents();

            AttachTemplateEvents();
            UpdateHsvFromColor(Color);
            UpdateTemplateFromState();
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (ColorPicker)d;
            if (!picker._isPreservingHsv)
                picker.UpdateHsvFromColor((Color)e.NewValue);

            picker.UpdateTemplateFromState();
        }

        private static object CoerceColor(DependencyObject d, object baseValue)
        {
            var color = (Color)baseValue;
            return ((ColorPicker)d).IsAlphaEnabled
                ? color
                : Color.FromRgb(color.R, color.G, color.B);
        }

        private static void OnIsAlphaEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (ColorPicker)d;
            picker._isPreservingHsv = true;
            try
            {
                picker.CoerceValue(ColorProperty);
            }
            finally
            {
                picker._isPreservingHsv = false;
            }

            picker.UpdateTemplateFromState();
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (ColorPicker)d;
            if ((bool)e.NewValue)
                picker._dropdownHandler.OnOpened();
            else
                picker._dropdownHandler.OnClosed();
        }

        private void OnSelectPresetCommand(object sender, ExecutedRoutedEventArgs e)
        {
            var text = e.Parameter as string;
            Color color;
            if (!ColorConverter.TryParse(text, out color))
                return;

            SetControlValue(ColorProperty, color);
        }

        private void AttachTemplateEvents()
        {
            if (_colorPlane != null)
            {
                _colorPlane.MouseLeftButtonDown += OnColorPlaneMouseLeftButtonDown;
                _colorPlane.MouseMove += OnColorPlaneMouseMove;
                _colorPlane.MouseLeftButtonUp += OnColorPlaneMouseLeftButtonUp;
                _colorPlane.LostMouseCapture += OnColorPlaneLostMouseCapture;
                _colorPlane.SizeChanged += OnColorPlaneSizeChanged;
            }

            if (_hueSlider != null)
            {
                _hueSlider.Background = HueBrush;
                _hueSlider.ValueChanged += OnHueSliderValueChanged;
            }

            if (_alphaSlider != null)
                _alphaSlider.ValueChanged += OnAlphaSliderValueChanged;

            if (_hexTextBox != null)
            {
                _hexTextBox.PreviewTextInput += OnHexTextBoxPreviewTextInput;
                _hexTextBox.TextChanged += OnHexTextBoxTextChanged;
                _hexTextBox.KeyDown += OnHexTextBoxKeyDown;
                _hexTextBox.LostKeyboardFocus += OnHexTextBoxLostKeyboardFocus;
                DataObject.AddPastingHandler(_hexTextBox, OnHexTextBoxPaste);
            }

            if (GetTemplateChild(DropDownPopupPartName) is Popup dropDownPopup && dropDownPopup.Child is FrameworkElement dropDownRoot)
            {
                _dropDownPopupRoot = dropDownRoot;
                dropDownRoot.AddHandler(MouseWheelEvent, new MouseWheelEventHandler(OnDropDownMouseWheel));
            }
        }

        private void DetachTemplateEvents()
        {
            if (_colorPlane != null)
            {
                _colorPlane.MouseLeftButtonDown -= OnColorPlaneMouseLeftButtonDown;
                _colorPlane.MouseMove -= OnColorPlaneMouseMove;
                _colorPlane.MouseLeftButtonUp -= OnColorPlaneMouseLeftButtonUp;
                _colorPlane.LostMouseCapture -= OnColorPlaneLostMouseCapture;
                _colorPlane.SizeChanged -= OnColorPlaneSizeChanged;
            }

            if (_hueSlider != null)
                _hueSlider.ValueChanged -= OnHueSliderValueChanged;

            if (_alphaSlider != null)
                _alphaSlider.ValueChanged -= OnAlphaSliderValueChanged;

            if (_hexTextBox != null)
            {
                _hexTextBox.PreviewTextInput -= OnHexTextBoxPreviewTextInput;
                _hexTextBox.TextChanged -= OnHexTextBoxTextChanged;
                _hexTextBox.KeyDown -= OnHexTextBoxKeyDown;
                _hexTextBox.LostKeyboardFocus -= OnHexTextBoxLostKeyboardFocus;
                DataObject.RemovePastingHandler(_hexTextBox, OnHexTextBoxPaste);
            }

            if (_dropDownPopupRoot != null)
            {
                _dropDownPopupRoot.RemoveHandler(MouseWheelEvent, new MouseWheelEventHandler(OnDropDownMouseWheel));
                _dropDownPopupRoot = null;
            }
        }

        private void OnDropDownMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Reaching the popup root means no control inside the flyout
            // consumed the wheel; swallow it so the window behind does not scroll.
            e.Handled = true;
        }

        private void AttachArgbEvents()
        {
            if (_argbA == null) return;

            var desc = DependencyPropertyDescriptor.FromProperty(
                NumericBox.ValueProperty, typeof(NumericBox));
            desc.AddValueChanged(_argbA, OnArgbValueChanged);
            desc.AddValueChanged(_argbR, OnArgbValueChanged);
            desc.AddValueChanged(_argbG, OnArgbValueChanged);
            desc.AddValueChanged(_argbB, OnArgbValueChanged);
        }

        private void DetachArgbEvents()
        {
            if (_argbA == null) return;

            var desc = DependencyPropertyDescriptor.FromProperty(
                NumericBox.ValueProperty, typeof(NumericBox));
            desc.RemoveValueChanged(_argbA, OnArgbValueChanged);
            desc.RemoveValueChanged(_argbR, OnArgbValueChanged);
            desc.RemoveValueChanged(_argbG, OnArgbValueChanged);
            desc.RemoveValueChanged(_argbB, OnArgbValueChanged);
        }

        private void OnArgbValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingArgb) return;

            _isUpdatingArgb = true;
            try
            {
                var a = (byte)MathCompat.Clamp(_argbA.Value, byte.MinValue, byte.MaxValue);
                var r = (byte)MathCompat.Clamp(_argbR.Value, byte.MinValue, byte.MaxValue);
                var g = (byte)MathCompat.Clamp(_argbG.Value, byte.MinValue, byte.MaxValue);
                var b = (byte)MathCompat.Clamp(_argbB.Value, byte.MinValue, byte.MaxValue);
                SetControlValue(ColorProperty, Color.FromArgb(a, r, g, b));
            }
            finally
            {
                _isUpdatingArgb = false;
            }
        }

        private void UpdateArgbBoxesFromColor()
        {
            if (_argbA == null) return;

            _isUpdatingArgb = true;
            try
            {
                _argbA.Value = Color.A;
                _argbR.Value = Color.R;
                _argbG.Value = Color.G;
                _argbB.Value = Color.B;
            }
            finally
            {
                _isUpdatingArgb = false;
            }
        }

        private void OnColorPlaneMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isPickingColorPlane = true;
            _colorPlane.CaptureMouse();
            UpdateColorPlane(e.GetPosition(_colorPlane));
            e.Handled = true;
        }

        private void OnColorPlaneMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPickingColorPlane)
                UpdateColorPlane(e.GetPosition(_colorPlane));
        }

        private void OnColorPlaneMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isPickingColorPlane)
                return;

            UpdateColorPlane(e.GetPosition(_colorPlane));
            _colorPlane.ReleaseMouseCapture();
            _isPickingColorPlane = false;
            e.Handled = true;
        }

        private void OnColorPlaneLostMouseCapture(object sender, MouseEventArgs e)
        {
            _isPickingColorPlane = false;
        }

        private void OnColorPlaneSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateColorPlaneThumb();
        }

        private void OnHueSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingTemplate)
                return;

            _hue = MathCompat.Clamp(e.NewValue, 0, 360);
            UpdateColorFromHsv();
        }

        private void OnAlphaSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingTemplate)
                return;

            var alpha = (byte)Math.Round(MathCompat.Clamp(e.NewValue, byte.MinValue, byte.MaxValue));
            var color = Color.FromArgb(alpha, Color.R, Color.G, Color.B);
            SetColorPreservingHsv(color);
        }

        private void OnHexTextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length != 1)
            {
                e.Handled = true;
                return;
            }

            var character = e.Text[0];
            var remaining = _hexTextBox.Text
                .Remove(_hexTextBox.SelectionStart, _hexTextBox.SelectionLength);
            if (Uri.IsHexDigit(character))
            {
                // Cap the digit count for the current alpha mode.
                e.Handled = CountHexDigits(remaining) >= MaxHexDigitCount;
                return;
            }

            // '#' is accepted only while it stays the single leading character.
            e.Handled = character != '#' || _hexTextBox.SelectionStart != 0 || remaining.IndexOf('#') >= 0;
        }

        private void OnHexTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingHexText)
                return;

            var text = _hexTextBox.Text;
            var normalizedText = NormalizeHexInput(text);
            if (normalizedText == text)
                return;

            var selectionStart = _hexTextBox.SelectionStart;
            _isUpdatingHexText = true;
            try
            {
                _hexTextBox.Text = normalizedText;
            }
            finally
            {
                _isUpdatingHexText = false;
            }

            _hexTextBox.SelectionStart = Math.Min(selectionStart, normalizedText.Length);
            _hexTextBox.SelectionLength = 0;
        }

        private void OnHexTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitHexText();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                UpdateHexText();
                e.Handled = true;
            }
        }

        private void OnHexTextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            CommitHexText();
        }

        private void OnHexTextBoxPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var pastedText = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            var selectionStart = _hexTextBox.SelectionStart;
            var insertionEnd = selectionStart + pastedText.Length;
            var proposedText = _hexTextBox.Text
                .Remove(selectionStart, _hexTextBox.SelectionLength)
                .Insert(selectionStart, pastedText);
            var normalizedText = NormalizeHexInput(proposedText);
            if (normalizedText == proposedText)
                return;

            _hexTextBox.Text = normalizedText;
            _hexTextBox.SelectionStart = Math.Min(
                GetNormalizedHexSelectionStart(proposedText, insertionEnd),
                normalizedText.Length);
            _hexTextBox.SelectionLength = 0;
            e.CancelCommand();
        }

        private void UpdateColorPlane(Point point)
        {
            if (_colorPlane == null || _colorPlane.ActualWidth <= 0 || _colorPlane.ActualHeight <= 0)
                return;

            _saturation = MathCompat.Clamp(point.X / _colorPlane.ActualWidth, 0, 1);
            _value = MathCompat.Clamp(1 - point.Y / _colorPlane.ActualHeight, 0, 1);
            UpdateColorFromHsv();
        }

        private void UpdateColorFromHsv()
        {
            var color = ColorConverter.FromHsv(_hue, _saturation, _value, Color.A);
            if (Color == color)
            {
                UpdateTemplateFromState();
                return;
            }

            SetColorPreservingHsv(color);
        }

        private void SetColorPreservingHsv(Color color)
        {
            _isPreservingHsv = true;
            try
            {
                SetControlValue(ColorProperty, color);
            }
            finally
            {
                _isPreservingHsv = false;
            }
        }

        private void UpdateHsvFromColor(Color color)
        {
            ColorConverter.ToHsv(color, out double hue, out _saturation, out _value);

            // When saturation or value is zero (pure black, white, or gray),
            // hue is mathematically undefined.  Preserve the previous hue so
            // that dragging the saturation/value back up restores the color
            // the user originally picked rather than snapping to red (hue=0).
            if (_saturation > 0 && _value > 0)
                _hue = hue;
        }

        private void UpdateTemplateFromState()
        {
            _isUpdatingTemplate = true;
            try
            {
                if (_hueSlider != null)
                    _hueSlider.Value = _hue;

                if (_alphaSlider != null)
                {
                    _alphaSlider.Value = Color.A;
                    _alphaSlider.Background = CreateAlphaBrush(Color);
                }

                if (_colorPlane != null)
                {
                    var planeColor = ColorConverter.FromHsv(_hue, 1, 1);
                    if (_colorPlaneBrush == null)
                    {
                        _colorPlaneBrush = new SolidColorBrush(planeColor);
                        _colorPlane.Background = _colorPlaneBrush;
                    }
                    else
                    {
                        _colorPlaneBrush.Color = planeColor;
                    }
                }

                UpdateColorPlaneThumb();
                UpdateHexText();
                UpdateArgbBoxesFromColor();
            }
            finally
            {
                _isUpdatingTemplate = false;
            }
        }

        private void UpdateColorPlaneThumb()
        {
            if (_colorPlane == null || _colorPlaneThumb == null)
                return;

            var x = _saturation * _colorPlane.ActualWidth - (_colorPlaneThumb.Width / 2);
            var y = (1 - _value) * _colorPlane.ActualHeight - (_colorPlaneThumb.Height / 2);
            Canvas.SetLeft(_colorPlaneThumb, x);
            Canvas.SetTop(_colorPlaneThumb, y);
        }

        private void UpdateHexText()
        {
            var text = FormatColor(Color);
            if (_hexTextBox != null && _hexTextBox.Text != text)
                _hexTextBox.Text = text;

            if (_displayText != null)
                _displayText.Text = text;

            if (_displayPreview != null)
            {
                if (_displayPreviewBrush == null)
                {
                    _displayPreviewBrush = new SolidColorBrush(Color);
                    _displayPreview.Background = _displayPreviewBrush;
                }
                else
                {
                    _displayPreviewBrush.Color = Color;
                }
            }
        }

        private void CommitHexText()
        {
            if (_hexTextBox == null) return;

            Color color;
            if (!ColorConverter.TryParse(_hexTextBox.Text, out color))
            {
                UpdateHexText();
                return;
            }

            SetControlValue(ColorProperty, color);
        }

        private int MaxHexDigitCount => IsAlphaEnabled ? 8 : 6;

        private string NormalizeHexInput(string text)
        {
            // Keep a single leading '#' followed by hex digits; drop everything else
            // and truncate to the maximum digit count for the current alpha mode.
            var normalizedText = NormalizeHexText(text);
            var maxTextLength = MaxHexDigitCount + 1;
            return normalizedText.Length > maxTextLength
                ? normalizedText.Substring(0, maxTextLength)
                : normalizedText;
        }

        private static string NormalizeHexText(string text)
        {
            var digits = new string(Array.FindAll(text.ToCharArray(), Uri.IsHexDigit));
            if (digits.Length > 0)
                return "#" + digits;

            // No hex digit survived; keep the marker only when one was present.
            return text.IndexOf('#') >= 0 ? "#" : string.Empty;
        }

        private static int CountHexDigits(string text)
        {
            return Array.FindAll(text.ToCharArray(), Uri.IsHexDigit).Length;
        }

        private static int GetNormalizedHexSelectionStart(string text, int insertionEnd)
        {
            if (text.Length == 0)
                return 0;

            var prefixLength = Math.Min(insertionEnd, text.Length);
            return CountHexDigits(text.Substring(0, prefixLength)) + 1;
        }

        private void SetControlValue(DependencyProperty property, object value)
            => DependencyObjectCompat.SetControlValue(this, property, value);

        private static LinearGradientBrush CreateAlphaBrush(Color color)
        {
            var opaque = Color.FromRgb(color.R, color.G, color.B);
            var transparent = Color.FromArgb(0, color.R, color.G, color.B);
            var brush = new LinearGradientBrush(transparent, opaque, 0);
            brush.Freeze();
            return brush;
        }

        private static string FormatColor(Color color)
        {
            return color.A == byte.MaxValue
                ? string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B)
                : string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B);
        }

        private static LinearGradientBrush CreateHueBrush()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5),
            };

            brush.GradientStops.Add(new GradientStop(Colors.Red, 0));
            brush.GradientStops.Add(new GradientStop(Colors.Yellow, 1.0 / 6));
            brush.GradientStops.Add(new GradientStop(Colors.Lime, 2.0 / 6));
            brush.GradientStops.Add(new GradientStop(Colors.Cyan, 3.0 / 6));
            brush.GradientStops.Add(new GradientStop(Colors.Blue, 4.0 / 6));
            brush.GradientStops.Add(new GradientStop(Colors.Magenta, 5.0 / 6));
            brush.GradientStops.Add(new GradientStop(Colors.Red, 1));
            brush.Freeze();
            return brush;
        }

    }

    internal static class ColorConverter
    {
        public static bool TryParse(string text, out Color color)
        {
            color = default(Color);
            if (StringCompat.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();
            if (text.StartsWith("#", StringComparison.Ordinal))
                text = text.Substring(1);

            switch (text.Length)
            {
                case 3:
                    return TryParse("FF", Expand(text[0]), Expand(text[1]), Expand(text[2]), out color);
                case 4:
                    return TryParse(Expand(text[0]), Expand(text[1]), Expand(text[2]), Expand(text[3]), out color);
                case 6:
                    return TryParse("FF", text.Substring(0, 2), text.Substring(2, 2), text.Substring(4, 2), out color);
                case 8:
                    return TryParse(text.Substring(0, 2), text.Substring(2, 2), text.Substring(4, 2), text.Substring(6, 2), out color);
                default:
                    return false;
            }
        }

        public static Color FromHsv(double hue, double saturation, double value, byte alpha = byte.MaxValue)
        {
            hue = NormalizeHue(hue);
            saturation = MathCompat.Clamp(saturation, 0, 1);
            value = MathCompat.Clamp(value, 0, 1);

            var chroma = value * saturation;
            var intermediate = chroma * (1 - Math.Abs((hue / 60 % 2) - 1));
            double red;
            double green;
            double blue;

            if (hue < 60)
            {
                red = chroma;
                green = intermediate;
                blue = 0;
            }
            else if (hue < 120)
            {
                red = intermediate;
                green = chroma;
                blue = 0;
            }
            else if (hue < 180)
            {
                red = 0;
                green = chroma;
                blue = intermediate;
            }
            else if (hue < 240)
            {
                red = 0;
                green = intermediate;
                blue = chroma;
            }
            else if (hue < 300)
            {
                red = intermediate;
                green = 0;
                blue = chroma;
            }
            else
            {
                red = chroma;
                green = 0;
                blue = intermediate;
            }

            var minimum = value - chroma;
            return Color.FromArgb(alpha, ToByte(red + minimum), ToByte(green + minimum), ToByte(blue + minimum));
        }

        public static void ToHsv(Color color, out double hue, out double saturation, out double value)
        {
            var maximum = Math.Max(color.R, Math.Max(color.G, color.B));
            var minimum = Math.Min(color.R, Math.Min(color.G, color.B));
            var delta = maximum - minimum;

            value = maximum / 255.0;
            saturation = maximum == 0 ? 0 : delta / (double)maximum;
            if (delta == 0)
            {
                hue = 0;
                return;
            }

            if (maximum == color.R)
                hue = 60 * (((color.G - color.B) / (double)delta) % 6);
            else if (maximum == color.G)
                hue = 60 * (((color.B - color.R) / (double)delta) + 2);
            else
                hue = 60 * (((color.R - color.G) / (double)delta) + 4);

            hue = NormalizeHue(hue);
        }

        public static double NormalizeHue(double hue)
        {
            hue %= 360;
            return hue < 0 ? hue + 360 : hue;
        }

        private static byte ToByte(double value)
        {
            return (byte)Math.Round(MathCompat.Clamp(value, 0, 1) * byte.MaxValue);
        }

        private static string Expand(char value)
        {
            return new string(value, 2);
        }

        private static bool TryParse(string alpha, string red, string green, string blue, out Color color)
        {
            color = default;
            if (!byte.TryParse(alpha, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte alphaValue) ||
                !byte.TryParse(red, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte redValue) ||
                !byte.TryParse(green, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte greenValue) ||
                !byte.TryParse(blue, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte blueValue))
                return false;

            color = Color.FromArgb(alphaValue, redValue, greenValue, blueValue);
            return true;
        }

    }
}
