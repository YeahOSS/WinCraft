using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        private const string Ole32 = "ole32.dll";

        /// <summary>
        /// Frees the storage medium and releases associated resources.
        /// </summary>
        [DllImport(Ole32)]
        internal static extern void ReleaseStgMedium(ref STGMEDIUM pmedium);
    }
}
