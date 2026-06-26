using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace WinCraft.UI.Mvvm
{
    /// <summary>
    /// Lightweight base class for ViewModels. Provides dictionary-backed
    /// <see cref="INotifyPropertyChanged"/> support so that derived types can
    /// declare properties with minimal boilerplate.
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        /// <summary>
        /// Returns true when the designer is active. ViewModels can use this
        /// to supply design-time data.
        /// </summary>
        public static bool IsInDesignMode { get; } =
            DesignerProperties.IsInDesignModeProperty
                .GetMetadata(typeof(DependencyObject))
                .DefaultValue is true;

        private readonly Dictionary<string, object> _propertyValues = [];
        private readonly object _lock = new();

        public event PropertyChangedEventHandler PropertyChanged;

        protected T GetValue<T>([CallerMemberName] string propertyName = null)
        {
            lock (_lock)
            {
                return _propertyValues.TryGetValue(propertyName, out object value) && value is T result
                    ? result
                    : default;
            }
        }

        protected void SetValue<T>(T value, [CallerMemberName] string propertyName = null)
        {
            bool changed = false;
            lock (_lock)
            {
                if (!_propertyValues.TryGetValue(propertyName, out object oldValue)
                    || !(oldValue is T typedOld && EqualityComparer<T>.Default.Equals(typedOld, value)))
                {
                    _propertyValues[propertyName] = value;
                    changed = true;
                }
            }

            if (changed)
            {
                RaisePropertyChanged(propertyName);
            }
        }

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for the calling property.
        /// Also useful for computed properties that do not go through
        /// <see cref="SetValue{T}"/>.
        /// </summary>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
