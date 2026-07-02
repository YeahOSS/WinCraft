using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace WinCraft.Overlay
{
    /// <summary>
    /// Loads dependency assemblies from a compressed container appended to the
    /// WinCraft.exe PE file.
    /// </summary>
    internal static class AssemblyResolver
    {
        private const uint HybridOverlayMagic = 0x59484F57; // "WOHY"
        private const string LzmaAssemblyName = "WinCraft.Lzma";
        private const string InnerDependenciesKey = "__dependencies";

        [ThreadStatic]
        private static bool _resolving;

        private static readonly object _lock = new();
        private static volatile Dictionary<string, byte[]> _cache;
        private static volatile Assembly _lzmaAssembly;

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

            if (_lzmaAssembly != null
                && string.Equals(name, LzmaAssemblyName, StringComparison.OrdinalIgnoreCase))
            {
                return _lzmaAssembly;
            }

            byte[] bytes;
            lock (_lock)
            {
                if (!_cache.TryGetValue(name, out bytes))
                    return null;
            }

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

                    if (BitConverter.ToUInt32(magicBytes, 0) != HybridOverlayMagic)
                        return [];

                    containerBytes = ReadDeflateOverlay(fs);
                }

                var outerEntries = ReadContainer(containerBytes);
                return ReadInnerContainer(outerEntries);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{nameof(AssemblyResolver)}: overlay read failed — {ex.Message}");
                return [];
            }
        }

        private static byte[] ReadDeflateOverlay(FileStream fs)
        {
            const int footerSize = 16;
            if (fs.Length < footerSize)
                return [];

            fs.Seek(-footerSize, SeekOrigin.End);
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
            using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream((int)rawLength);
            CopyStream(deflateStream, outputStream);
            if (outputStream.Length != rawLength)
                return [];

            return outputStream.ToArray();
        }

        private static Dictionary<string, byte[]> ReadInnerContainer(Dictionary<string, byte[]> outerEntries)
        {
            if (!outerEntries.TryGetValue(LzmaAssemblyName, out var lzmaBytes)
                || !outerEntries.TryGetValue(InnerDependenciesKey, out var innerPayload))
            {
                return outerEntries;
            }

            var lzmaAssembly = Assembly.Load(lzmaBytes);
            var codecType = lzmaAssembly.GetType("WinCraft.Lzma.LzmaCodec", throwOnError: true);
            var decompress = codecType.GetMethod(
                "Decompress",
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(byte[])],
                null)
                ?? throw new MissingMethodException("WinCraft.Lzma.LzmaCodec", "Decompress");
            var innerContainerBytes = (byte[])decompress.Invoke(null, [innerPayload]);
            var innerEntries = ReadContainer(innerContainerBytes);

            _lzmaAssembly = lzmaAssembly;

            // Preserve outer entries that aren't the LZMA bootstrap payloads.
            foreach (var kv in outerEntries)
            {
                if (kv.Key != LzmaAssemblyName && kv.Key != InnerDependenciesKey)
                    innerEntries[kv.Key] = kv.Value;
            }

            return innerEntries;
        }

        private static void CopyStream(Stream source, Stream destination)
        {
            var buffer = new byte[81920];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                destination.Write(buffer, 0, read);
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
                        if (dataLen <= 0 || dataLen > ms.Length - ms.Position)
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
