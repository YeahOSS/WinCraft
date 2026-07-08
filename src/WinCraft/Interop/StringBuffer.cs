using System;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace WinCraft.Interop
{
    /// <summary>
    /// Utilities for working with native string buffers — copying managed strings
    /// into fixed-size inline arrays, allocating writable PWSTR buffers for native
    /// APIs, and reading back null-terminated results.
    /// </summary>
    internal static class StringBuffer
    {
        // -- Write helpers (managed → native) --

        /// <summary>
        /// Copies a managed string into a CsWin32-generated fixed-size char inline
        /// array, null-terminating.  Truncates if <paramref name="value"/> exceeds
        /// <c>MAX_PATH - 1</c>.
        /// </summary>
        public static void CopyTo(ref __char_260 field, string value)
        {
            if (value == null)
                return;

            int len = Math.Min(value.Length, (int)PInvoke.MAX_PATH - 1);
            for (int i = 0; i < len; i++)
                field[i] = value[i];
            field[len] = '\0';
        }

        /// <summary>
        /// Copies <paramref name="value"/> into <paramref name="buf"/>, respecting
        /// <paramref name="maxLength"/> (including room for the null terminator).
        /// Always writes a null terminator at <c>buf[min(value.Length, maxLength - 1)]</c>.
        /// </summary>
        public static unsafe void CopyToBuffer(char* buf, string value, int maxLength)
        {
            int len = Math.Min(value.Length, maxLength - 1);
            for (int i = 0; i < len; i++)
                buf[i] = value[i];
            buf[len] = '\0';
        }

        /// <summary>
        /// Copies <paramref name="source"/> to a stack-allocated writable buffer,
        /// invokes <paramref name="write"/> with a pointer to it, then reads back
        /// the resulting null-terminated string.
        /// </summary>
        public static unsafe string StackWrite(string source, Action<PWSTR> write)
        {
            int len = source.Length;
            char* buffer = stackalloc char[len + 2];
            for (int i = 0; i < len; i++)
                buffer[i] = source[i];
            buffer[len] = '\0';

            write(new PWSTR(buffer));

            return ReadNullTerminated(buffer, len + 2);
        }

        /// <summary>
        /// Same as <see cref="StackWrite(string, Action{PWSTR})"/> but also
        /// captures the return value of <paramref name="write"/>.
        /// </summary>
        public static unsafe string StackWrite<T>(string source, Func<PWSTR, T> write, out T result)
        {
            int len = source.Length;
            char* buffer = stackalloc char[len + 2];
            for (int i = 0; i < len; i++)
                buffer[i] = source[i];
            buffer[len] = '\0';

            result = write(new PWSTR(buffer));

            return ReadNullTerminated(buffer, len + 2);
        }

        /// <summary>
        /// Allocates a managed string of <paramref name="length"/> chars, passes a
        /// writable pointer to <paramref name="write"/>, then returns the resulting
        /// null-terminated content.
        /// </summary>
        public static unsafe string HeapWrite(int length, Action<PWSTR> write)
        {
            var buffer = new string('\0', length);
            fixed (char* pBuffer = buffer)
            {
                write(new PWSTR(pBuffer));
            }

            return ReadNullTerminatedFromString(buffer);
        }

        // -- Read helpers (native → managed) --

        /// <summary>
        /// Reads a null-terminated wide-char string from a buffer.
        /// </summary>
        public static unsafe string ReadNullTerminated(char* buffer, int maxLength)
        {
            int end = 0;
            while (end < maxLength && buffer[end] != '\0')
                end++;
            return new string(buffer, 0, end);
        }

        /// <summary>
        /// Reads the prefix up to the first '\0' from a managed string used as a
        /// native output buffer.
        /// </summary>
        public static string ReadNullTerminatedFromString(string buffer)
        {
            int end = buffer.IndexOf('\0');
            return end >= 0 ? buffer.Substring(0, end) : buffer;
        }
    }
}
