using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WinCraft.UI
{
    public class MessageBar : ContentControl
    {
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(MessageBar),
                new FrameworkPropertyMetadata(true, OnIsOpenChanged));

        public static readonly DependencyProperty IsClosableProperty =
            DependencyProperty.Register(
                nameof(IsClosable),
                typeof(bool),
                typeof(MessageBar),
                new PropertyMetadata(true));

        public static readonly DependencyProperty CloseCommandProperty =
            DependencyProperty.Register(
                nameof(CloseCommand),
                typeof(ICommand),
                typeof(MessageBar),
                new PropertyMetadata(null));

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public bool IsClosable
        {
            get => (bool)GetValue(IsClosableProperty);
            set => SetValue(IsClosableProperty, value);
        }

        public ICommand CloseCommand
        {
            get => (ICommand)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        static MessageBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(MessageBar),
                new FrameworkPropertyMetadata(typeof(MessageBar)));
        }

        public MessageBar()
        {
            CloseCommand = new RelayCommand(Close);
        }

        private void Close()
        {
            IsOpen = false;
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bar = (MessageBar)d;
            bar.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
