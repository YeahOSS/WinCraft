using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WinCraft.UI
{
    public static class PasswordReveal
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(PasswordReveal),
                new FrameworkPropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty IsRevealedProperty =
            DependencyProperty.RegisterAttached(
                "IsRevealed",
                typeof(bool),
                typeof(PasswordReveal),
                new FrameworkPropertyMetadata(false, OnIsRevealedChanged));

        private static readonly DependencyProperty RevealTextProperty =
            DependencyProperty.RegisterAttached(
                "RevealText",
                typeof(string),
                typeof(PasswordReveal),
                new FrameworkPropertyMetadata(string.Empty, OnRevealTextChanged));

        private static readonly DependencyProperty IsSynchronizingProperty =
            DependencyProperty.RegisterAttached(
                "IsSynchronizing",
                typeof(bool),
                typeof(PasswordReveal),
                new PropertyMetadata(false));

        private static readonly DependencyProperty OwnerProperty =
            DependencyProperty.RegisterAttached(
                "Owner",
                typeof(PasswordBox),
                typeof(PasswordReveal),
                new PropertyMetadata(null));

        public static bool GetIsEnabled(PasswordBox target) =>
            (bool)target.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(PasswordBox target, bool value) =>
            target.SetValue(IsEnabledProperty, value);

        public static bool GetIsRevealed(PasswordBox target) =>
            (bool)target.GetValue(IsRevealedProperty);

        public static void SetIsRevealed(PasswordBox target, bool value) =>
            target.SetValue(IsRevealedProperty, value);

        internal static void Clear(PasswordBox passwordBox)
        {
            Synchronize(
                passwordBox,
                () =>
                {
                    passwordBox.Password = string.Empty;
                    SetRevealText(passwordBox, string.Empty);
                });
            UpdateRevealedTextBox(passwordBox);
        }

        private static void OnIsEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not PasswordBox passwordBox)
                return;

            passwordBox.PasswordChanged -= OnPasswordChanged;
            if (e.NewValue is true)
            {
                passwordBox.PasswordChanged += OnPasswordChanged;
                Synchronize(
                    passwordBox,
                    () => SetRevealText(passwordBox, passwordBox.Password));
                return;
            }

            SetIsRevealed(passwordBox, false);
            ClearRevealedTextBox(passwordBox);
            SetRevealText(passwordBox, string.Empty);
        }

        private static void OnIsRevealedChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not PasswordBox passwordBox)
                return;

            if (!GetIsEnabled(passwordBox))
            {
                if (e.NewValue is true)
                    SetIsRevealed(passwordBox, false);

                return;
            }

            if (e.NewValue is true)
                UpdateRevealedTextBox(passwordBox);

            if (!passwordBox.IsKeyboardFocusWithin)
                return;

            passwordBox.Dispatcher.BeginInvoke(
                DispatcherPriority.Input,
                new Action(() => FocusPasswordInput(passwordBox)));
        }

        private static void OnRevealTextChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not PasswordBox passwordBox ||
                !GetIsEnabled(passwordBox) ||
                (bool)passwordBox.GetValue(IsSynchronizingProperty))
            {
                return;
            }

            Synchronize(
                passwordBox,
                () => passwordBox.Password = (string)e.NewValue ?? string.Empty);
            UpdateRevealedTextBox(passwordBox);
        }

        private static void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox passwordBox ||
                (bool)passwordBox.GetValue(IsSynchronizingProperty))
            {
                return;
            }

            Synchronize(
                passwordBox,
                () => SetRevealText(passwordBox, passwordBox.Password));

            if (GetIsRevealed(passwordBox))
                UpdateRevealedTextBox(passwordBox);
        }

        private static void FocusPasswordInput(PasswordBox passwordBox)
        {
            if (!GetIsEnabled(passwordBox) || !passwordBox.IsKeyboardFocusWithin)
                return;

            if (!GetIsRevealed(passwordBox))
            {
                passwordBox.Focus();
                return;
            }

            var revealedTextBox = GetRevealedTextBox(passwordBox);
            if (revealedTextBox == null)
                return;

            UpdateRevealedTextBox(passwordBox);
            revealedTextBox.Focus();
        }

        private static TextBox GetRevealedTextBox(PasswordBox passwordBox)
        {
            passwordBox.ApplyTemplate();
            if (passwordBox.Template?.FindName(
                "PART_RevealedTextBox",
                passwordBox) is not TextBox revealedTextBox)
            {
                return null;
            }

            revealedTextBox.SetValue(OwnerProperty, passwordBox);
            revealedTextBox.TextChanged -= OnRevealedTextBoxTextChanged;
            revealedTextBox.TextChanged += OnRevealedTextBoxTextChanged;
            return revealedTextBox;
        }

        private static void UpdateRevealedTextBox(PasswordBox passwordBox)
        {
            var revealedTextBox = GetRevealedTextBox(passwordBox);
            if (revealedTextBox == null || revealedTextBox.Text == passwordBox.Password)
                return;

            Synchronize(passwordBox, () => revealedTextBox.Text = passwordBox.Password);
        }

        private static void ClearRevealedTextBox(PasswordBox passwordBox)
        {
            var revealedTextBox = GetRevealedTextBox(passwordBox);
            if (revealedTextBox == null)
                return;

            revealedTextBox.TextChanged -= OnRevealedTextBoxTextChanged;
            revealedTextBox.ClearValue(OwnerProperty);
            revealedTextBox.Clear();
        }

        private static void OnRevealedTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox revealedTextBox ||
                revealedTextBox.GetValue(OwnerProperty) is not PasswordBox passwordBox ||
                !GetIsEnabled(passwordBox) ||
                (bool)passwordBox.GetValue(IsSynchronizingProperty))
            {
                return;
            }

            SetRevealText(passwordBox, revealedTextBox.Text);
        }

        private static void Synchronize(PasswordBox passwordBox, Action action)
        {
            var wasSynchronizing = (bool)passwordBox.GetValue(IsSynchronizingProperty);
            passwordBox.SetValue(IsSynchronizingProperty, true);
            try
            {
                action();
            }
            finally
            {
                passwordBox.SetValue(IsSynchronizingProperty, wasSynchronizing);
            }
        }

        private static void SetRevealText(PasswordBox target, string value) =>
            target.SetValue(RevealTextProperty, value ?? string.Empty);
    }
}
