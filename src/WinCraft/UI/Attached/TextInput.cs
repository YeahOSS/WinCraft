using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WinCraft.Compatibility;

namespace WinCraft.UI
{
    public static class TextInput
    {
        private static readonly DependencyPropertyKey HasTextPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "HasText",
                typeof(bool),
                typeof(TextInput),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.RegisterAttached(
                "Watermark",
                typeof(object),
                typeof(TextInput),
                new FrameworkPropertyMetadata(null, OnWatermarkChanged));

        public static readonly DependencyProperty HasTextProperty =
            HasTextPropertyKey.DependencyProperty;

        public static readonly DependencyProperty UseThemedCaretProperty =
            DependencyProperty.RegisterAttached(
                "UseThemedCaret",
                typeof(bool),
                typeof(TextInput),
                new PropertyMetadata(false, OnUseThemedCaretChanged));

        public static readonly DependencyProperty IsClearButtonEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsClearButtonEnabled",
                typeof(bool),
                typeof(TextInput),
                new FrameworkPropertyMetadata(false, OnIsClearButtonEnabledChanged));

        /// <summary>
        /// Enables triple-click to select all text, matching the Win32 EDIT
        /// behavior.  WPF only implements triple-click paragraph selection for
        /// <see cref="RichTextBox"/> (<c>TextEditor.AcceptsRichContent</c>),
        /// so <see cref="TextBox"/> needs this opt-in to gain the gesture.
        /// </summary>
        public static readonly DependencyProperty IsTripleClickSelectAllEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsTripleClickSelectAllEnabled",
                typeof(bool),
                typeof(TextInput),
                new FrameworkPropertyMetadata(false, OnIsTripleClickSelectAllEnabledChanged));

        public static object GetWatermark(Control target) =>
            target.GetValue(WatermarkProperty);

        public static void SetWatermark(Control target, object value) =>
            target.SetValue(WatermarkProperty, value);

        public static bool GetHasText(Control target) =>
            (bool)target.GetValue(HasTextProperty);

        public static bool GetUseThemedCaret(Control target) =>
            (bool)target.GetValue(UseThemedCaretProperty);

        public static void SetUseThemedCaret(Control target, bool value) =>
            target.SetValue(UseThemedCaretProperty, value);

        public static bool GetIsClearButtonEnabled(Control target) =>
            (bool)target.GetValue(IsClearButtonEnabledProperty);

        public static void SetIsClearButtonEnabled(Control target, bool value) =>
            target.SetValue(IsClearButtonEnabledProperty, value);

        public static bool GetIsTripleClickSelectAllEnabled(Control target) =>
            (bool)target.GetValue(IsTripleClickSelectAllEnabledProperty);

        public static void SetIsTripleClickSelectAllEnabled(Control target, bool value) =>
            target.SetValue(IsTripleClickSelectAllEnabledProperty, value);

        private static void OnIsClearButtonEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            switch (target)
            {
                case TextBox textBox:
                    ConfigureClearButton(textBox, e.NewValue is true);
                    break;
                case PasswordBox passwordBox:
                    ConfigureClearButton(passwordBox, e.NewValue is true);
                    break;
            }
        }

        private static void OnIsTripleClickSelectAllEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not TextBox textBox)
                return;

            textBox.PreviewMouseLeftButtonDown -= OnTripleClickPreviewMouseLeftButtonDown;
            if (e.NewValue is true)
                textBox.PreviewMouseLeftButtonDown += OnTripleClickPreviewMouseLeftButtonDown;
        }

        private static void OnTripleClickPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Mirrors the WPF triple-click branch (TextEditorMouse), which
            // requires Shift to be up and only acts on the third click onward.
            if (e.ClickCount < 3 || (Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                return;

            if (sender is TextBox textBox)
                textBox.SelectAll();
        }

        private static void ConfigureClearButton(TextBox textBox, bool isEnabled)
        {
            textBox.Loaded -= OnClearButtonTextBoxLoaded;
            if (isEnabled)
            {
                textBox.Loaded += OnClearButtonTextBoxLoaded;
                if (textBox.IsLoaded)
                    AttachClearButton(textBox);
            }
            else
            {
                DetachClearButton(textBox);
            }
        }

        private static void ConfigureClearButton(PasswordBox passwordBox, bool isEnabled)
        {
            passwordBox.Loaded -= OnClearButtonPasswordBoxLoaded;
            if (isEnabled)
            {
                passwordBox.Loaded += OnClearButtonPasswordBoxLoaded;
                if (passwordBox.IsLoaded)
                    AttachClearButton(passwordBox);
            }
            else
            {
                DetachClearButton(passwordBox);
            }
        }

        private static void OnClearButtonTextBoxLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && GetIsClearButtonEnabled(textBox))
                AttachClearButton(textBox);
        }

        private static void OnClearButtonPasswordBoxLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && GetIsClearButtonEnabled(passwordBox))
                AttachClearButton(passwordBox);
        }

        private static void AttachClearButton(TextBox textBox)
        {
            textBox.ApplyTemplate();
            if (textBox.Template?.FindName("PART_ClearButton", textBox) is ButtonBase clearButton)
            {
                clearButton.Click -= OnClearTextButtonClick;
                clearButton.Click += OnClearTextButtonClick;
            }
        }

        private static void AttachClearButton(PasswordBox passwordBox)
        {
            passwordBox.ApplyTemplate();
            if (passwordBox.Template?.FindName("PART_ClearButton", passwordBox) is ButtonBase clearButton)
            {
                clearButton.Click -= OnClearPasswordButtonClick;
                clearButton.Click += OnClearPasswordButtonClick;
            }
        }

        private static void DetachClearButton(PasswordBox passwordBox)
        {
            if (passwordBox.Template?.FindName("PART_ClearButton", passwordBox) is ButtonBase clearButton)
                clearButton.Click -= OnClearPasswordButtonClick;
        }

        private static void DetachClearButton(TextBox textBox)
        {
            if (textBox.Template?.FindName("PART_ClearButton", textBox) is ButtonBase clearButton)
                clearButton.Click -= OnClearTextButtonClick;
        }

        private static void OnClearTextButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element &&
                element.TemplatedParent is TextBox textBox)
            {
                textBox.Clear();
            }
        }

        private static void OnClearPasswordButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element &&
                element.TemplatedParent is PasswordBox passwordBox)
            {
                PasswordReveal.Clear(passwordBox);
            }
        }

        private static void OnWatermarkChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not Control textInput)
                return;

            DetachTextStateHandlers(textInput);
            if (e.NewValue == null)
            {
                SetHasText(textInput, false);
                return;
            }

            AttachTextStateHandlers(textInput);
        }

        private static void DetachTextStateHandlers(Control textInput)
        {
            if (textInput is TextBox textBox)
            {
                textBox.Loaded -= OnTextInputLoaded;
                textBox.TextChanged -= OnTextChanged;
                return;
            }

            if (textInput is PasswordBox passwordBox)
            {
                passwordBox.Loaded -= OnTextInputLoaded;
                passwordBox.PasswordChanged -= OnPasswordChanged;
                return;
            }

            if (textInput is RichTextBox richTextBox)
            {
                richTextBox.Loaded -= OnTextInputLoaded;
                richTextBox.TextChanged -= OnTextChanged;

                return;
            }

            if (textInput is ComboBox comboBox)
            {
                comboBox.Loaded -= OnTextInputLoaded;
                comboBox.SelectionChanged -= OnComboBoxSelectionChanged;
                var descriptor = DependencyPropertyDescriptor.FromProperty(
                    ComboBox.TextProperty,
                    typeof(ComboBox));
                descriptor?.RemoveValueChanged(comboBox, OnComboBoxTextChanged);
            }
        }

        private static void AttachTextStateHandlers(Control textInput)
        {
            if (textInput is TextBox textBox)
            {
                textBox.Loaded -= OnTextInputLoaded;
                textBox.Loaded += OnTextInputLoaded;
                textBox.TextChanged -= OnTextChanged;
                textBox.TextChanged += OnTextChanged;
                UpdateHasText(textBox);
                return;
            }

            if (textInput is RichTextBox richTextBox)
            {
                richTextBox.Loaded -= OnTextInputLoaded;
                richTextBox.Loaded += OnTextInputLoaded;
                richTextBox.TextChanged -= OnTextChanged;
                richTextBox.TextChanged += OnTextChanged;
                UpdateHasText(richTextBox);
                return;
            }

            if (textInput is PasswordBox passwordBox)
            {
                passwordBox.Loaded -= OnTextInputLoaded;
                passwordBox.Loaded += OnTextInputLoaded;
                passwordBox.PasswordChanged -= OnPasswordChanged;
                passwordBox.PasswordChanged += OnPasswordChanged;
                UpdateHasText(passwordBox);
                return;
            }

            if (textInput is ComboBox comboBox)
            {
                comboBox.Loaded -= OnTextInputLoaded;
                comboBox.Loaded += OnTextInputLoaded;
                comboBox.SelectionChanged -= OnComboBoxSelectionChanged;
                comboBox.SelectionChanged += OnComboBoxSelectionChanged;

                var descriptor = DependencyPropertyDescriptor.FromProperty(
                    ComboBox.TextProperty,
                    typeof(ComboBox));
                descriptor?.RemoveValueChanged(comboBox, OnComboBoxTextChanged);
                descriptor?.AddValueChanged(comboBox, OnComboBoxTextChanged);

                UpdateHasText(comboBox);
            }
        }

        private static void OnTextInputLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Control textInput)
                UpdateHasText(textInput);
        }

        private static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Control textInput)
                UpdateHasText(textInput);
        }

        private static void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is Control textInput)
                UpdateHasText(textInput);
        }

        private static void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is Control textInput)
                UpdateHasText(textInput);
        }

        private static void OnComboBoxTextChanged(object sender, EventArgs e)
        {
            if (sender is Control textInput)
                UpdateHasText(textInput);
        }

        private static void UpdateHasText(Control textInput)
        {
            switch (textInput)
            {
                case TextBox textBox:
                    SetHasText(textBox, !string.IsNullOrEmpty(textBox.Text));
                    break;
                case PasswordBox passwordBox:
                    SetHasText(passwordBox, !string.IsNullOrEmpty(passwordBox.Password));
                    break;
                case RichTextBox richTextBox:
                    SetHasText(richTextBox, HasRichTextBoxContent(richTextBox));
                    break;
                case ComboBox comboBox:
                    SetHasText(
                        comboBox,
                        comboBox.SelectedItem != null || !string.IsNullOrEmpty(comboBox.Text));
                    break;
            }
        }

        private static bool HasRichTextBoxContent(RichTextBox richTextBox)
        {
            var document = richTextBox.Document;
            return document != null &&
                !StringCompat.IsNullOrWhiteSpace(
                    new TextRange(document.ContentStart, document.ContentEnd).Text);
        }

        private static void SetHasText(Control target, bool value) =>
            target.SetValue(HasTextPropertyKey, value);

        private static void OnUseThemedCaretChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not Control textInput ||
                (target is not TextBoxBase && target is not PasswordBox))
            {
                return;
            }

            textInput.Loaded -= OnThemedCaretTextInputLoaded;
            textInput.GotFocus -= OnThemedCaretTextInputGotFocus;
            var foregroundDescriptor = DependencyPropertyDescriptor.FromProperty(
                Control.ForegroundProperty,
                typeof(Control));
            foregroundDescriptor?.RemoveValueChanged(textInput, OnThemedCaretTextInputBrushChanged);

            if (e.NewValue is not true)
            {
                TextInputCaretAdornerManager.SetEnabled(textInput, false);
                return;
            }

            textInput.Loaded += OnThemedCaretTextInputLoaded;
            textInput.GotFocus += OnThemedCaretTextInputGotFocus;
            foregroundDescriptor?.AddValueChanged(textInput, OnThemedCaretTextInputBrushChanged);
            AttachTextStateHandlers(textInput);
            UpdateTextInputBrushes(textInput);
        }

        private static void OnThemedCaretTextInputLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Control textInput)
                UpdateTextInputBrushes(textInput);
        }

        private static void OnThemedCaretTextInputGotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is Control textInput)
                UpdateTextInputBrushes(textInput);
        }

        private static void OnThemedCaretTextInputBrushChanged(object sender, EventArgs e)
        {
            if (sender is Control textInput)
                UpdateTextInputBrushes(textInput);
        }

        private static void UpdateTextInputBrushes(Control textInput)
        {
            if (!textInput.Dispatcher.CheckAccess())
            {
                textInput.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    new Action(() => UpdateTextInputBrushes(textInput)));
                return;
            }

            var caretBrush = (Application.Current?.TryFindResource("TextDefault") as Brush)
                             ?? textInput.Foreground
                             ?? SystemColors.WindowTextBrush;
            var selectionBrush = (Application.Current?.TryFindResource("AccentSelectionBackground") as Brush)
                                 ?? caretBrush
                                 ?? SystemColors.HighlightBrush;
            var usesNativeCaretBrush = TextBoxBaseCompat.SetTextInputBrushes(
                textInput,
                caretBrush,
                selectionBrush,
                caretBrush);
            TextInputCaretAdornerManager.SetEnabled(textInput, !usesNativeCaretBrush);
        }
    }
}
