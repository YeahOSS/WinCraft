using System.Text;

namespace WinCraft.Infrastructure.Shell
{
    /// <summary>
    /// Formats command-line argument text for legacy process launching scenarios.
    /// </summary>
    internal static class ShellCommandLine
    {
        public static string BuildArgumentString(string[] args)
        {
            if (args == null || args.Length == 0)
                return string.Empty;

            var quoted = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
                quoted[i] = QuoteArgument(args[i] ?? string.Empty);

            return string.Join(" ", quoted);
        }

        // Legacy target frameworks do not provide ArgumentList, so quote arguments manually.
        public static string QuoteArgument(string value)
        {
            if (value.Length == 0)
                return "\"\"";

            bool requiresQuotes = false;

            for (int index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsWhiteSpace(character) || character == '"')
                {
                    requiresQuotes = true;
                    break;
                }
            }

            if (!requiresQuotes)
                return value;

            var builder = new StringBuilder();
            builder.Append('"');

            int backslashCount = 0;
            for (int index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character == '\\')
                {
                    backslashCount++;
                    continue;
                }

                if (character == '"')
                {
                    builder.Append('\\', backslashCount * 2 + 1);
                    builder.Append('"');
                    backslashCount = 0;
                    continue;
                }

                if (backslashCount > 0)
                {
                    builder.Append('\\', backslashCount);
                    backslashCount = 0;
                }

                builder.Append(character);
            }

            if (backslashCount > 0)
                builder.Append('\\', backslashCount * 2);

            builder.Append('"');
            return builder.ToString();
        }
    }
}
