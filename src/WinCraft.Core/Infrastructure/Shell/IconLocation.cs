using System;
using Windows.Win32;
using WinCraft.Interop;

namespace WinCraft.Infrastructure.Shell
{
    /// <summary>
    /// Parsed icon location: a file path and optional resource index.
    /// Uses PathParseIconLocation to parse strings like "shell32.dll,4".
    /// </summary>
    public readonly struct IconLocation : IEquatable<IconLocation>
    {
        public string FileName { get; }
        public int Index { get; }

        public IconLocation(string fileName, int index)
        {
            FileName = fileName;
            Index = index;
        }

        public IconLocation(string iconLocation)
        {
            if (string.IsNullOrEmpty(iconLocation))
            {
                FileName = string.Empty;
                Index = 0;
                return;
            }

            unsafe
            {
                FileName = StringBuffer.StackWrite(
                    iconLocation,
                    p => PInvoke.PathParseIconLocation(p),
                    out int index);
                Index = index;
            }
        }

        public override readonly string ToString()
        {
            return Index == 0 ? FileName : FileName + "," + Index;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is IconLocation other && Equals(other);
        }

        public readonly bool Equals(IconLocation other)
        {
            return Index == other.Index
                && string.Equals(FileName, other.FileName, StringComparison.OrdinalIgnoreCase);
        }

        public override readonly int GetHashCode()
        {
            int fileNameHash = FileName != null
                ? StringComparer.OrdinalIgnoreCase.GetHashCode(FileName)
                : 0;
            return unchecked(fileNameHash * 397) ^ Index;
        }

        public static bool operator ==(IconLocation left, IconLocation right) => left.Equals(right);

        public static bool operator !=(IconLocation left, IconLocation right) => !left.Equals(right);
    }
}
