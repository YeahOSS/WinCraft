using System;
using System.IO;
using Microsoft.Win32;
using WinCraft.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace WinCraft.Infrastructure.FileSystem
{
    /// <summary>
    /// Resolves short executable names to fully qualified file paths.
    /// </summary>
    internal static class PathUtility
    {
        internal const string AppPathsRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";

        /// <summary>
        /// Returns the full path to a file or directory under the system default
        /// user profile directory (e.g. <c>C:\Users\Default\AppData\...</c>),
        /// obtained via <see cref="PInvoke.GetDefaultUserProfileDirectory"/>.
        /// Returns <c>null</c> if the default profile directory cannot be determined.
        /// </summary>
        internal static string GetDefaultProfilePath(string relativePath)
        {
            string root = GetDefaultProfileRoot();
            return root != null ? Path.Combine(root, relativePath) : null;
        }

        private static unsafe string GetDefaultProfileRoot()
        {
            uint cch = PInvoke.MAX_PATH;
            var buffer = new string('\0', (int)cch);
            fixed (char* pBuffer = buffer)
            {
                if (PInvoke.GetDefaultUserProfileDirectory(new PWSTR(pBuffer), ref cch))
                    return StringBuffer.ReadNullTerminatedFromString(buffer);
            }

            return null;
        }

        /// <summary>
        /// Resolves a file name or partial path to a fully qualified path.
        /// Returns the original value unchanged if it is already rooted,
        /// or null if the file cannot be located.
        /// </summary>
        public static string FindPath(string fileNameOrPath)
        {
            if (string.IsNullOrEmpty(fileNameOrPath))
                return null;

            // Already a rooted or relative path with directory component — don't search.
            if (Path.IsPathRooted(fileNameOrPath))
                return File.Exists(fileNameOrPath) ? fileNameOrPath : null;

            if (fileNameOrPath.IndexOf(Path.DirectorySeparatorChar) >= 0
                || fileNameOrPath.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            {
                return File.Exists(fileNameOrPath) ? Path.GetFullPath(fileNameOrPath) : null;
            }

            // Search the system PATH.
            string pathResult = SearchSystemPath(fileNameOrPath);
            if (pathResult != null)
                return pathResult;

            // Search App Paths registry.
            return SearchAppPaths(fileNameOrPath);
        }

        private static unsafe string SearchSystemPath(string fileName)
        {
            string fullPath = StringBuffer.HeapWrite(
                (int)PInvoke.MAX_PATH,
                p => PInvoke.SearchPath(null, fileName, null, PInvoke.MAX_PATH, p, null));

            return File.Exists(fullPath) ? fullPath : null;
        }

        private static string SearchAppPaths(string fileName)
        {
            string appPathsKey = Path.Combine(AppPathsRegistryKey, fileName);

            using (var key = Registry.LocalMachine.OpenSubKey(appPathsKey, writable: false))
            {
                string path = ReadAppPathValue(key);
                if (path != null && File.Exists(path))
                    return path;
            }

            using (var key = Registry.CurrentUser.OpenSubKey(appPathsKey, writable: false))
            {
                string path = ReadAppPathValue(key);
                if (path != null && File.Exists(path))
                    return path;
            }

            return null;
        }

        /// <summary>
        /// Truncates a path to at most <paramref name="maxChars"/> characters by
        /// inserting an ellipsis (…) at a directory boundary.  Returns the original
        /// string unchanged if no truncation is needed.
        /// </summary>
        public static unsafe string CompactPath(string path, uint maxChars)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= maxChars)
                return path;

            var buffer = new string('\0', (int)maxChars);
            fixed (char* pDst = buffer)
            {
                var bufPtr = new PWSTR(pDst);
                if (PInvoke.PathCompactPathEx(bufPtr, path, maxChars + 1, 0))
                    return StringBuffer.ReadNullTerminatedFromString(buffer);
            }

            return path;
        }

        /// <summary>
        /// Removes the trailing backslash from a path, if present.
        /// Returns the original string unchanged if the path does not end with <c>\</c>.
        /// </summary>
        internal static unsafe string RemoveBackslash(string path)
        {
            if (string.IsNullOrEmpty(path) || path[path.Length - 1] != '\\')
                return path;

            // PathRemoveBackslash modifies in-place; use StackWrite to get a mutable copy.
            return StringBuffer.StackWrite(path, p => PInvoke.PathRemoveBackslash(p));
        }

        /// <summary>
        /// Encloses a path in quotation marks if it contains spaces.
        /// Returns the original string unchanged otherwise.
        /// </summary>
        internal static unsafe string QuoteSpaces(string path)
        {
            if (string.IsNullOrEmpty(path) || path.IndexOf(' ') < 0)
                return path;

            return StringBuffer.HeapWrite(
                (int)PInvoke.MAX_PATH,
                p =>
                {
                    StringBuffer.CopyToBuffer(p.Value, path, (int)PInvoke.MAX_PATH);
                    PInvoke.PathQuoteSpaces(p);
                });
        }

        /// <summary>
        /// Replaces portions of a fully qualified path with environment-variable
        /// names (e.g. <c>%USERPROFILE%</c>).  Returns the original string if
        /// no substitution applies.
        /// </summary>
        internal static unsafe string UnExpandEnvStrings(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            return StringBuffer.HeapWrite(
                (int)PInvoke.MAX_PATH,
                p => PInvoke.PathUnExpandEnvStrings(path, p, PInvoke.MAX_PATH));
        }

        private static string ReadAppPathValue(RegistryKey key)
        {
            if (key == null)
                return null;

            string path = key.GetValue(null) as string;
            if (string.IsNullOrEmpty(path))
                path = key.GetValue("Path") as string;

            return string.IsNullOrEmpty(path) ? null : Environment.ExpandEnvironmentVariables(path);
        }
    }
}
