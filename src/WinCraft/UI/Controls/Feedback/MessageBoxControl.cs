using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WinCraft.UI
{
    /// <summary>
    /// Themed message box content control. Assign properties directly, then host
    /// inside a <see cref="ChromeWindow"/> and call <c>ShowDialog()</c>.
    /// The window title bar carries the dialog title.
    /// </summary>
    public class MessageBoxControl : Control
    {
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(MessageBoxControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(IconGlyph), typeof(MessageBoxControl),
                new PropertyMetadata(IconGlyph.None));

        public static readonly DependencyProperty IsIconVisibleProperty =
            DependencyProperty.Register(nameof(IsIconVisible), typeof(bool), typeof(MessageBoxControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty VisualRoleProperty =
            DependencyProperty.Register(nameof(VisualRole), typeof(VisualRole), typeof(MessageBoxControl),
                new PropertyMetadata(VisualRole.Info));

        public static readonly DependencyProperty AdditionalContentProperty =
            DependencyProperty.Register(nameof(AdditionalContent), typeof(object), typeof(MessageBoxControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty AdditionalContentTemplateProperty =
            DependencyProperty.Register(nameof(AdditionalContentTemplate), typeof(DataTemplate), typeof(MessageBoxControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty CheckBoxTextProperty =
            DependencyProperty.Register(nameof(CheckBoxText), typeof(string), typeof(MessageBoxControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsCheckBoxCheckedProperty =
            DependencyProperty.Register(nameof(IsCheckBoxChecked), typeof(bool), typeof(MessageBoxControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty AcceptTextProperty =
            DependencyProperty.Register(nameof(AcceptText), typeof(string), typeof(MessageBoxControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty AcceptCommandProperty =
            DependencyProperty.Register(nameof(AcceptCommand), typeof(ICommand), typeof(MessageBoxControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty CloseTextProperty =
            DependencyProperty.Register(nameof(CloseText), typeof(string), typeof(MessageBoxControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsCloseVisibleProperty =
            DependencyProperty.Register(nameof(IsCloseVisible), typeof(bool), typeof(MessageBoxControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty CloseCommandProperty =
            DependencyProperty.Register(nameof(CloseCommand), typeof(ICommand), typeof(MessageBoxControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ThirdTextProperty =
            DependencyProperty.Register(nameof(ThirdText), typeof(string), typeof(MessageBoxControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsThirdVisibleProperty =
            DependencyProperty.Register(nameof(IsThirdVisible), typeof(bool), typeof(MessageBoxControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty ThirdCommandProperty =
            DependencyProperty.Register(nameof(ThirdCommand), typeof(ICommand), typeof(MessageBoxControl),
                new PropertyMetadata(null));

        static MessageBoxControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(MessageBoxControl),
                new FrameworkPropertyMetadata(typeof(MessageBoxControl)));
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public IconGlyph Icon
        {
            get => (IconGlyph)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool IsIconVisible
        {
            get => (bool)GetValue(IsIconVisibleProperty);
            set => SetValue(IsIconVisibleProperty, value);
        }

        public VisualRole VisualRole
        {
            get => (VisualRole)GetValue(VisualRoleProperty);
            set => SetValue(VisualRoleProperty, value);
        }

        public object AdditionalContent
        {
            get => GetValue(AdditionalContentProperty);
            set => SetValue(AdditionalContentProperty, value);
        }

        public DataTemplate AdditionalContentTemplate
        {
            get => (DataTemplate)GetValue(AdditionalContentTemplateProperty);
            set => SetValue(AdditionalContentTemplateProperty, value);
        }

        public string CheckBoxText
        {
            get => (string)GetValue(CheckBoxTextProperty);
            set => SetValue(CheckBoxTextProperty, value);
        }

        public bool IsCheckBoxChecked
        {
            get => (bool)GetValue(IsCheckBoxCheckedProperty);
            set => SetValue(IsCheckBoxCheckedProperty, value);
        }

        public string AcceptText
        {
            get => (string)GetValue(AcceptTextProperty);
            set => SetValue(AcceptTextProperty, value);
        }

        public ICommand AcceptCommand
        {
            get => (ICommand)GetValue(AcceptCommandProperty);
            set => SetValue(AcceptCommandProperty, value);
        }

        public string CloseText
        {
            get => (string)GetValue(CloseTextProperty);
            set => SetValue(CloseTextProperty, value);
        }

        public bool IsCloseVisible
        {
            get => (bool)GetValue(IsCloseVisibleProperty);
            set => SetValue(IsCloseVisibleProperty, value);
        }

        public ICommand CloseCommand
        {
            get => (ICommand)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        public string ThirdText
        {
            get => (string)GetValue(ThirdTextProperty);
            set => SetValue(ThirdTextProperty, value);
        }

        public bool IsThirdVisible
        {
            get => (bool)GetValue(IsThirdVisibleProperty);
            set => SetValue(IsThirdVisibleProperty, value);
        }

        public ICommand ThirdCommand
        {
            get => (ICommand)GetValue(ThirdCommandProperty);
            set => SetValue(ThirdCommandProperty, value);
        }

        /// <summary>
        /// Result of the message box, set when a button is clicked.
        /// </summary>
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        /// <summary>
        /// Raised when the user clicks any button and the host window should close.
        /// </summary>
        public event Action RequestClose;

        /// <summary>
        /// Configures icon, role, and buttons based on the supplied parameters.
        /// Call once after setting <see cref="Message"/>.
        /// </summary>
        public void Configure(MessageBoxButton button, MessageBoxImage icon, VisualRole role)
        {
            if (icon != MessageBoxImage.None)
                role = IconToRole(icon);

            Icon = GetIconGlyph(icon);
            IsIconVisible = icon != MessageBoxImage.None;
            VisualRole = role;

            switch (button)
            {
                case MessageBoxButton.OK:
                    AcceptText = "OK";
                    AcceptCommand = BuildCommand(MessageBoxResult.OK);
                    break;

                case MessageBoxButton.OKCancel:
                    AcceptText = "OK";
                    AcceptCommand = BuildCommand(MessageBoxResult.OK);
                    CloseText = "Cancel";
                    IsCloseVisible = true;
                    CloseCommand = BuildCommand(MessageBoxResult.Cancel);
                    break;

                case MessageBoxButton.YesNo:
                    AcceptText = "Yes";
                    AcceptCommand = BuildCommand(MessageBoxResult.Yes);
                    CloseText = "No";
                    IsCloseVisible = true;
                    CloseCommand = BuildCommand(MessageBoxResult.No);
                    break;

                case MessageBoxButton.YesNoCancel:
                    AcceptText = "Yes";
                    AcceptCommand = BuildCommand(MessageBoxResult.Yes);
                    ThirdText = "No";
                    IsThirdVisible = true;
                    ThirdCommand = BuildCommand(MessageBoxResult.No);
                    CloseText = "Cancel";
                    IsCloseVisible = true;
                    CloseCommand = BuildCommand(MessageBoxResult.Cancel);
                    break;
            }
        }

        private ICommand BuildCommand(MessageBoxResult result)
        {
            return new RelayCommand(() =>
            {
                Result = result;
                RequestClose?.Invoke();
            });
        }

        internal static IconGlyph GetIconGlyph(MessageBoxImage icon)
        {
            return icon switch
            {
                MessageBoxImage.Error => IconGlyph.ErrorCircle24,
                MessageBoxImage.Warning => IconGlyph.Warning24,
                MessageBoxImage.Question => IconGlyph.QuestionCircle24,
                MessageBoxImage.Information => IconGlyph.Info24,
                _ => IconGlyph.None,
            };
        }

        private static VisualRole IconToRole(MessageBoxImage icon)
        {
            return icon switch
            {
                MessageBoxImage.Error => VisualRole.Error,
                MessageBoxImage.Warning => VisualRole.Warning,
                _ => VisualRole.Info,
            };
        }
    }
}
