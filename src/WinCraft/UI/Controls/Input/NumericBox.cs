using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinCraft.Compatibility;

namespace WinCraft.UI
{
    public enum NumericInputType
    {
        Integer,
        Decimal
    }

    public class NumericBox : TextBox
    {
        public static readonly RoutedCommand IncreaseCommand =
            new(nameof(IncreaseCommand), typeof(NumericBox));

        public static readonly RoutedCommand DecreaseCommand =
            new(nameof(DecreaseCommand), typeof(NumericBox));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(NumericBox),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged,
                    CoerceValue));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(NumericBox),
                new PropertyMetadata(double.NegativeInfinity, OnRangeChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(NumericBox),
                new PropertyMetadata(double.PositiveInfinity, OnRangeChanged));

        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(
                nameof(Step),
                typeof(double),
                typeof(NumericBox),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty DecimalPlacesProperty =
            DependencyProperty.Register(
                nameof(DecimalPlaces),
                typeof(int),
                typeof(NumericBox),
                new PropertyMetadata(0, OnDecimalPlacesChanged, CoerceDecimalPlaces));

        public static readonly DependencyProperty InputTypeProperty =
            DependencyProperty.Register(
                nameof(InputType),
                typeof(NumericInputType),
                typeof(NumericBox),
                new PropertyMetadata(NumericInputType.Integer, OnInputTypeChanged));

        private bool _isUpdatingText;

        static NumericBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(NumericBox),
                new FrameworkPropertyMetadata(typeof(NumericBox)));
        }

        public NumericBox()
        {
            base.Text = FormatValue(Value);
            CommandBindings.Add(new CommandBinding(IncreaseCommand, OnIncreaseCommand, OnCanIncrease));
            CommandBindings.Add(new CommandBinding(DecreaseCommand, OnDecreaseCommand, OnCanDecrease));
            // TextBoxBase only suppresses its default menu for a local null value.
            ContextMenu = null;
            DataObject.AddPastingHandler(this, OnPaste);
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Step
        {
            get => (double)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

        public int DecimalPlaces
        {
            get => (int)GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }

        public NumericInputType InputType
        {
            get => (NumericInputType)GetValue(InputTypeProperty);
            set => SetValue(InputTypeProperty, value);
        }

        [Obsolete("NumericBox uses Value instead.", true)]
        public new string Text
        {
            get => base.Text;
            set => base.Text = value;
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);
            if (IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            e.Handled = !IsValidInput(GetProposedText(e.Text));
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            if (_isUpdatingText)
                return;

            if (TryParseText(base.Text, out var value))
            {
                _isUpdatingText = true;
                try
                {
                    SetControlValue(ValueProperty, value);
                    if (HasNormalizedValueChanged(Value, value))
                        UpdateTextFromValue();
                }
                finally
                {
                    _isUpdatingText = false;
                }
            }
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            CommitText();
            base.OnLostKeyboardFocus(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Up)
            {
                ChangeValue(GetNormalizedStep());
                e.Handled = true;
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Down)
            {
                ChangeValue(-GetNormalizedStep());
                e.Handled = true;
                return;
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                CommitText();

            base.OnKeyDown(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            // ComboBox-style behavior: adjust the value only when the box
            // owns keyboard focus, so hovering without focus still scrolls
            // the surrounding page instead of hijacking it.
            if (IsKeyboardFocusWithin && !IsReadOnly &&
                Keyboard.Modifiers == ModifierKeys.None && e.Delta != 0)
            {
                ChangeValue(e.Delta > 0 ? GetNormalizedStep() : -GetNormalizedStep());
                e.Handled = true;
                return;
            }

            base.OnMouseWheel(e);
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            return ((NumericBox)d).NormalizeValue((double)baseValue);
        }

        private static object CoerceDecimalPlaces(DependencyObject d, object baseValue)
        {
            var value = (int)baseValue;
            if (((NumericBox)d).InputType == NumericInputType.Integer)
                return 0;

            return Math.Max(0, Math.Min(12, value));
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = (NumericBox)d;
            if (!box._isUpdatingText)
                box.UpdateTextFromValue();

            CommandManager.InvalidateRequerySuggested();
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = (NumericBox)d;
            if (box.Minimum > box.Maximum)
            {
                if (e.Property == MinimumProperty)
                    box.SetControlValue(MaximumProperty, box.Minimum);
                else
                    box.SetControlValue(MinimumProperty, box.Maximum);
            }

            box.CoerceValue(ValueProperty);
            box.UpdateTextFromValue();
            CommandManager.InvalidateRequerySuggested();
        }

        private static void OnDecimalPlacesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = (NumericBox)d;
            box.CoerceValue(ValueProperty);
            box.UpdateTextFromValue();
        }

        private static void OnInputTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = (NumericBox)d;
            box.CoerceValue(DecimalPlacesProperty);
            box.CoerceValue(ValueProperty);
            box.CommitText();
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var text = e.DataObject.GetData(DataFormats.Text) as string;
            if (!IsValidInput(GetProposedText(text ?? string.Empty)))
                e.CancelCommand();
        }

        private void OnIncreaseCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ChangeValue(GetNormalizedStep());
        }

        private void OnDecreaseCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ChangeValue(-GetNormalizedStep());
        }

        private void OnCanIncrease(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsEnabled && !IsReadOnly && Value < Maximum;
        }

        private void OnCanDecrease(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsEnabled && !IsReadOnly && Value > Minimum;
        }

        private void ChangeValue(double delta)
        {
            if (IsReadOnly)
                return;

            SetControlValue(ValueProperty, Value + delta);
            SelectAll();
        }

        private void CommitText()
        {
            if (!TryParseText(base.Text, out var value))
            {
                UpdateTextFromValue();
                return;
            }

            SetControlValue(ValueProperty, value);
            UpdateTextFromValue();
        }

        private void SetControlValue(DependencyProperty property, object value)
            => DependencyObjectCompat.SetControlValue(this, property, value);

        private void UpdateTextFromValue()
        {
            var formatted = FormatValue(Value);
            if (base.Text == formatted)
                return;

            _isUpdatingText = true;
            base.Text = formatted;
            _isUpdatingText = false;
        }

        private static bool HasNormalizedValueChanged(double normalizedValue, double parsedValue)
        {
            return !string.Equals(
                normalizedValue.ToString("R", CultureInfo.InvariantCulture),
                parsedValue.ToString("R", CultureInfo.InvariantCulture),
                StringComparison.Ordinal);
        }

        private string FormatValue(double value)
        {
            return value.ToString("F" + DecimalPlaces, CultureInfo.CurrentCulture);
        }

        private double NormalizeValue(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                value = GetFiniteFallback();

            value = Math.Max(Minimum, Math.Min(Maximum, value));
            return Math.Round(value, DecimalPlaces);
        }

        private double GetFiniteFallback()
        {
            if (!double.IsNegativeInfinity(Minimum))
                return Minimum;

            if (!double.IsPositiveInfinity(Maximum))
                return Maximum;

            return 0.0;
        }

        private double GetNormalizedStep()
        {
            var step = Step;
            if (double.IsNaN(step) || double.IsInfinity(step) || step <= 0)
                return 1.0;

            return step;
        }

        private string GetProposedText(string input)
        {
            return base.Text.Remove(SelectionStart, SelectionLength).Insert(SelectionStart, input);
        }

        private bool IsValidInput(string text)
        {
            if (IsIntermediateText(text))
                return true;

            return TryParseText(text, out var value) &&
                   (Minimum < 0 || value >= 0);
        }

        private bool IsIntermediateText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            var culture = CultureInfo.CurrentCulture;
            var separator = culture.NumberFormat.NumberDecimalSeparator;
            var negative = culture.NumberFormat.NegativeSign;
            var allowsNegative = Minimum < 0;
            var allowsDecimal = InputType == NumericInputType.Decimal;

            if (allowsNegative && text == negative)
                return true;

            if (!allowsDecimal)
                return false;

            return text == separator ||
                   (allowsNegative && text == negative + separator);
        }

        private bool TryParseText(string text, out double value)
        {
            var styles = NumberStyles.AllowLeadingSign;
            if (InputType == NumericInputType.Decimal)
                styles |= NumberStyles.AllowDecimalPoint;

            return double.TryParse(
                text,
                styles,
                CultureInfo.CurrentCulture,
                out value);
        }
    }
}
