using WinCraft.Interop;
using Windows.Win32;

namespace WinCraft.Infrastructure.FileSystem
{
    /// <summary>
    /// Reads and writes a single .ini file via
    /// <c>GetPrivateProfileString</c> / <c>WritePrivateProfileString</c>.
    /// </summary>
    internal class IniFile(string filePath)
    {
        public string FilePath { get; } = filePath;

        /// <summary>
        /// Gets or sets a value in <paramref name="section"/>.
        /// </summary>
        public string this[string section, string key]
        {
            get
            {
                unsafe
                {
                    string result = StringBuffer.HeapWrite(
                        (int)PInvoke.MAX_PATH,
                        p => PInvoke.GetPrivateProfileString(section, key, null, p, PInvoke.MAX_PATH, FilePath));

                    return string.IsNullOrEmpty(result) ? null : result;
                }
            }
            set
            {
                PInvoke.WritePrivateProfileString(section, key, value ?? string.Empty, FilePath);
            }
        }

        /// <summary>
        /// Deletes a key from <paramref name="section"/>.
        /// Returns false when the key did not exist.
        /// </summary>
        public bool RemoveKey(string section, string key)
        {
            if (this[section, key] == null)
                return false;

            PInvoke.WritePrivateProfileString(section, key, null, FilePath);
            return true;
        }
    }
}
