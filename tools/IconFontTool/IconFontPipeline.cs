using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace WinCraft.IconFontTool
{
    internal static class IconFontPipeline
    {
        private static readonly Regex StaticIconPattern = new(
            @"\bIconGlyph\.([A-Za-z_][A-Za-z0-9_]*)\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex XamlIconPattern = new(
            @"\b(?:Checked)?Icon\s*=\s*(?<quote>[""'])(?<name>[A-Za-z_][A-Za-z0-9_]*)\k<quote>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex XamlSetterPattern = new(
            @"<Setter\b(?=[^>]*\bProperty\s*=\s*(?<propertyQuote>[""'])(?:(?:[A-Za-z_][A-Za-z0-9_]*:)?(?:Design\.)?(?:Checked)?Icon)\k<propertyQuote>)(?=[^>]*\bValue\s*=\s*(?<valueQuote>[""'])(?<name>[A-Za-z_][A-Za-z0-9_]*)\k<valueQuote>)[^>]*>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        internal static void Run(CommandLineOptions options)
        {
            var regularItems = ReadMetadata(options.GetRequired("regular-metadata"));
            var filledItems = ReadMetadata(options.GetRequired("filled-metadata"));
            var mode = options.GetRequired("mode");

            IReadOnlyList<IconItem> selectedItems;
            if (string.Equals(mode, "full", StringComparison.OrdinalIgnoreCase))
            {
                selectedItems = regularItems;
            }
            else if (string.Equals(mode, "used", StringComparison.OrdinalIgnoreCase))
            {
                var knownNames = new HashSet<string>(regularItems.Select(item => item.Name), StringComparer.Ordinal);
                var usedNames = FindUsedNames(
                    options.GetRequired("source-roots"),
                    options.GetOptional("excluded-source"),
                    knownNames);
                var regularByName = regularItems.ToDictionary(item => item.Name, StringComparer.Ordinal);
                var unknownNames = usedNames.Where(name => !regularByName.ContainsKey(name)).ToArray();
                if (unknownNames.Length > 0)
                    throw new InvalidOperationException("Unknown icon glyphs: " + string.Join(", ", unknownNames));

                selectedItems = usedNames.Select(name => regularByName[name]).ToArray();
                if (selectedItems.Count == 0)
                    throw new InvalidOperationException("No icon glyph references were found in the configured source roots.");
            }
            else
            {
                throw new ArgumentException("--mode must be either full or used.");
            }

            var filledByName = filledItems.ToDictionary(item => item.Name, StringComparer.Ordinal);
            WriteTextIfChanged(options.GetRequired("enum-output"), BuildEnum(selectedItems));
            WriteTextIfChanged(options.GetRequired("map-output"), BuildGlyphMap(selectedItems, filledByName));

            if (!string.Equals(mode, "used", StringComparison.OrdinalIgnoreCase))
                return;

            WriteSubsetFont(
                options.GetRequired("regular-font"),
                options.GetRequired("regular-font-output"),
                selectedItems.Select(item => item.CodePoint));

            var filledCodepoints = selectedItems
                .Where(item => filledByName.ContainsKey(item.Name))
                .Select(item => filledByName[item.Name].CodePoint)
                .ToArray();
            if (filledCodepoints.Length == 0)
                throw new InvalidOperationException("No filled glyphs match the selected regular icons.");

            WriteSubsetFont(
                options.GetRequired("filled-font"),
                options.GetRequired("filled-font-output"),
                filledCodepoints);
        }

        internal static void Verify(CommandLineOptions options)
        {
            var manifestPath = Path.GetFullPath(options.GetRequired("manifest"));
            var serializer = new JavaScriptSerializer();
            var manifest = serializer.Deserialize<FontAssetManifest>(File.ReadAllText(manifestPath));
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.Source) || string.IsNullOrWhiteSpace(manifest.Version))
                throw new InvalidDataException("The icon font manifest must specify source and version.");
            if (manifest.Assets == null || manifest.Assets.Count == 0)
                throw new InvalidDataException("The icon font manifest contains no assets.");

            var manifestDirectory = Path.GetDirectoryName(manifestPath);
            var manifestAssets = new Dictionary<string, FontAssetRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var asset in manifest.Assets)
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.File) || string.IsNullOrWhiteSpace(asset.Sha256))
                    throw new InvalidDataException("Every icon font manifest asset must specify file and sha256.");
                if (manifestAssets.ContainsKey(asset.File))
                    throw new InvalidDataException("The icon font manifest contains a duplicate asset: " + asset.File);

                var assetPath = Path.GetFullPath(Path.Combine(manifestDirectory, asset.File));
                if (!File.Exists(assetPath))
                    throw new FileNotFoundException("The icon font asset does not exist.", assetPath);

                string actualHash;
                using (var stream = File.OpenRead(assetPath))
                using (var sha256 = SHA256.Create())
                    actualHash = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();

                if (!string.Equals(actualHash, asset.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SHA-256 mismatch for " + asset.File + ". Expected " + asset.Sha256 + ", actual " + actualHash + ".");

                manifestAssets.Add(asset.File, asset);
            }

            var regularItems = ReadAndVerifyMetadata(options.GetRequired("regular-metadata"), manifestAssets);
            var filledItems = ReadAndVerifyMetadata(options.GetRequired("filled-metadata"), manifestAssets);
            VerifyFont(options.GetRequired("regular-font"), regularItems, manifestAssets);
            VerifyFont(options.GetRequired("filled-font"), filledItems, manifestAssets);

            var regularNames = new HashSet<string>(regularItems.Select(item => item.Name), StringComparer.Ordinal);
            var filledNames = new HashSet<string>(filledItems.Select(item => item.Name), StringComparer.Ordinal);
            var sharedCount = regularNames.Count(name => filledNames.Contains(name));
            Console.WriteLine(
                "Verified Fluent UI System Icons {0}: Regular {1}, Filled {2}, shared {3}, Regular-only {4}, Filled-only {5}.",
                manifest.Version,
                regularItems.Count,
                filledItems.Count,
                sharedCount,
                regularItems.Count - sharedCount,
                filledItems.Count - sharedCount);
        }

        internal static string ConvertToEnumName(string sourceName)
        {
            const string metadataPrefix = "ic_fluent_";
            var normalized = sourceName.StartsWith(metadataPrefix, StringComparison.Ordinal)
                ? sourceName.Substring(metadataPrefix.Length)
                : sourceName;
            var parts = normalized.Split('_').ToList();
            if (parts.Count > 1 &&
                (string.Equals(parts[parts.Count - 1], "regular", StringComparison.Ordinal) ||
                 string.Equals(parts[parts.Count - 1], "filled", StringComparison.Ordinal)))
            {
                parts.RemoveAt(parts.Count - 1);
            }

            var builder = new StringBuilder();
            foreach (var part in parts)
            {
                if (part.Length == 0)
                    continue;
                if (part.All(char.IsDigit))
                    builder.Append(part);
                else
                    builder.Append(char.ToUpperInvariant(part[0])).Append(part.Substring(1));
            }

            if (builder.Length > 0 && char.IsDigit(builder[0]))
                builder.Insert(0, "Icon");
            return builder.ToString();
        }

        internal static IReadOnlyList<string> FindUsedNames(
            string roots,
            string excludedSources,
            ISet<string> knownNames)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            var excludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(excludedSources))
            {
                foreach (var path in excludedSources.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    excludedPaths.Add(Path.GetFullPath(path.Trim()));
            }

            foreach (var rootValue in roots.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var root = Path.GetFullPath(rootValue.Trim());
                if (!Directory.Exists(root))
                    continue;

                foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    var extension = Path.GetExtension(path);
                    if (!string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(extension, ".xaml", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (ContainsDirectory(path, "bin") || ContainsDirectory(path, "obj"))
                        continue;

                    var fullPath = Path.GetFullPath(path);
                    if (excludedPaths.Contains(fullPath))
                        continue;

                    var content = File.ReadAllText(fullPath);
                    foreach (Match match in StaticIconPattern.Matches(content))
                        names.Add(match.Groups[1].Value);

                    if (!string.Equals(extension, ".xaml", StringComparison.OrdinalIgnoreCase))
                        continue;

                    AddKnownXamlNames(content, XamlIconPattern, knownNames, names);
                    AddKnownXamlNames(content, XamlSetterPattern, knownNames, names);
                }
            }

            names.Remove("None");
            return names.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        }

        private static IReadOnlyList<IconItem> ReadMetadata(string path)
        {
            var serializer = new JavaScriptSerializer();
            var values = serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(path));
            var items = values.Select(pair => new IconItem(
                ConvertToEnumName(pair.Key),
                Convert.ToInt32(pair.Value, CultureInfo.InvariantCulture))).ToList();

            foreach (var duplicate in items.GroupBy(item => item.Name).Where(group => group.Count() > 1))
            {
                var legacyItems = duplicate.OrderBy(item => item.CodePoint).Take(duplicate.Count() - 1).ToArray();
                for (var index = 0; index < legacyItems.Length; index++)
                {
                    var prefix = index == 0 ? "Legacy" : "Legacy" + (index + 1).ToString(CultureInfo.InvariantCulture);
                    legacyItems[index].Name = prefix + legacyItems[index].Name;
                }
            }

            return items.OrderBy(item => item.Name, StringComparer.Ordinal).ToArray();
        }

        private static IReadOnlyList<IconItem> ReadAndVerifyMetadata(
            string path,
            IDictionary<string, FontAssetRecord> manifestAssets)
        {
            var asset = GetManifestAsset(path, manifestAssets);
            var items = ReadMetadata(path);
            if (asset.ExpectedEntries <= 0)
                throw new InvalidDataException("The metadata manifest entry must specify expectedEntries: " + asset.File);
            if (items.Count != asset.ExpectedEntries)
            {
                throw new InvalidDataException(
                    asset.File + " contains " + items.Count.ToString(CultureInfo.InvariantCulture) +
                    " entries; expected " + asset.ExpectedEntries.ToString(CultureInfo.InvariantCulture) + ".");
            }
            return items;
        }

        private static void VerifyFont(
            string path,
            IReadOnlyList<IconItem> items,
            IDictionary<string, FontAssetRecord> manifestAssets)
        {
            GetManifestAsset(path, manifestAssets);
            TrueTypeSubsetter.ValidateFont(File.ReadAllBytes(path), items.Select(item => item.CodePoint));
        }

        private static FontAssetRecord GetManifestAsset(
            string path,
            IDictionary<string, FontAssetRecord> manifestAssets)
        {
            var fileName = Path.GetFileName(path);
            if (!manifestAssets.TryGetValue(fileName, out FontAssetRecord asset))
                throw new InvalidDataException("The icon font manifest does not contain " + fileName + ".");
            return asset;
        }

        private static string BuildEnum(IEnumerable<IconItem> items)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("namespace WinCraft.UI");
            builder.AppendLine("{");
            builder.AppendLine("    public enum IconGlyph");
            builder.AppendLine("    {");
            builder.AppendLine("        None = 0,");
            foreach (var item in items)
                builder.Append("        ").Append(item.Name).Append(" = ").Append(item.CodePoint).AppendLine(",");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildGlyphMap(
            IEnumerable<IconItem> regularItems,
            IDictionary<string, IconItem> filledByName)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine();
            builder.AppendLine("namespace WinCraft.UI");
            builder.AppendLine("{");
            builder.AppendLine("    internal static class IconGlyphMap");
            builder.AppendLine("    {");
            builder.AppendLine("        internal static readonly Dictionary<int, int> RegularToFilled = new Dictionary<int, int>");
            builder.AppendLine("        {");

            var entryCount = 0;
            foreach (var regularItem in regularItems)
            {
                if (!filledByName.TryGetValue(regularItem.Name, out IconItem filledItem))
                    continue;
                builder.Append("            { ").Append(regularItem.CodePoint).Append(", ")
                    .Append(filledItem.CodePoint).AppendLine(" },");
                entryCount++;
            }

            if (entryCount == 0)
                throw new InvalidOperationException("The regular and filled metadata files have no matching icons.");

            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AddKnownXamlNames(
            string content,
            Regex pattern,
            ISet<string> knownNames,
            ISet<string> result)
        {
            foreach (Match match in pattern.Matches(content))
            {
                var name = match.Groups["name"].Value;
                if (knownNames.Contains(name))
                    result.Add(name);
            }
        }

        private static bool ContainsDirectory(string path, string directoryName)
        {
            var separator = Path.DirectorySeparatorChar;
            var alternateSeparator = Path.AltDirectorySeparatorChar;
            return path.IndexOf(separator + directoryName + separator, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   path.IndexOf(alternateSeparator + directoryName + alternateSeparator, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void WriteTextIfChanged(string path, string content)
        {
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            if (File.Exists(fullPath) && string.Equals(File.ReadAllText(fullPath), content, StringComparison.Ordinal))
            {
                File.SetLastWriteTimeUtc(fullPath, DateTime.UtcNow);
                return;
            }

            File.WriteAllText(fullPath, content, new UTF8Encoding(false));
        }

        private static void WriteSubsetFont(string inputPath, string outputPath, IEnumerable<int> codepoints)
        {
            var output = TrueTypeSubsetter.SubsetFont(File.ReadAllBytes(inputPath), codepoints);
            var fullOutputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));
            if (File.Exists(fullOutputPath) && File.ReadAllBytes(fullOutputPath).SequenceEqual(output))
            {
                File.SetLastWriteTimeUtc(fullOutputPath, DateTime.UtcNow);
                return;
            }

            File.WriteAllBytes(fullOutputPath, output);
        }

        private sealed class IconItem
        {
            internal IconItem(string name, int codePoint)
            {
                Name = name;
                CodePoint = codePoint;
            }

            internal string Name { get; set; }

            internal int CodePoint { get; }
        }

        private sealed class FontAssetManifest
        {
            public string Source { get; set; }

            public string Version { get; set; }

            public List<FontAssetRecord> Assets { get; set; }
        }

        private sealed class FontAssetRecord
        {
            public string File { get; set; }

            public string Sha256 { get; set; }

            public int ExpectedEntries { get; set; }
        }
    }
}
