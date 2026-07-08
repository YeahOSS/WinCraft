using System;
using Windows.Win32;
using WinCraft.Compatibility;
using WinCraft.Infrastructure.FileSystem;
using WinCraft.Interop;

namespace WinCraft.Infrastructure.Shell
{
    /// <summary>
    /// Parses command-line strings, including mshta.exe vbscript patterns.
    /// </summary>
    internal static class CommandLineParser
    {
        /// <summary>
        /// Extracts the executable path from a command string, stripping arguments.
        /// Supports both plain and mshta.exe-encoded commands.
        /// </summary>
        public static string ExtractExecutablePath(string command)
        {
            if (StringCompat.IsNullOrWhiteSpace(command))
                return null;

            var info = Parse(command);
            return StringCompat.IsNullOrWhiteSpace(info.File) ? null : info.File;
        }

        /// <summary>
        /// Parses a command string into its components.
        /// Handles both <c>"path" args</c> and
        /// <c>mshta.exe vbscript:createobject("shell.application").shellexecute(...)</c>.
        /// </summary>
        public static CommandInfo Parse(string command)
        {
            if (StringCompat.IsNullOrWhiteSpace(command))
                return CommandInfo.Empty;

            string trimmed = command.Trim();

            if (trimmed.StartsWith("mshta.exe ", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("mshta ", StringComparison.OrdinalIgnoreCase))
            {
                return ParseMshta(trimmed);
            }

            return ParsePlain(trimmed);
        }

        /// <summary>
        /// Creates a command string with optional verb, working directory, and window style.
        /// </summary>
        public static string CreateCommand(
            string filePath,
            string arguments = null,
            string verb = null,
            string workingDir = null,
            int windowStyle = 0)
        {
            var info = new CommandInfo
            {
                File = filePath ?? string.Empty,
                Args = arguments ?? string.Empty,
                Dir = workingDir,
                Verb = verb,
                Style = windowStyle
            };

            return info.ToCommandString();
        }

        private static unsafe CommandInfo ParsePlain(string command)
        {
            var info = CommandInfo.Empty;

            // Use the OS parser for correct handling of backslash-quote rules.
            string[] argv = PInvoke.CommandLineToArgv(command);
            if (argv.Length > 0)
                info.File = argv[0];

            // PathGetArgs returns a pointer into the original string after the
            // executable token.  Pin the string ourselves so the pointer stays
            // valid while we read the null-terminated result.
            fixed (char* pCmd = command)
            {
                var argsPtr = PInvoke.PathGetArgs(pCmd);
                if (argsPtr.Value != null && *argsPtr.Value != '\0')
                    info.Args = new string(argsPtr.Value);
            }

            return info;
        }

        private static CommandInfo ParseMshta(string command)
        {
            // mshta.exe vbscript:createobject("shell.application").shellexecute("file","args","dir","verb",style)
            int paren = command.IndexOf("shellexecute(", StringComparison.OrdinalIgnoreCase);
            if (paren < 0)
                return CommandInfo.Empty;

            // Move past "shellexecute(" to the first parameter.
            return ParseShellExecuteParams(command, command.IndexOf('(', paren) + 1);
        }

        private static CommandInfo ParseShellExecuteParams(string command, int start)
        {
            var info = CommandInfo.Empty;
            var values = new string[5];
            int valueIdx = 0;

            while (valueIdx < values.Length && start < command.Length)
            {
                while (start < command.Length && (command[start] == ' ' || command[start] == ','))
                    start++;
                if (start >= command.Length || command[start] == ')')
                    break;

                string value;
                if (command[start] == '"')
                {
                    start++;
                    int end = start;
                    while (end < command.Length)
                    {
                        if (command[end] == '\\' && end + 1 < command.Length)
                            end++;
                        else if (command[end] == '"')
                            break;
                        end++;
                    }

                    value = command.Substring(start, end - start).Replace("\\\"", "\"").Replace("\\\\", "\\");
                    start = end + 1;
                }
                else
                {
                    int end = start;
                    while (end < command.Length && command[end] != ',' && command[end] != ')')
                        end++;
                    value = command.Substring(start, end - start).Trim();
                    start = end;
                }

                values[valueIdx++] = value;
            }

            info.File = valueIdx > 0 ? values[0] : string.Empty;
            info.Args = valueIdx > 1 ? values[1] : string.Empty;
            info.Dir  = valueIdx > 2 ? values[2] : string.Empty;
            info.Verb = valueIdx > 3 ? values[3] : string.Empty;
            if (valueIdx > 4 && int.TryParse(values[4], out int style))
                info.Style = style;

            return info;
        }

        /// <summary>
        /// Extracts the executable from <paramref name="command"/> and resolves it
        /// to a fully qualified path.  Handles plain <c>"path" args</c> and
        /// mshta.exe-encoded commands.  Returns null when no executable can be identified.
        /// </summary>
        public static string ResolveExecutablePath(string command)
        {
            if (StringCompat.IsNullOrWhiteSpace(command))
                return null;

            string candidate = ExtractExecutablePath(command);
            if (StringCompat.IsNullOrWhiteSpace(candidate))
                return null;

            string resolved = PathUtility.FindPath(candidate);
            if (resolved != null)
                return resolved;

            // If the command specifies a path directly, verify it is an executable.
            if (PInvoke.PathIsExe(candidate))
                return candidate;

            return null;
        }

    }
}
