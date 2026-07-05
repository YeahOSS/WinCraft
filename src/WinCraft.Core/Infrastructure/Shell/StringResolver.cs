using WinCraft.Interop;
using Windows.Win32;

namespace WinCraft.Infrastructure.Shell
{
    /// <summary>
    /// Resolves indirect string references of the form @file,-resID
    /// and @{package}ms-resource://... used by shell registry values.
    /// </summary>
    internal static class StringResolver
    {
        /// <summary>
        /// Resolves <paramref name="rawText"/> if it is an indirect string reference.
        /// Returns the resolved text, or the original string if it is not a reference
        /// or resolution fails.
        /// </summary>
        public static string Resolve(string rawText)
        {
            if (string.IsNullOrEmpty(rawText) || rawText[0] != '@')
                return rawText;

            return TryResolve(rawText) ?? rawText;
        }

        private static string TryResolve(string rawText)
        {
            string result = StringBuffer.HeapWrite(
                (int)PInvoke.MAX_PATH,
                p => PInvoke.SHLoadIndirectString(rawText, p, PInvoke.MAX_PATH));

            if (string.IsNullOrEmpty(result))
                return null;

            return result;
        }
    }
}
