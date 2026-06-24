using System;
using System.IO;
using System.Linq;

namespace WinCraft.Compatibility
{
    /// <summary>
    /// Provides <see cref="Path"/> helpers that bridge gaps between framework variants.
    /// </summary>
    public static class PathCompat
    {
        /// <summary>
        /// Combines three path segments into a single path.
        /// .NET 3.0 only has the two-argument overload of <see cref="Path.Combine(string, string)"/>.
        /// </summary>
        public static string Combine(string path1, string path2, string path3)
        {
            return Path.Combine(Path.Combine(path1, path2), path3);
        }

        /// <summary>
        /// Combines four path segments into a single path.
        /// </summary>
        public static string Combine(string path1, string path2, string path3, string path4)
        {
            return Path.Combine(Path.Combine(Path.Combine(path1, path2), path3), path4);
        }

        /// <summary>
        /// Combines an arbitrary number of path segments into a single path.
        /// The fixed-arity overloads are preferred for common call sites to avoid
        /// the array allocation that <c>params</c> incurs.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="paths"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="paths"/> is empty.</exception>
        public static string Combine(params string[] paths)
        {
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));
            if (paths.Length == 0)
                throw new ArgumentException("Value cannot be empty.", nameof(paths));

            return paths.Aggregate(Path.Combine);
        }
    }
}
