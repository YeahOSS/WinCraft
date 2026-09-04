using System;
using System.Runtime.InteropServices;

namespace WinCraft.Compatibility
{
    internal static class MarshalCompat
    {
        /// <summary>
        /// Marshals data from an unmanaged block of memory to a managed object of type <typeparamref name="T"/>.
        /// Uses the non-generic <see cref="Marshal.PtrToStructure(IntPtr, Type)"/> on all target frameworks
        /// because the generic overload was introduced in .NET 4.5.1, which is newer than net45.
        /// </summary>
        internal static T PtrToStructure<T>(IntPtr ptr) where T : struct
        {
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
        }

        /// <summary>
        /// Marshals data from a managed object to an unmanaged block of memory.
        /// </summary>
        internal static void StructureToPtr<T>(T structure, IntPtr ptr, bool fDeleteOld) where T : struct
        {
            Marshal.StructureToPtr(structure, ptr, fDeleteOld);
        }
    }
}
