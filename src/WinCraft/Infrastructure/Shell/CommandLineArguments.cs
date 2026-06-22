using System;

namespace WinCraft.Infrastructure.Shell
{
    /// <summary>
    /// Reads simple command-line flags and option values.
    /// </summary>
    internal static class CommandLineArguments
    {
        public static bool Contains(string[] args, string name)
        {
            if (args == null || string.IsNullOrEmpty(name))
                return false;

            for (int index = 0; index < args.Length; index++)
            {
                var argument = args[index];
                if (string.Equals(argument, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static int GetInt32Value(string[] args, string name)
        {
            var value = GetValue(args, name);
            if (string.IsNullOrEmpty(value))
                return 0;

            return int.TryParse(value, out int parsedValue) ? parsedValue : 0;
        }

        public static bool TryGetInt32Value(string[] args, string name, out int value)
        {
            value = 0;

            var rawValue = GetValue(args, name);
            if (string.IsNullOrEmpty(rawValue))
                return false;

            return int.TryParse(rawValue, out value);
        }

        public static string GetValue(string[] args, string name)
        {
            if (args == null || string.IsNullOrEmpty(name))
                return null;

            for (int index = 0; index < args.Length; index++)
            {
                var argument = args[index];
                if (!string.Equals(argument, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                var valueIndex = index + 1;
                if (valueIndex >= args.Length)
                    return null;

                return args[valueIndex];
            }

            return null;
        }
    }
}
