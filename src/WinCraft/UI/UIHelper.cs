using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WinCraft.Infrastructure;

namespace WinCraft.UI
{
    public static class UIHelper
    {
        public static bool IsInDesignMode { get; } =
            DesignerProperties.IsInDesignModeProperty
                .GetMetadata(typeof(DependencyObject))
                .DefaultValue is true;

        public static bool RunOnUI(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted)
                return false;

            dispatcher.BeginInvoke(DispatcherPriority.Background, action);
            return true;
        }

        /// <summary>
        /// Restores WPF's submenu delay when a zero system delay makes nested
        /// menus close before the pointer can reach them.
        /// </summary>
        public static void FixMenuShowDelay()
        {
            _ = SystemParameters.MenuShowDelay; // refresh the cached value
            typeof(SystemParameters).TrySetNonPublicStaticField("_menuShowDelay", 0);
        }

        // ── Window ──

        /// <summary>
        /// Returns the best window to use as the <see cref="Window.Owner"/>
        /// for a window that is about to be shown.
        /// </summary>
        /// <remarks>
        /// Picks the active window first, then the last visible window,
        /// falling back to <see cref="Application.MainWindow"/>.  This is
        /// meant for async scenarios (e.g. a dialog shown from a background
        /// task via <c>Dispatcher.BeginInvoke</c>) where the calling context
        /// has no natural owner reference and the active window may have
        /// changed by the time the callback runs.
        /// </remarks>
        public static Window GetBestOwner()
        {
            var app = Application.Current;
            if (app == null)
                return null;

            var windows = app.Windows.OfType<Window>();
            return windows.FirstOrDefault(w => w.IsActive)
                ?? windows.LastOrDefault(w => w.IsVisible)
                ?? app.MainWindow;
        }

        // ── Visual tree ──

        public static T FindVisualChild<T>(this DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }

        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    yield return typedChild;

                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        public static T FindVisualAncestor<T>(this DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T result)
                    return result;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="descendant"/> is a visual
        /// or logical descendant of <paramref name="ancestor"/>.  Walks both
        /// trees so it works across Popup boundaries where the visual parent
        /// chain breaks but the logical tree is still connected.
        /// </summary>
        public static bool IsDescendant(this DependencyObject ancestor, DependencyObject descendant)
        {
            if (descendant == null) return false;
            var parent = descendant;
            while (parent != null)
            {
                if (parent == ancestor) return true;
                parent = VisualTreeHelper.GetParent(parent)
                         ?? LogicalTreeHelper.GetParent(parent);
            }
            return false;
        }

        // ── Hit testing ──

        /// <summary>
        /// Determines whether <paramref name="point"/> is inside the element.
        /// Works even when <see cref="UIElement.IsEnabled"/> is <c>false</c>,
        /// <see cref="UIElement.IsHitTestVisible"/> is <c>false</c>, or
        /// <see cref="UIElement.Visibility"/> is <see cref="Visibility.Hidden"/>.
        /// </summary>
        public static bool ContainsPoint(this FrameworkElement element, Point point)
        {
            return point.X >= 0 && point.X <= element.ActualWidth
                && point.Y >= 0 && point.Y <= element.ActualHeight;
        }

        public static bool ContainsPoint(this FrameworkElement element, MouseEventArgs e)
        {
            return element.ContainsPoint(e.GetPosition(element));
        }

        public static bool ContainsPoint(this FrameworkElement element, DragEventArgs e)
        {
            return element.ContainsPoint(e.GetPosition(element));
        }

        // ── ItemsControl ──

        public static T GetVisualItem<T>(this ItemsControl control, int index) where T : DependencyObject
        {
            return control.ItemContainerGenerator.ContainerFromIndex(index) as T;
        }

        public static T GetVisualItem<T>(this ItemsControl control, object item) where T : DependencyObject
        {
            return control.ItemContainerGenerator.ContainerFromItem(item) as T;
        }
    }
}
