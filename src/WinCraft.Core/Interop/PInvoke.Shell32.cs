using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        private const string Shell32 = "shell32.dll";

        /// <summary>
        /// Retrieves the path to the default user profile directory.
        /// </summary>
        /// <remarks>
        /// GetDefaultUserProfileDirectory is absent from win32metadata.
        /// </remarks>
        [DllImport(Shell32, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool GetDefaultUserProfileDirectory(PWSTR lpProfileDir, ref uint lpcchSize);

        /// <summary>
        /// Parses a command-line string into an array of arguments
        /// using the same rules as the Windows command-line parser.
        /// </summary>
        /// <remarks>
        /// CommandLineToArgvW is absent from win32metadata.
        /// The returned array is allocated with <c>LocalAlloc</c> and must be freed
        /// with <see cref="Marshal.FreeHGlobal"/>.
        /// </remarks>
        [DllImport(Shell32, ExactSpelling = true, SetLastError = true)]
        internal static extern unsafe char** CommandLineToArgvW(char* lpCmdLine, int* pNumArgs);

        /// <summary>
        /// Managed wrapper around <see cref="CommandLineToArgvW"/>.
        /// Returns an empty array on failure.
        /// </summary>
        internal static unsafe string[] CommandLineToArgv(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine))
                return [];

            fixed (char* pCmdLine = commandLine)
            {
                int numArgs = 0;
                char** argv = CommandLineToArgvW(pCmdLine, &numArgs);
                if (argv == null || numArgs <= 0)
                    return [];

                try
                {
                    var result = new string[numArgs];
                    for (int i = 0; i < numArgs; i++)
                        result[i] = new string(argv[i]);
                    return result;
                }
                finally
                {
                    Marshal.FreeHGlobal((IntPtr)argv);
                }
            }
        }
    }
}
