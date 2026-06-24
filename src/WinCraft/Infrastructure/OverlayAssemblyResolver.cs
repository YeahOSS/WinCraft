using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace WinCraft.Infrastructure
{
    /// <summary>
    /// Loads dependency assemblies from a compressed container appended to the
    /// WinCraft.exe PE file, keeping the PE metadata in its original form.
    ///
    /// Overlay format (appended after the last PE section):
    ///   [Deflate compressed container]
    ///   [4 bytes LE: compressed container length]
    ///   [4 bytes: magic "WOVL" = 0x4C564F57]
    ///
    /// The decompressed container is a flat sequence:
    ///   [4 bytes: entry count]
    ///   For each entry:
    ///     [2 bytes LE: name length in UTF-8 bytes]
    ///     [name UTF-8 bytes]
    ///     [4 bytes LE: data length]
    ///     [data bytes]
    /// </summary>
    internal static class OverlayAssemblyResolver
    {
        private const uint OverlayMagic = 0x4C564F57; // "WOVL"

        [ThreadStatic]
        private static bool _resolving;

        private static readonly object _lock = new();
        private static Dictionary<string, byte[]> _cache;

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
                Trace.TraceWarning($"{nameof(OverlayAssemblyResolver)}: failed to parse assembly name — {ex.Message}");
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
                    _cache ??= ReadOverlay() ?? [];
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
                    _cache.Remove(name);
                    Trace.TraceError($"{nameof(OverlayAssemblyResolver)}: failed to load {name} from overlay, evicting — {ex.Message}");
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
                    const int footerSize = 8;
                    if (fs.Length < footerSize)
                        return [];

                    fs.Seek(-4, SeekOrigin.End);
                    var magicBytes = new byte[4];
                    if (fs.Read(magicBytes, 0, 4) != 4)
                        return [];

                    if (BitConverter.ToUInt32(magicBytes, 0) != OverlayMagic)
                        return [];

                    fs.Seek(-8, SeekOrigin.End);
                    var lenBytes = new byte[4];
                    if (fs.Read(lenBytes, 0, 4) != 4)
                        return [];

                    var compressedLength = BitConverter.ToInt32(lenBytes, 0);
                    if (compressedLength <= 0 || compressedLength > fs.Length - footerSize)
                        return [];

                    fs.Seek(fs.Length - footerSize - compressedLength, SeekOrigin.Begin);
                    using var deflate = new DeflateStream(fs, CompressionMode.Decompress, true);
                    using var ms = new MemoryStream();
                    var buffer = new byte[8192];
                    int read;
                    while ((read = deflate.Read(buffer, 0, buffer.Length)) > 0)
                        ms.Write(buffer, 0, read);
                    containerBytes = ms.ToArray();
                }

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
                                Trace.TraceWarning($"{nameof(OverlayAssemblyResolver)}: duplicate entry '{key}', overwriting");
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
            catch (Exception ex)
            {
                Trace.TraceError($"{nameof(OverlayAssemblyResolver)}: overlay read failed — {ex.Message}");
                return [];
            }
        }
    }
}
