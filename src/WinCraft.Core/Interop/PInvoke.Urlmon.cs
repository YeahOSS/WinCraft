using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        private const string Urlmon = "urlmon.dll";

        /// <summary>
        /// Creates a COM format enumerator from an array of FORMATETC structures.
        /// </summary>
        [DllImport(Urlmon)]
        internal static extern void CreateFormatEnumerator(
            int cfmtetc, FORMATETC[] rgfmtetc, out IEnumFORMATETC enumfmtetc);

        /// <summary>
        /// Deep-copies one STGMEDIUM structure to another.
        /// </summary>
        [DllImport(Urlmon)]
        internal static extern int CopyStgMedium(
            ref STGMEDIUM pcstgmedSrc, ref STGMEDIUM pstgmedDest);
    }
}
