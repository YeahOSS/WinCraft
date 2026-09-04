using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WinCraft.UI
{
    /// <summary>
    /// A container that arranges <see cref="FormItem"/> children in a column,
    /// sharing a uniform label width across all rows.
    /// </summary>
    public class Form : ItemsControl
    {
        /// <summary>
        /// Inheritable attached property that controls the header-column width
        /// for all <see cref="FormItem"/> descendants.
        /// Set it on the <see cref="Form"/> element (or any ancestor) and every
        /// <see cref="FormItem"/> inside reads it automatically.
        /// An individual <see cref="FormItem"/> may override the inherited value
        /// by setting the same property locally.
        /// </summary>
        public static readonly DependencyProperty LabelWidthProperty =
            DependencyProperty.RegisterAttached(
                "LabelWidth",
                typeof(double),
                typeof(Form),
                new FrameworkPropertyMetadata(
                    double.NaN,
                    FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty SpacingProperty =
            SpacingPanel.SpacingProperty.AddOwner(
                typeof(Form),
                new FrameworkPropertyMetadata(0.0));

        /// <summary>
        /// Gets the inherited <see cref="LabelWidthProperty"/> value.
        /// </summary>
        public static double GetLabelWidth(DependencyObject obj) =>
            (double)obj.GetValue(LabelWidthProperty);

        /// <summary>
        /// Sets the <see cref="LabelWidthProperty"/> value on the given element.
        /// </summary>
        public static void SetLabelWidth(DependencyObject obj, double value) =>
            obj.SetValue(LabelWidthProperty, value);

        /// <summary>
        /// Convenience CLR wrapper — sets the label width on the Form itself,
        /// which then inherits to all child <see cref="FormItem"/> elements.
        /// </summary>
        public double LabelWidth
        {
            get => (double)GetValue(LabelWidthProperty);
            set => SetValue(LabelWidthProperty, value);
        }

        /// <summary>
        /// Vertical spacing between form rows.  Maps to the design-token
        /// system via <see cref="Spacing"/>.
        /// </summary>
        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        static Form()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(Form),
                new FrameworkPropertyMetadata(typeof(Form)));
        }

        public Form()
        {
            Focusable = false;
            ItemsPanel = CreateItemsPanel();
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item) =>
            item is FormItem;

        internal void ApplySpacing(Spacing spacing)
        {
            SpacingToken.Apply(this, spacing, SpacingProperty);
        }

        private static ItemsPanelTemplate CreateItemsPanel()
        {
            var panel = new FrameworkElementFactory(typeof(SpacingStackPanel));
            panel.SetBinding(
                SpacingPanel.SpacingProperty,
                new Binding(nameof(Spacing))
                {
                    RelativeSource = new RelativeSource(
                        RelativeSourceMode.FindAncestor,
                        typeof(Form),
                        1),
                });
            return new ItemsPanelTemplate(panel);
        }
    }
}
