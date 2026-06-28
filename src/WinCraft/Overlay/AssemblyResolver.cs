using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace WinCraft.Overlay
{
    /// <summary>
    /// Loads dependency assemblies from a compressed container appended to the
    /// WinCraft.exe PE file, keeping the PE metadata in its original form.
    ///
    /// LZMA overlay format (appended after the last PE section):
    ///   [LZMA compressed container]
    ///   [5 bytes: LZMA properties]
    ///   [8 bytes LE: decompressed container length]
    ///   [4 bytes LE: compressed container length]
    ///   [4 bytes: magic "WOLZ" = 0x5A4C4F57]
    ///
    /// The decompressed container is a flat sequence:
    ///   [4 bytes: entry count]
    ///   For each entry:
    ///     [2 bytes LE: name length in UTF-8 bytes]
    ///     [name UTF-8 bytes]
    ///     [4 bytes LE: data length]
    ///     [data bytes]
    /// </summary>
    internal static class AssemblyResolver
    {
        private const uint LzmaOverlayMagic = 0x5A4C4F57; // "WOLZ"

        [ThreadStatic]
        private static bool _resolving;

        private static readonly object _lock = new();
        private static volatile Dictionary<string, byte[]> _cache;

        public static void Register()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            if (_resolving)
                return null;

            string name;
            try
            {
                name = new AssemblyName(args.Name).Name;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"{nameof(AssemblyResolver)}: failed to parse assembly name — {ex.Message}");
                return null;
            }

            if (string.IsNullOrEmpty(name))
                return null;

            _resolving = true;
            try
            {
                return LoadFromOverlay(name);
            }
            finally
            {
                _resolving = false;
            }
        }

        private static Assembly LoadFromOverlay(string name)
        {
            if (_cache == null)
            {
                lock (_lock)
                {
                    _cache ??= ReadOverlay();
                }
            }

            if (_cache.TryGetValue(name, out var bytes))
            {
                try
                {
                    return Assembly.Load(bytes);
                }
                catch (Exception ex)
                {
                    lock (_lock) { _cache.Remove(name); }
                    Trace.TraceError($"{nameof(AssemblyResolver)}: failed to load {name} from overlay, evicting — {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        private static Dictionary<string, byte[]> ReadOverlay()
        {
            try
            {
                var exePath = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    return [];

                byte[] containerBytes;
                using (var fs = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (fs.Length < 4)
                        return [];

                    fs.Seek(-4, SeekOrigin.End);
                    var magicBytes = new byte[4];
                    if (fs.Read(magicBytes, 0, 4) != 4)
                        return [];

                    if (BitConverter.ToUInt32(magicBytes, 0) != LzmaOverlayMagic)
                        return [];

                    containerBytes = ReadLzmaOverlay(fs);
                }

                return ReadContainer(containerBytes);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{nameof(AssemblyResolver)}: overlay read failed — {ex.Message}");
                return [];
            }
        }

        private static byte[] ReadLzmaOverlay(FileStream fs)
        {
            const int footerSize = 21;
            if (fs.Length < footerSize)
                return [];

            fs.Seek(-footerSize, SeekOrigin.End);
            var properties = new byte[5];
            if (fs.Read(properties, 0, properties.Length) != properties.Length)
                return [];

            var rawLengthBytes = new byte[8];
            if (fs.Read(rawLengthBytes, 0, rawLengthBytes.Length) != rawLengthBytes.Length)
                return [];

            var compressedLengthBytes = new byte[4];
            if (fs.Read(compressedLengthBytes, 0, compressedLengthBytes.Length) != compressedLengthBytes.Length)
                return [];

            var rawLength = BitConverter.ToInt64(rawLengthBytes, 0);
            var compressedLength = BitConverter.ToInt32(compressedLengthBytes, 0);
            if (rawLength <= 0
                || rawLength > int.MaxValue
                || compressedLength <= 0
                || compressedLength > fs.Length - footerSize)
            {
                return [];
            }

            fs.Seek(fs.Length - footerSize - compressedLength, SeekOrigin.Begin);
            var compressedBytes = new byte[compressedLength];
            if (fs.Read(compressedBytes, 0, compressedBytes.Length) != compressedBytes.Length)
                return [];

            using var compressedStream = new MemoryStream(compressedBytes);
            using var outputStream = new MemoryStream((int)rawLength);
            var decoder = new SevenZip.Compression.LZMA.Decoder();
            decoder.SetDecoderProperties(properties);
            decoder.Code(compressedStream, outputStream, compressedLength, rawLength, null);
            return outputStream.ToArray();
        }

        private static Dictionary<string, byte[]> ReadContainer(byte[] containerBytes)
        {
            var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            using (var ms = new MemoryStream(containerBytes))
            using (var reader = new BinaryReader(ms, Encoding.UTF8))
            {
                var count = reader.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    try
                    {
                        var nameLen = reader.ReadInt16();
                        if (nameLen <= 0 || nameLen > 512)
                            break;

                        var entryName = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));
                        var dataLen = reader.ReadInt32();
                        if (dataLen <= 0)
                            break;

                        var data = reader.ReadBytes(dataLen);
                        if (data.Length != dataLen)
                            break;

                        var key = Path.GetFileNameWithoutExtension(entryName);
                        if (result.ContainsKey(key))
                            Trace.TraceWarning($"{nameof(AssemblyResolver)}: duplicate entry '{key}', overwriting");
                        result[key] = data;
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }
                }
            }

            return result;
        }
    }
}
