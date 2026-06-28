using System;

namespace WinCraft.Compatibility
{
    /// <summary>
    /// Provides <see cref="Guid"/> helpers that bridge gaps between framework variants.
    /// </summary>
    public static class GuidCompat
    {
        /// <summary>
        /// Converts the string representation of a GUID to its equivalent
        /// <see cref="Guid"/> structure. net30 lacks <see cref="Guid.TryParse(string, out Guid)"/>.
        /// </summary>
        public static bool TryParse(string input, out Guid result)
        {
#if NET45
            return Guid.TryParse(input, out result);
#else
            result = Guid.Empty;
            if (input == null)
                return false;

            try
            {
                result = new Guid(input);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
#endif
        }
    }
}
