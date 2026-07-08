using System;

namespace WinCraft.Compatibility
{
    /// <summary>
    /// Provides <see cref="Enum"/> helpers that bridge gaps between framework variants.
    /// </summary>
    public static class EnumCompat
    {
        /// <summary>
        /// Converts the string representation of an enum value to its equivalent
        /// enumerated object, using case-insensitive comparison when requested.
        /// net30 lacks the generic <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> overload.
        /// </summary>
        public static bool TryParse<TEnum>(string value, bool ignoreCase, out TEnum result) where TEnum : struct
        {
#if NET45
            return Enum.TryParse(value, ignoreCase, out result);
#else
            result = default;
            if (string.IsNullOrEmpty(value))
                return false;

            try
            {
                result = (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
#endif
        }
    }
}
