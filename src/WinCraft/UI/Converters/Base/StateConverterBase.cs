using System;
using System.Globalization;
using WinCraft.Compatibility;

namespace WinCraft.UI
{
    public abstract class StateConverterBase(Func<object, bool> checkStateFunc, object trueState, object falseState) : ValueConverterBase
    {
        private readonly Func<object, bool> _checkStateFunc = checkStateFunc;
        private readonly object _trueState = trueState;
        private readonly object _falseState = falseState;

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return _checkStateFunc(value) ? _trueState : _falseState;
        }

        protected static bool IsTrue(object value) => value is true;

        protected static bool IsFalse(object value) => value is false;

        protected static bool IsNull(object value) => value is null;

        protected static bool IsNullOrEmpty(object value) => value is null || (value is string str && string.IsNullOrEmpty(str));

        protected static bool IsNullOrWhiteSpace(object value) => value is null || (value is string str && StringCompat.IsNullOrWhiteSpace(str));
    }
}
