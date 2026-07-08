using System;
using System.Globalization;
using System.Linq;

namespace WinCraft.UI
{
    public abstract class MultiStateConverterBase(Func<object[], bool> combiner, object trueState, object falseState) : MultiValueConverterBase
    {
        private readonly Func<object[], bool> _combiner = combiner;
        private readonly object _trueState = trueState;
        private readonly object _falseState = falseState;

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return _combiner(values) ? _trueState : _falseState;
        }

        protected static bool AllTrue(object[] values) => values.All(v => v is true);

        protected static bool AnyTrue(object[] values) => values.Any(v => v is true);

        protected static bool AllFalse(object[] values) => values.All(v => v is false);

        protected static bool AnyFalse(object[] values) => values.Any(v => v is false);
    }
}
