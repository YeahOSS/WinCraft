using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WinCraft.IconFontTool
{
    internal static class TrueTypeSubsetter
    {
        internal static byte[] SubsetFont(byte[] font, IEnumerable<int> codepoints)
        {
            var requested = GetRequestedCodepoints(font, codepoints);
            return Subset(font, requested);
        }

        internal static void ValidateFont(byte[] font, IEnumerable<int> codepoints)
        {
            var requested = GetRequestedCodepoints(font, codepoints);
            var tables = ReadTableDirectory(font, out uint unusedSfntVersion);
            GetMappedGlyphIds(font, tables, requested);
        }

        // ── Big-endian helpers ──────────────────────────────────────────

        private static ushort ReadU16(byte[] data, int offset) =>
            (ushort)((data[offset] << 8) | data[offset + 1]);

        private static short ReadS16(byte[] data, int offset) =>
            (short)((data[offset] << 8) | data[offset + 1]);

        private static uint ReadU32(byte[] data, int offset) =>
            ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) |
            ((uint)data[offset + 2] << 8) | data[offset + 3];

        private static void WriteU16(byte[] data, int offset, ushort value)
        {
            data[offset] = (byte)(value >> 8);
            data[offset + 1] = (byte)(value & 0xFF);
        }

        private static void WriteU32(byte[] data, int offset, uint value)
        {
            data[offset] = (byte)(value >> 24);
            data[offset + 1] = (byte)((value >> 16) & 0xFF);
            data[offset + 2] = (byte)((value >> 8) & 0xFF);
            data[offset + 3] = (byte)(value & 0xFF);
        }

        private static string Tag(byte[] data, int offset) =>
            Encoding.ASCII.GetString(data, offset, 4);

        // ── Core ────────────────────────────────────────────────────────

        private static readonly HashSet<string> PassthroughTables = new(StringComparer.Ordinal)
        {
            "cvt ", "fpgm", "gasp", "name", "prep"
        };

        private static byte[] Subset(byte[] font, HashSet<int> codepoints)
        {
            var tables = ReadTableDirectory(font, out uint sfntVersion);
            var glyphIds = GetMappedGlyphIds(font, tables, codepoints);
            glyphIds.Add(0); // .notdef is always glyph 0

            CollectCompoundRefs(font, tables, glyphIds);

            var sorted = glyphIds.OrderBy(g => g).ToList();
            if (sorted.Count > ushort.MaxValue)
                throw new NotSupportedException("The subset contains more than 65,535 glyphs.");
            var maxGid = sorted[sorted.Count - 1];
            var remap = new ushort[maxGid + 1];
            for (var newId = 0; newId < sorted.Count; newId++)
                remap[sorted[newId]] = (ushort)newId;

            var newCmap = BuildCmap(font, tables, codepoints, remap);
            var newGlyf = BuildGlyf(font, tables, sorted, remap);
            var useShortLoca = newGlyf.TotalGlyfSize <= 0x1FFFE;
            var newLoca = BuildLoca(newGlyf.Glyphs, useShortLoca);
            var newHmtx = BuildHmtx(font, tables, sorted);
            var newHead = BuildHead(font, tables, useShortLoca);
            var newHhea = BuildHhea(font, tables, sorted.Count);
            var newMaxp = BuildMaxp(font, tables, sorted.Count);

            var newTables = new List<TableEntry>();
            AddTable(newTables, "cmap", newCmap);
            AddTable(newTables, "glyf", newGlyf.Data);
            AddTable(newTables, "loca", newLoca);
            AddTable(newTables, "hmtx", newHmtx);
            AddTable(newTables, "head", newHead);
            AddTable(newTables, "hhea", newHhea);
            AddTable(newTables, "maxp", newMaxp);

            if (tables.TryGetValue("OS/2", out TableRec os2Table))
                AddTable(newTables, "OS/2", BuildOS2(font, os2Table, codepoints));

            foreach (var tag in PassthroughTables)
            {
                if (tables.TryGetValue(tag, out TableRec table))
                    AddTable(newTables, tag, CopyTable(font, table));
            }

            if (tables.TryGetValue("post", out TableRec postTable))
                AddTable(newTables, "post", BuildPost(font, postTable));

            return Assemble(newTables, sfntVersion);
        }

        private static HashSet<int> GetRequestedCodepoints(byte[] font, IEnumerable<int> codepoints)
        {
            if (font == null)
                throw new ArgumentNullException(nameof(font));
            if (codepoints == null)
                throw new ArgumentNullException(nameof(codepoints));

            var requested = new HashSet<int>(codepoints);
            if (requested.Count == 0)
                throw new ArgumentException("At least one Unicode codepoint is required.", nameof(codepoints));
            if (requested.Any(codepoint => codepoint < 0 || codepoint > 0x10FFFF))
                throw new ArgumentOutOfRangeException(nameof(codepoints), "Unicode codepoints must be between U+0000 and U+10FFFF.");
            return requested;
        }

        private static Dictionary<string, TableRec> ReadTableDirectory(byte[] font, out uint sfntVersion)
        {
            if (font.Length < 12)
                throw new InvalidDataException("The input is not a valid sfnt font.");

            sfntVersion = ReadU32(font, 0);
            if (sfntVersion != 0x00010000 && sfntVersion != 0x74727565)
                throw new NotSupportedException("Only TrueType fonts with glyf outlines are supported.");

            var numTables = ReadU16(font, 4);
            var tables = new Dictionary<string, TableRec>(numTables);
            for (var index = 0; index < numTables; index++)
            {
                var offset = 12 + index * 16;
                if (offset + 16 > font.Length)
                    throw new InvalidDataException("The sfnt table directory is truncated.");
                var tag = Tag(font, offset);
                var table = new TableRec
                {
                    Checksum = ReadU32(font, offset + 4),
                    Offset = ReadU32(font, offset + 8),
                    Length = ReadU32(font, offset + 12)
                };
                if ((ulong)table.Offset + table.Length > (ulong)font.Length)
                    throw new InvalidDataException("The " + tag + " table extends beyond the input file.");
                if (tables.ContainsKey(tag))
                    throw new InvalidDataException("The input contains a duplicate " + tag + " table.");
                tables.Add(tag, table);
            }

            var requiredTables = new[] { "OS/2", "cmap", "glyf", "head", "hhea", "hmtx", "loca", "maxp", "name", "post" };
            var missingTables = requiredTables.Where(table => !tables.ContainsKey(table)).ToArray();
            if (missingTables.Length > 0)
                throw new NotSupportedException("The TrueType font is missing required tables: " + string.Join(", ", missingTables));
            return tables;
        }

        private static HashSet<int> GetMappedGlyphIds(
            byte[] font,
            Dictionary<string, TableRec> tables,
            IEnumerable<int> codepoints)
        {
            var originalGlyphCount = ReadU16(font, checked((int)tables["maxp"].Offset) + 4);
            var glyphIds = new HashSet<int>();
            var missingCodepoints = new List<int>();
            foreach (var codepoint in codepoints)
            {
                var glyphId = FindGlyphForCodepoint(font, tables, codepoint);
                if (glyphId <= 0)
                    missingCodepoints.Add(codepoint);
                else if (glyphId >= originalGlyphCount)
                    throw new InvalidDataException("The cmap table references a glyph outside maxp.numGlyphs.");
                else
                    glyphIds.Add(glyphId);
            }

            if (missingCodepoints.Count > 0)
            {
                throw new InvalidOperationException(
                    "The font does not contain: " +
                    string.Join(", ", missingCodepoints.OrderBy(value => value).Select(value => "U+" + value.ToString("X", CultureInfo.InvariantCulture))));
            }
            return glyphIds;
        }

        // ── Compound glyph traversal ─────────────────────────────────────

        private static void CollectCompoundRefs(byte[] font, Dictionary<string, TableRec> tables, HashSet<int> glyphIds)
        {
            var glyfOff = (int)tables["glyf"].Offset;
            var locaOff = (int)tables["loca"].Offset;
            var headOff = (int)tables["head"].Offset;
            var locaFmt = ReadS16(font, headOff + 50);
            var glyphCount = ReadU16(font, (int)tables["maxp"].Offset + 4);

            var queue = new Queue<int>(glyphIds);
            while (queue.Count > 0)
            {
                var gid = queue.Dequeue();
                var gOff = GetGlyphOffset(font, locaOff, locaFmt, gid);
                var nextOff = GetGlyphOffset(font, locaOff, locaFmt, gid + 1);
                if (gOff < 0 || nextOff < 0 || gOff >= nextOff) continue; // empty glyph

                var contours = ReadS16(font, glyfOff + gOff);
                if (contours >= 0) continue; // simple glyph

                // Compound glyph: walk components
                var pos = glyfOff + gOff + 10; // skip header
                ushort flags;
                do
                {
                    flags = ReadU16(font, pos);
                    pos += 2;
                    var compGid = ReadU16(font, pos);
                    pos += 2;
                    if (compGid >= glyphCount)
                        throw new InvalidDataException("A compound glyph references a glyph outside maxp.numGlyphs.");

                    if (glyphIds.Add(compGid))
                        queue.Enqueue(compGid);

                    // Skip arguments and transform based on flags
                    if ((flags & 1) != 0) { pos += 4; } else { pos += 2; } // ARG_1_AND_2_ARE_WORDS
                    if ((flags & 8) != 0) pos += 2;  // WE_HAVE_A_SCALE
                    else if ((flags & 64) != 0) pos += 4;  // WE_HAVE_AN_X_AND_Y_SCALE
                    else if ((flags & 128) != 0) pos += 8; // WE_HAVE_A_TWO_BY_TWO
                }
                while ((flags & 32) != 0); // MORE_COMPONENTS
            }
        }

        private static int GetGlyphOffset(byte[] font, int locaOff, int locaFmt, int gid)
        {
            if (locaFmt == 0)
                return ReadU16(font, locaOff + gid * 2) * 2;
            else
                return (int)ReadU32(font, locaOff + gid * 4);
        }

        // ── Table builders ───────────────────────────────────────────────

        /// <summary>
        /// Build a new cmap by filtering the original encoding records and
        /// subtables, keeping only entries for the target codepoints and
        /// remapping their glyph IDs.
        /// </summary>
        private static byte[] BuildCmap(byte[] font, Dictionary<string, TableRec> tables,
            HashSet<int> codepoints, ushort[] remap)
        {
            var origOff = (int)tables["cmap"].Offset;
            var numSub = ReadU16(font, origOff + 2);

            // Gather filtered subtables
            var subData = new List<byte[]>();
            var subPlat = new List<ushort>();
            var subEnc = new List<ushort>();

            for (var i = 0; i < numSub; i++)
            {
                var recOff = origOff + 4 + i * 8;
                var plat = ReadU16(font, recOff);
                var enc = ReadU16(font, recOff + 2);
                var subOff = origOff + (int)ReadU32(font, recOff + 4);
                var fmt = ReadU16(font, subOff);

                byte[] filtered = null;
                if (fmt == 4) filtered = FilterCmap4(font, subOff, codepoints, remap);
                else if (fmt == 12) filtered = FilterCmap12(font, subOff, codepoints, remap);

                if (filtered != null)
                {
                    subData.Add(filtered);
                    subPlat.Add(plat);
                    subEnc.Add(enc);
                }
            }

            if (subData.Count == 0)
                throw new InvalidDataException("The font has no supported Unicode cmap subtable.");

            // Assemble cmap header + encoding records + subtable data
            var totalLen = 4 + 8 * subData.Count;
            foreach (var d in subData) totalLen += d.Length;
            var result = new byte[totalLen];
            WriteU16(result, 0, 0);                              // version
            WriteU16(result, 2, (ushort)subData.Count);          // numTables

            var dataOff = 4 + 8 * subData.Count;
            for (var i = 0; i < subData.Count; i++)
            {
                WriteU16(result, 4 + i * 8, subPlat[i]);
                WriteU16(result, 6 + i * 8, subEnc[i]);
                WriteU32(result, 8 + i * 8, (uint)dataOff);
                Array.Copy(subData[i], 0, result, dataOff, subData[i].Length);
                dataOff += subData[i].Length;
            }

            return result;
        }

        /// <summary>
        /// Filter a format-4 cmap subtable: keep only segments that cover at
        /// least one target codepoint, and remap the glyph IDs via
        /// <paramref name="remap"/>.
        /// Keeps an empty format-4 terminator table when only supplementary
        /// codepoints are selected because WPF expects a BMP cmap record.
        /// </summary>
        private static byte[] FilterCmap4(byte[] font, int offset, HashSet<int> codepoints, ushort[] remap)
        {
            var segCountX2 = ReadU16(font, offset + 6);
            var segCount = segCountX2 / 2;
            var endCodesOff = offset + 14;
            var startCodesOff = endCodesOff + 2 + segCountX2;
            var idDeltasOff = startCodesOff + segCountX2;
            var idRangeOffsetsOff = idDeltasOff + segCountX2;

            var pairs = new List<CmapPair>();
            foreach (var cp in codepoints)
            {
                if (cp >= 0xFFFF) continue;
                var origGid = -1;
                for (var i = 0; i < segCount && origGid < 0; i++)
                {
                    var s = ReadU16(font, startCodesOff + i * 2);
                    var e = ReadU16(font, endCodesOff + i * 2);
                    if (cp < s || cp > e) continue;
                    var delta = (short)ReadU16(font, idDeltasOff + i * 2);
                    var ro = ReadU16(font, idRangeOffsetsOff + i * 2);
                    if (ro == 0) { origGid = (cp + delta) & 0xFFFF; }
                    else { var idx = cp - s; var bo = idRangeOffsetsOff + i * 2; var g = ReadU16(font, bo + ro + idx * 2); origGid = g != 0 ? ((g + delta) & 0xFFFF) : 0; }
                }
                if (origGid > 0 && origGid < remap.Length)
                    pairs.Add(new CmapPair { Cp = cp, NewGid = remap[origGid] });
            }
            pairs.Sort((a, b) => a.Cp.CompareTo(b.Cp));

            var newSegCount = pairs.Count + 1; // +1 terminator
            var newSegCountX2 = newSegCount * 2;
            var size = 16 + newSegCountX2 * 4;
            var result = new byte[size];
            WriteU16(result, 0, 4);
            WriteU16(result, 2, (ushort)size);
            WriteU16(result, 6, (ushort)newSegCountX2);
            var sr = MaxPowerOf2NotExceeding(newSegCount) * 2;
            WriteU16(result, 8, (ushort)sr);
            WriteU16(result, 10, (ushort)Log2(sr / 2));
            WriteU16(result, 12, (ushort)(newSegCountX2 - sr));

            var eOff = 14;
            var sOff = eOff + newSegCountX2 + 2;
            var dOff = sOff + newSegCountX2;
            var rOff = dOff + newSegCountX2;

            for (var i = 0; i < pairs.Count; i++)
            {
                var cp16 = (ushort)pairs[i].Cp;
                var gid = pairs[i].NewGid;
                WriteU16(result, eOff + i * 2, cp16);
                WriteU16(result, sOff + i * 2, cp16);
                WriteU16(result, dOff + i * 2, (ushort)((gid - cp16) & 0xFFFF));
                WriteU16(result, rOff + i * 2, 0);
            }
            var li = pairs.Count;
            WriteU16(result, eOff + li * 2, 0xFFFF);
            WriteU16(result, sOff + li * 2, 0xFFFF);
            WriteU16(result, dOff + li * 2, 1);
            WriteU16(result, rOff + li * 2, 0);

            return result;
        }

        /// <summary>
        /// Filter a format-12 cmap subtable: keep only groups that cover at
        /// least one target codepoint, and remap glyph IDs.
        /// </summary>
        private static byte[] FilterCmap12(byte[] font, int offset, HashSet<int> codepoints, ushort[] remap)
        {
            var numGroups = (int)ReadU32(font, offset + 12);
            var groups = new List<CmapGroup>();

            foreach (var cp in codepoints)
            {
                for (var i = 0; i < numGroups; i++)
                {
                    var go = offset + 16 + i * 12;
                    var sc = (int)ReadU32(font, go);
                    var ec = (int)ReadU32(font, go + 4);
                    if (cp < sc || cp > ec) continue;

                    var sg = (int)ReadU32(font, go + 8);
                    var origGid = sg + (cp - sc);
                    if (origGid < remap.Length)
                        groups.Add(new CmapGroup { StartCp = cp, EndCp = cp, StartGid = remap[origGid] });
                    break;
                }
            }

            if (groups.Count == 0) return null;
            groups.Sort((a, b) => a.StartCp.CompareTo(b.StartCp));

            // Merge adjacent groups
            var merged = new List<CmapGroup>();
            foreach (var g in groups)
            {
                if (merged.Count > 0 &&
                    merged[merged.Count - 1].EndCp + 1 == g.StartCp &&
                    merged[merged.Count - 1].StartGid + (g.StartCp - merged[merged.Count - 1].StartCp) == g.StartGid)
                {
                    var last = merged[merged.Count - 1];
                    merged[merged.Count - 1] = new CmapGroup { StartCp = last.StartCp, EndCp = g.EndCp, StartGid = last.StartGid };
                }
                else
                {
                    merged.Add(g);
                }
            }

            var size = 16 + merged.Count * 12;
            var result = new byte[size];
            WriteU16(result, 0, 12);
            WriteU32(result, 4, (uint)size);
            WriteU32(result, 12, (uint)merged.Count);
            for (var i = 0; i < merged.Count; i++)
            {
                var off = 16 + i * 12;
                WriteU32(result, off, (uint)merged[i].StartCp);
                WriteU32(result, off + 4, (uint)merged[i].EndCp);
                WriteU32(result, off + 8, (uint)merged[i].StartGid);
            }

            return result;
        }

        private static int FindGlyphForCodepoint(byte[] font, Dictionary<string, TableRec> tables, int cp)
        {
            var cmapOff = unchecked((int)tables["cmap"].Offset);
            var numSub = ReadU16(font, cmapOff + 2);
            var subOffset = cmapOff + 4;

            for (var i = 0; i < numSub; i++)
            {
                var plat = ReadU16(font, subOffset);
                var encoding = ReadU16(font, subOffset + 2);
                var off = cmapOff + (int)ReadU32(font, subOffset + 4);
                subOffset += 8;
                if (plat != 0 && !(plat == 3 && (encoding == 1 || encoding == 10)))
                    continue;

                var fmt = ReadU16(font, off);

                if (fmt == 4 && cp <= 0xFFFF)
                {
                    // Search format 4
                    var segCount = ReadU16(font, off + 6) / 2;
                    var endCodes = off + 14;
                    var startCodes = endCodes + 2 + segCount * 2;
                    var idDeltas = startCodes + segCount * 2;
                    var idRangeOffsets = idDeltas + segCount * 2;

                    for (var j = 0; j < segCount; j++)
                    {
                        var start = ReadU16(font, startCodes + j * 2);
                        var end = ReadU16(font, endCodes + j * 2);
                        if (cp < start || cp > end) continue;

                        var delta = (short)ReadU16(font, idDeltas + j * 2);
                        var rangeOff = ReadU16(font, idRangeOffsets + j * 2);

                        if (rangeOff == 0)
                            return (cp + delta) & 0xFFFF;

                        var idx = cp - start;
                        var baseOff = idRangeOffsets + j * 2;
                        var gid = ReadU16(font, baseOff + rangeOff + idx * 2);
                        if (gid != 0) gid = (ushort)((gid + delta) & 0xFFFF);
                        return gid;
                    }
                }
                else if (fmt == 12)
                {
                    var numGroups = ReadU32(font, off + 12);
                    var groupOff = off + 16;
                    for (var j = 0u; j < numGroups; j++)
                    {
                        var go = (int)(groupOff + j * 12);
                        var startCp = (int)ReadU32(font, go);
                        var endCp = (int)ReadU32(font, go + 4);
                        if (cp >= startCp && cp <= endCp)
                            return (int)ReadU32(font, go + 8) + (cp - startCp);
                    }
                }
            }

            return -1;
        }

        private struct GlyfResult
        {
            public byte[] Data;
            public byte[][] Glyphs;
            public int TotalGlyfSize;
        }

        private static GlyfResult BuildGlyf(
            byte[] font, Dictionary<string, TableRec> tables, List<int> sorted, ushort[] remap)
        {
            var glyfOff = (int)tables["glyf"].Offset;
            var locaOff = (int)tables["loca"].Offset;
            var headOff = (int)tables["head"].Offset;
            var locaFmt = ReadS16(font, headOff + 50);
            if (locaFmt != 0 && locaFmt != 1)
                throw new InvalidDataException("head.indexToLocFormat must be 0 or 1.");

            var newGlyphs = new byte[sorted.Count][];
            for (var i = 0; i < sorted.Count; i++)
            {
                var gid = sorted[i];
                var gOff = GetGlyphOffset(font, locaOff, locaFmt, gid);
                var nextOff = GetGlyphOffset(font, locaOff, locaFmt, gid + 1);

                if (gOff < 0 || nextOff <= gOff) { newGlyphs[i] = []; continue; }

                var len = nextOff - gOff;
                var srcOff = glyfOff + gOff;
                var glyfEnd = (long)glyfOff + tables["glyf"].Length;
                if (srcOff < glyfOff || (long)srcOff + len > glyfEnd)
                    throw new InvalidDataException("Glyph " + gid + " extends beyond the glyf table.");
                var glyph = new byte[(len + 3) & ~3];
                Array.Copy(font, srcOff, glyph, 0, len);

                // Remap compound glyph references
                var contours = ReadS16(glyph, 0);
                if (contours < 0 && len > 10)
                {
                    var pos = 10; // skip numberOfContours, xMin, yMin, xMax, yMax
                    ushort flags;
                    do
                    {
                        if (pos + 4 > len) break;
                        flags = ReadU16(glyph, pos);
                        var compGid = ReadU16(glyph, pos + 2);
                        if (compGid >= remap.Length || (compGid != 0 && remap[compGid] == 0))
                            throw new InvalidDataException("A compound glyph references an uncollected component.");
                        WriteU16(glyph, pos + 2, remap[compGid]);

                        pos += 4;
                        if ((flags & 1) != 0) { pos += 4; } else { pos += 2; }
                        if ((flags & 8) != 0) pos += 2;
                        else if ((flags & 64) != 0) pos += 4;
                        else if ((flags & 128) != 0) pos += 8;
                    }
                    while ((flags & 32) != 0);
                }

                newGlyphs[i] = glyph;
            }

            var totalSize = 0;
            foreach (var g in newGlyphs) totalSize += g.Length;

            var data = new byte[totalSize];
            var writeOff = 0;
            for (var i = 0; i < newGlyphs.Length; i++)
            {
                Array.Copy(newGlyphs[i], 0, data, writeOff, newGlyphs[i].Length);
                writeOff += newGlyphs[i].Length;
            }

            return new GlyfResult { Data = data, Glyphs = newGlyphs, TotalGlyfSize = totalSize };
        }

        private static byte[] BuildLoca(byte[][] newGlyphs, bool useShort)
        {
            var numGlyphs = newGlyphs.Length;

            if (useShort)
            {
                var result = new byte[(numGlyphs + 1) * 2];
                var offset = 0u;
                for (var i = 0; i < numGlyphs; i++)
                {
                    WriteU16(result, i * 2, (ushort)(offset / 2));
                    offset += (uint)newGlyphs[i].Length;
                }
                WriteU16(result, numGlyphs * 2, (ushort)(offset / 2));
                return result;
            }
            else
            {
                var result = new byte[(numGlyphs + 1) * 4];
                var offset = 0u;
                for (var i = 0; i < numGlyphs; i++)
                {
                    WriteU32(result, i * 4, offset);
                    offset += (uint)newGlyphs[i].Length;
                }
                WriteU32(result, numGlyphs * 4, offset);
                return result;
            }
        }

        private static byte[] BuildHmtx(byte[] font, Dictionary<string, TableRec> tables, List<int> sorted)
        {
            var hmtxOff = (int)tables["hmtx"].Offset;
            var hheaOff = (int)tables["hhea"].Offset;
            var numLongMetrics = ReadU16(font, hheaOff + 34);
            if (numLongMetrics == 0)
                throw new InvalidDataException("hhea.numberOfHMetrics must be greater than zero.");

            var result = new byte[sorted.Count * 4]; // advanceWidth (2) + lsb (2)
            for (var i = 0; i < sorted.Count; i++)
            {
                var gid = sorted[i];
                if (gid < numLongMetrics)
                {
                    Array.Copy(font, hmtxOff + gid * 4, result, i * 4, 4);
                }
                else
                {
                    // Use last long metric's advance + own lsb
                    var lastAdvOff = (numLongMetrics - 1) * 4;
                    Array.Copy(font, hmtxOff + lastAdvOff, result, i * 4, 2); // advance from last long
                    var lsbOff = hmtxOff + numLongMetrics * 4 + (gid - numLongMetrics) * 2;
                    WriteU16(result, i * 4 + 2, ReadU16(font, lsbOff)); // own lsb
                }
            }

            return result;
        }

        private static byte[] BuildHead(byte[] font, Dictionary<string, TableRec> tables, bool useShortLoca)
        {
            var headOff = (int)tables["head"].Offset;
            var len = (int)tables["head"].Length;
            var result = new byte[len];
            Array.Copy(font, headOff, result, 0, len);

            WriteU32(result, 8, 0);
            WriteU16(result, 50, useShortLoca ? (ushort)0 : (ushort)1);

            return result;
        }

        private static byte[] BuildHhea(byte[] font, Dictionary<string, TableRec> tables, int newNumGlyphs)
        {
            var hheaOff = (int)tables["hhea"].Offset;
            var len = (int)tables["hhea"].Length;
            var result = new byte[len];
            Array.Copy(font, hheaOff, result, 0, len);
            // numLongMetrics must not exceed numGlyphs.  Set it to numGlyphs
            // so every glyph gets a full 4-byte hmtx entry.
            WriteU16(result, 34, (ushort)newNumGlyphs);
            return result;
        }

        private static byte[] BuildMaxp(byte[] font, Dictionary<string, TableRec> tables, int newNumGlyphs)
        {
            var maxpOff = (int)tables["maxp"].Offset;
            var len = (int)tables["maxp"].Length;
            var result = new byte[len];
            Array.Copy(font, maxpOff, result, 0, len);
            WriteU16(result, 4, (ushort)newNumGlyphs);
            return result;
        }

        private static byte[] BuildOS2(byte[] font, TableRec table, IEnumerable<int> codepoints)
        {
            var result = CopyTable(font, table);
            if (result.Length < 68)
                throw new InvalidDataException("The OS/2 table is truncated.");

            var ordered = codepoints.OrderBy(codepoint => codepoint).ToArray();
            WriteU16(result, 64, (ushort)Math.Min(0xFFFF, ordered[0]));
            WriteU16(result, 66, (ushort)Math.Min(0xFFFF, ordered[ordered.Length - 1]));
            return result;
        }

        private static byte[] BuildPost(byte[] font, TableRec table)
        {
            if (table.Length < 32)
                throw new InvalidDataException("The post table is truncated.");

            var result = new byte[32];
            Array.Copy(font, checked((int)table.Offset), result, 0, result.Length);
            WriteU32(result, 0, 0x00030000);
            return result;
        }

        private static byte[] CopyTable(byte[] font, TableRec table)
        {
            var result = new byte[checked((int)table.Length)];
            Array.Copy(font, checked((int)table.Offset), result, 0, result.Length);
            return result;
        }

        // ── Assembly ─────────────────────────────────────────────────────

        private struct TableEntry
        {
            public string Tag;
            public byte[] Data;
        }

        private static void AddTable(List<TableEntry> list, string tag, byte[] data)
        {
            list.Add(new TableEntry { Tag = tag, Data = data });
        }

        private static byte[] Assemble(List<TableEntry> entries, uint sfntVersion)
        {
            entries = [.. entries.OrderBy(entry => entry.Tag, StringComparer.Ordinal)];

            // Calculate sizes
            var numTables = entries.Count;
            var headerSize = 12 + 16 * numTables;

            // Calculate table offsets (each table starts 4-byte aligned)
            var tableOffsets = new int[numTables];
            var offset = headerSize;
            for (var i = 0; i < numTables; i++)
            {
                // Align to 4 bytes
                offset = (offset + 3) & ~3;
                tableOffsets[i] = offset;
                offset += entries[i].Data.Length;
            }

            var totalSize = (offset + 3) & ~3;
            var result = new byte[totalSize];

            WriteU32(result, 0, sfntVersion);
            WriteU16(result, 4, (ushort)(numTables));
            // searchRange, entrySelector, rangeShift
            var sr = MaxPowerOf2NotExceeding(numTables) * 16;
            WriteU16(result, 6, (ushort)sr);
            WriteU16(result, 8, (ushort)Log2(sr / 16));
            WriteU16(result, 10, (ushort)(numTables * 16 - sr));

            // Write table directory + data
            for (var i = 0; i < numTables; i++)
            {
                var dirOff = 12 + i * 16;
                var tagBytes = Encoding.ASCII.GetBytes(entries[i].Tag);
                Array.Copy(tagBytes, 0, result, dirOff, 4);
                WriteU32(result, dirOff + 8, (uint)tableOffsets[i]);
                WriteU32(result, dirOff + 12, (uint)entries[i].Data.Length);

                // Copy table data
                Array.Copy(entries[i].Data, 0, result, tableOffsets[i], entries[i].Data.Length);
            }

            // Calculate checksums
            for (var i = 0; i < numTables; i++)
            {
                var cs = CalculateChecksum(result, tableOffsets[i], entries[i].Data.Length);
                WriteU32(result, 12 + i * 16 + 4, cs);
            }

            // Calculate head checksumAdjustment
            var headIdx = entries.FindIndex(e => e.Tag == "head");
            if (headIdx >= 0)
            {
                var fullChecksum = CalculateChecksum(result, 0, totalSize);
                WriteU32(result, tableOffsets[headIdx] + 8, 0xB1B0AFBA - fullChecksum);
            }

            return result;
        }

        private static uint CalculateChecksum(byte[] data, int offset, int length)
        {
            // Checksum is sum of all uint32 values, with any odd final bytes padded with 0
            uint sum = 0;
            var end = offset + (length & ~3); // round down to multiple of 4
            for (var i = offset; i < end; i += 4)
                sum += ReadU32(data, i);

            // Handle remaining 1-3 bytes
            var rem = length & 3;
            if (rem > 0)
            {
                var pad = 0u;
                for (var i = 0; i < rem; i++)
                    pad = (pad << 8) | data[offset + length - rem + i];
                pad <<= (4 - rem) * 8;
                sum += pad;
            }

            return sum;
        }

        private static int MaxPowerOf2NotExceeding(int n)
        {
            var p = 1;
            while (p * 2 <= n) p *= 2;
            return p;
        }

        private static int Log2(int n) => n <= 1 ? 0 : 1 + Log2(n / 2);

        // ── Table record ─────────────────────────────────────────────────

        private struct CmapPair { public int Cp; public ushort NewGid; }
        private struct CmapGroup { public int StartCp; public int EndCp; public int StartGid; }

        private struct TableRec
        {
            public uint Checksum;
            public uint Offset;
            public uint Length;
        }
    }
}
