using System;
using System.Runtime.InteropServices;

namespace Windows.Win32.UI.Shell
{
    /// <summary>
    /// Simplified PROPVARIANT for scalar property values.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal struct PROPVARIANT
    {
        // Native VARTYPE is ushort (2 bytes). wReserved1/2/3 are each ushort.
        [FieldOffset(0)]
        internal ushort vt;

        [FieldOffset(2)]
        internal ushort wReserved1;

        [FieldOffset(4)]
        internal ushort wReserved2;

        [FieldOffset(6)]
        internal ushort wReserved3;

        [FieldOffset(8)]
        internal byte bVal;

        [FieldOffset(8)]
        internal sbyte cVal;

        [FieldOffset(8)]
        internal ushort uiVal;

        [FieldOffset(8)]
        internal short iVal;

        [FieldOffset(8)]
        internal uint uintVal;

        [FieldOffset(8)]
        internal int intVal;

        [FieldOffset(8)]
        internal ulong ulVal;

        [FieldOffset(8)]
        internal long lVal;

        [FieldOffset(8)]
        internal float fltVal;

        [FieldOffset(8)]
        internal double dblVal;

        [FieldOffset(8)]
        internal short boolVal;

        [FieldOffset(8)]
        internal IntPtr pclsidVal;

        [FieldOffset(8)]
        internal IntPtr pszVal;

        [FieldOffset(8)]
        internal IntPtr pwszVal;

        [FieldOffset(8)]
        internal IntPtr punkVal;

        [FieldOffset(8)]
        internal IntPtr ca;

        /// <summary>
        /// Releases resources owned by this PROPVARIANT. Call after <see cref="IPropertyStore.SetValue"/>
        /// or <see cref="IPropertyStore.GetValue"/> when the variant holds allocated memory.
        /// Safe to call on a default-initialized variant.
        /// </summary>
        internal void Clear()
        {
            if ((vt == (ushort)VarEnum.VT_LPWSTR || vt == (ushort)VarEnum.VT_LPSTR || vt == (ushort)VarEnum.VT_BSTR)
                && pwszVal != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pwszVal);
                pwszVal = IntPtr.Zero;
            }
            this = default;
        }

        internal static PROPVARIANT FromObject(object value)
        {
            PROPVARIANT pv = default;

            if (value is int i)
            {
                pv.vt = (ushort)VarEnum.VT_I4;
                pv.intVal = i;
            }
            else if (value is uint u)
            {
                pv.vt = (ushort)VarEnum.VT_UI4;
                pv.uintVal = u;
            }
            else if (value is long l)
            {
                pv.vt = (ushort)VarEnum.VT_I8;
                pv.lVal = l;
            }
            else if (value is ulong ul)
            {
                pv.vt = (ushort)VarEnum.VT_UI8;
                pv.ulVal = ul;
            }
            else if (value is short s)
            {
                pv.vt = (ushort)VarEnum.VT_I2;
                pv.iVal = s;
            }
            else if (value is ushort us)
            {
                pv.vt = (ushort)VarEnum.VT_UI2;
                pv.uiVal = us;
            }
            else if (value is double d)
            {
                pv.vt = (ushort)VarEnum.VT_R8;
                pv.dblVal = d;
            }
            else if (value is string str)
            {
                pv.vt = (ushort)VarEnum.VT_LPWSTR;
                pv.pwszVal = Marshal.StringToCoTaskMemAuto(str);
            }
            else if (value is bool b)
            {
                pv.vt = (ushort)VarEnum.VT_BOOL;
                pv.boolVal = b ? (short)-1 : (short)0;
            }
            else
            {
                throw new NotSupportedException(
                    string.Format("{0} does not support values of type {1}.", nameof(PROPVARIANT), value.GetType()));
            }

            return pv;
        }
    }
}
