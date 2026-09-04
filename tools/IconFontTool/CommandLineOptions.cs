using System;
using System.Collections.Generic;

namespace WinCraft.IconFontTool
{
    internal sealed class CommandLineOptions
    {
        private readonly Dictionary<string, string> values;

        private CommandLineOptions(Dictionary<string, string> values)
        {
            this.values = values;
        }

        internal static CommandLineOptions Parse(string[] args, int startIndex)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = startIndex; index < args.Length; index++)
            {
                var argument = args[index];
                if (!argument.StartsWith("--", StringComparison.Ordinal) || argument.Length == 2)
                    throw new ArgumentException("Expected an option name, but found: " + argument);

                var separatorIndex = argument.IndexOf('=');
                string name;
                string value;
                if (separatorIndex >= 0)
                {
                    name = argument.Substring(2, separatorIndex - 2);
                    value = argument.Substring(separatorIndex + 1);
                }
                else
                {
                    name = argument.Substring(2);
                    if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                        throw new ArgumentException("Missing value for --" + name + ".");
                    value = args[++index];
                }

                if (values.ContainsKey(name))
                    throw new ArgumentException("Option --" + name + " was specified more than once.");
                values.Add(name, value);
            }

            return new CommandLineOptions(values);
        }

        internal string GetRequired(string name)
        {
            if (!values.TryGetValue(name, out string value) || string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Missing required option --" + name + ".");
            return value;
        }

        internal string GetOptional(string name)
        {
            return values.TryGetValue(name, out string value) ? value : null;
        }
    }
}
