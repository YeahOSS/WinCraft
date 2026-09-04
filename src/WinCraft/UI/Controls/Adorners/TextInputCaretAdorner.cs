using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using WinCraft.Infrastructure;

namespace WinCraft.UI
{
    internal sealed class TextInputCaretAdorner : Adorner
    {
        private const int CaretBlinkMilliseconds = 530;

        public static readonly DependencyProperty CaretBrushProperty =
            DependencyProperty.Register(
                nameof(CaretBrush),
                typeof(Brush),
                typeof(TextInputCaretAdorner),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush CaretBrush
        {
            get => (Brush)GetValue(CaretBrushProperty);
            set => SetValue(CaretBrushProperty, value);
        }

        private readonly Control _textInput;
        private readonly DispatcherTimer _blinkTimer;
        private bool _isCaretVisible;

        public TextInputCaretAdorner(Control textInput) : base(textInput)
        {
            _textInput = textInput;
            IsHitTestVisible = false;
            SnapsToDevicePixels = true;

            _blinkTimer = new DispatcherTimer(DispatcherPriority.Input, textInput.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(CaretBlinkMilliseconds),
            };
            _blinkTimer.Tick += OnBlinkTimerTick;

            textInput.GotKeyboardFocus += OnGotKeyboardFocus;
            textInput.LostKeyboardFocus += OnLostKeyboardFocus;
            textInput.LayoutUpdated += OnLayoutUpdated;

            if (textInput is TextBoxBase textBoxBase)
            {
                textBoxBase.SelectionChanged += OnTextInputChanged;
                textBoxBase.TextChanged += OnTextInputChanged;
            }
            else if (textInput is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged += OnPasswordChanged;
            }

            UpdateCaretVisibility();
        }

        public void Detach()
        {
            _blinkTimer.Stop();
            _blinkTimer.Tick -= OnBlinkTimerTick;

            _textInput.GotKeyboardFocus -= OnGotKeyboardFocus;
            _textInput.LostKeyboardFocus -= OnLostKeyboardFocus;
            _textInput.LayoutUpdated -= OnLayoutUpdated;

            if (_textInput is TextBoxBase textBoxBase)
            {
                textBoxBase.SelectionChanged -= OnTextInputChanged;
                textBoxBase.TextChanged -= OnTextInputChanged;
            }
            else if (_textInput is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged -= OnPasswordChanged;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!_isCaretVisible || !TryGetCaretRectangle(out var caretRectangle))
                return;

            var brush = CaretBrush ?? _textInput.Foreground;
            if (brush == null)
                return;

            drawingContext.DrawRectangle(brush, null, caretRectangle);
        }

        private void OnBlinkTimerTick(object sender, EventArgs e)
        {
            _isCaretVisible = !_isCaretVisible;
            InvalidateVisual();
        }

        private void OnGotKeyboardFocus(object sender, RoutedEventArgs e) =>
            UpdateCaretVisibility();

        private void OnLostKeyboardFocus(object sender, RoutedEventArgs e) =>
            UpdateCaretVisibility();

        private void OnTextInputChanged(object sender, RoutedEventArgs e) =>
            RestartCaretBlink();

        private void OnPasswordChanged(object sender, RoutedEventArgs e) =>
            RestartCaretBlink();

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            if (_textInput.IsKeyboardFocusWithin)
                InvalidateVisual();
        }

        private void UpdateCaretVisibility()
        {
            _isCaretVisible = _textInput.IsKeyboardFocusWithin;
            if (_isCaretVisible)
                _blinkTimer.Start();
            else
                _blinkTimer.Stop();

            InvalidateVisual();
        }

        private void RestartCaretBlink()
        {
            if (!_textInput.IsKeyboardFocusWithin)
                return;

            _isCaretVisible = true;
            _blinkTimer.Stop();
            _blinkTimer.Start();
            InvalidateVisual();
        }

        private bool TryGetCaretRectangle(out Rect caretRectangle)
        {
            caretRectangle = Rect.Empty;
            var selection = GetSelection();
            if (selection == null)
                return false;

            var selectionType = selection.GetType();
            if (!selection.TryGetNonPublicProperty(selectionType, "CaretElement", out object caretElementObj))
                return false;

            var caretElement = caretElementObj as Adorner;
            if (caretElement == null)
                return false;

            var caretElementType = caretElement.GetType();
            if (!caretElement.TryGetNonPublicField(caretElementType, "_left", out double left) ||
                !caretElement.TryGetNonPublicField(caretElementType, "_top", out double top) ||
                !caretElement.TryGetNonPublicField(caretElementType, "_height", out double height) ||
                !caretElement.TryGetNonPublicField(caretElementType, "_systemCaretWidth", out double width) ||
                height <= 0)
            {
                return false;
            }

            var adornedElement = caretElement.AdornedElement;
            if (adornedElement == null)
                return false;

            try
            {
                var topLeft = adornedElement.TransformToAncestor(_textInput).Transform(
                    new Point(left - width / 2, top));
                caretRectangle = new Rect(
                    topLeft.X,
                    topLeft.Y,
                    Math.Max(1, width),
                    height);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private object GetSelection()
        {
            if (_textInput is TextBoxBase)
            {
                _textInput.TryGetNonPublicProperty(typeof(TextBoxBase), "TextSelectionInternal", out object selection);
                return selection;
            }

            if (_textInput is PasswordBox)
            {
                _textInput.TryGetNonPublicProperty(typeof(PasswordBox), "Selection", out object selection);
                return selection;
            }

            return null;
        }
    }
}
