using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace WinCraft.IconFontTool
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length > 0 && string.Equals(args[0], "generate", StringComparison.OrdinalIgnoreCase))
                {
                    IconFontPipeline.Run(CommandLineOptions.Parse(args, 1));
                    return 0;
                }

                if (args.Length > 0 && string.Equals(args[0], "verify", StringComparison.OrdinalIgnoreCase))
                {
                    IconFontPipeline.Verify(CommandLineOptions.Parse(args, 1));
                    return 0;
                }

                var optionStart = args.Length > 0 && string.Equals(args[0], "subset", StringComparison.OrdinalIgnoreCase)
                    ? 1
                    : 0;
                var options = CommandLineOptions.Parse(args, optionStart);
                var inputPath = options.GetRequired("input");
                var outputPath = options.GetRequired("output");
                var codepoints = ParseCodepoints(options.GetRequired("unicodes"));

                var outputBytes = TrueTypeSubsetter.SubsetFont(File.ReadAllBytes(inputPath), codepoints);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));
                File.WriteAllBytes(outputPath, outputBytes);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static IEnumerable<int> ParseCodepoints(string value)
        {
            foreach (var item in value.Split([','], StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(item.Trim(), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int codepoint))
                    throw new ArgumentException("Invalid Unicode codepoint: " + item);

                yield return codepoint;
            }
        }
    }
}
