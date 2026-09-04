using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace WinCraft.UI
{
    /// <summary>
    /// A binary toggle switch. Inherits <see cref="ToggleButton"/> but
    /// rejects content and three-state properties; the template omits
    /// the ContentPresenter.
    /// </summary>
    public class Switch : ToggleButton
    {
        static Switch()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(Switch),
                new FrameworkPropertyMetadata(typeof(Switch)));
            ContentProperty.OverrideMetadata(
                typeof(Switch),
                new FrameworkPropertyMetadata(null, OnRejectedPropertyChanged));
            ContentTemplateProperty.OverrideMetadata(
                typeof(Switch),
                new FrameworkPropertyMetadata(null, OnRejectedPropertyChanged));
            ContentTemplateSelectorProperty.OverrideMetadata(
                typeof(Switch),
                new FrameworkPropertyMetadata(null, OnRejectedPropertyChanged));
            ContentStringFormatProperty.OverrideMetadata(
                typeof(Switch),
                new FrameworkPropertyMetadata(null, OnRejectedPropertyChanged));
            IsThreeStateProperty.OverrideMetadata(
                typeof(Switch),
                new FrameworkPropertyMetadata(false, OnRejectedPropertyChanged));
        }

        private static void OnRejectedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (Equals(e.NewValue, e.OldValue))
                return; // initialization: false→false, null→null
            throw new InvalidOperationException(
                $"Switch does not support {e.Property.Name}.");
        }

        // ── Hidden at compile time via [Obsolete(error:true)] ──

        [Obsolete("Switch does not support Content.", true)]
        public new object Content
        {
            get => base.Content;
            set => base.Content = value;
        }

        [Obsolete("Switch does not support IsThreeState.", true)]
        public new bool IsThreeState
        {
            get => base.IsThreeState;
            set => base.IsThreeState = value;
        }

        [Obsolete("Switch does not support ContentTemplate.", true)]
        public new DataTemplate ContentTemplate
        {
            get => base.ContentTemplate;
            set => base.ContentTemplate = value;
        }
    }
}
