using System;

namespace Windows.Win32
{
    /// <summary>
    /// Extension methods for CsWin32-generated char inline arrays.
    /// </summary>
    internal static class CharInlineArrayExtensions
    {
        /// <summary>
        /// Copies a managed string into a fixed-size char array, null-terminating.
        /// </summary>
        public static unsafe void SetString(this ref __char_260 field, string value)
        {
            if (value == null)
                return;

            int len = Math.Min(value.Length, (int)PInvoke.MAX_PATH - 1);
            for (int i = 0; i < len; i++)
                field[i] = value[i];
            field[len] = '\0';
        }
    }
}
