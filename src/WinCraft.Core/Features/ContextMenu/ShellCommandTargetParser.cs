using System;
using System.IO;
using WinCraft.Compatibility;

namespace WinCraft.Features.ContextMenu
{
    /// <summary>
    /// Extracts the executable path from a shell command string.
    /// </summary>
    internal static class ShellCommandTargetParser
    {
        public static string GetExecutablePath(string command)
        {
            if (StringCompat.IsNullOrWhiteSpace(command))
                return null;

            string trimmed = command.Trim();
            if (trimmed.Length == 0)
                return null;

            string candidate = trimmed[0] == '"'
                ? ReadQuotedToken(trimmed)
                : ReadLikelyPath(trimmed);
            return StringCompat.IsNullOrWhiteSpace(candidate) ? null : candidate;
        }

        private static string ReadQuotedToken(string command)
        {
            int endIndex = command.IndexOf('"', 1);
            return endIndex < 0 ? command.Trim('"') : command.Substring(1, endIndex - 1);
        }

        private static string ReadLikelyPath(string command)
        {
            int exeIndex = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exeIndex >= 0)
                return command.Substring(0, exeIndex + 4).Trim();

            int whitespaceIndex = command.IndexOf(' ');
            if (whitespaceIndex < 0)
                return command.Trim();

            string firstToken = command.Substring(0, whitespaceIndex).Trim();
            return Path.HasExtension(firstToken) ? firstToken : null;
        }
    }
}
