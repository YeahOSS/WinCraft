namespace WinCraft.UI.Mvvm
{
    internal static class CommandParameter
    {
        public static bool TryGet<T>(object parameter, out T value)
        {
            // Direct type match — covers non-null reference types and value types.
            if (parameter is T typedValue)
            {
                value = typedValue;
                return true;
            }

            // Null parameter is valid for reference types and Nullable<T>.
            if (parameter == null && default(T) == null)
            {
                value = default;
                return true;
            }

            value = default;
            return false;
        }
    }
}
