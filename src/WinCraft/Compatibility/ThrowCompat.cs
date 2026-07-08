using System;

namespace WinCraft.Compatibility
{
    /// <summary>
    /// Null-argument guard compatible with net30.
    /// </summary>
    public static class ThrowCompat
    {
        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if <paramref name="argument"/>
        /// is null.  Returns the non-null value for inline use.
        /// </summary>
        public static T IfNull<T>(T argument, string paramName)
            where T : class
        {
            if (argument == null)
                throw new ArgumentNullException(paramName);
            return argument;
        }
    }
}
