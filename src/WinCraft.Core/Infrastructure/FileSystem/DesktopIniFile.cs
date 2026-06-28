using System;
using System.Collections.Generic;
using System.IO;

namespace WinCraft.Infrastructure.FileSystem
{
    /// <summary>
    /// Reads and updates simple desktop.ini values used by shell folders.
    /// </summary>
    internal sealed class DesktopIniFile(string directoryPath)
    {
        private readonly string _path = Path.Combine(directoryPath, "desktop.ini");

        public string FilePath => _path;

        public string GetValue(string sectionName, string keyName)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(_path);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }

            string currentSection = null;
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed[0] == ';')
                    continue;

                if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                    continue;
                }

                if (!string.Equals(currentSection, sectionName, StringComparison.OrdinalIgnoreCase))
                    continue;

                int equalsIndex = line.IndexOf('=');
                if (equalsIndex < 0)
                    continue;

                string key = line.Substring(0, equalsIndex).Trim();
                if (string.Equals(key, keyName, StringComparison.OrdinalIgnoreCase))
                    return line.Substring(equalsIndex + 1).Trim();
            }

            return null;
        }

        public string CreateContentWithValue(string sectionName, string keyName, string value)
        {
            var lines = new List<string>();
            try
            {
                lines.AddRange(File.ReadAllLines(_path));
            }
            catch (FileNotFoundException)
            {
                // File does not exist — start with empty content.
            }
            catch (DirectoryNotFoundException)
            {
                // Directory does not exist — start with empty content.
            }

            string currentSection = null;
            int sectionEndIndex = lines.Count;
            bool sectionFound = false;
            for (int index = 0; index < lines.Count; index++)
            {
                string trimmed = lines[index].Trim();
                if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
                {
                    if (string.Equals(currentSection, sectionName, StringComparison.OrdinalIgnoreCase))
                        sectionEndIndex = index;
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                    if (string.Equals(currentSection, sectionName, StringComparison.OrdinalIgnoreCase))
                        sectionFound = true;
                    continue;
                }

                if (!string.Equals(currentSection, sectionName, StringComparison.OrdinalIgnoreCase))
                    continue;

                int equalsIndex = lines[index].IndexOf('=');
                if (equalsIndex < 0)
                    continue;

                string key = lines[index].Substring(0, equalsIndex).Trim();
                if (string.Equals(key, keyName, StringComparison.OrdinalIgnoreCase))
                {
                    lines[index] = keyName + "=" + (value ?? string.Empty);
                    return string.Join(Environment.NewLine, lines.ToArray()) + Environment.NewLine;
                }
            }

            if (!sectionFound)
            {
                if (lines.Count > 0 && lines[lines.Count - 1].Length != 0)
                    lines.Add(string.Empty);
                lines.Add("[" + sectionName + "]");
                lines.Add(keyName + "=" + (value ?? string.Empty));
                return string.Join(Environment.NewLine, lines.ToArray()) + Environment.NewLine;
            }

            lines.Insert(sectionEndIndex, keyName + "=" + (value ?? string.Empty));
            return string.Join(Environment.NewLine, lines.ToArray()) + Environment.NewLine;
        }
    }
}
