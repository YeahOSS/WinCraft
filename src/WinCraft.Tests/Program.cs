using System;
using System.IO;
using NUnitLite;

namespace WinCraft.Tests
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            return new AutoRun().Execute(WithDefaultResultPath(args));
        }

        private static string[] WithDefaultResultPath(string[] args)
        {
            if (HasResultOption(args))
                return args;

            var inputLength = args?.Length ?? 0;
            var runnerArgs = new string[inputLength + 1];
            if (inputLength > 0)
                Array.Copy(args, runnerArgs, inputLength);

            runnerArgs[inputLength] = "--result=" + GetDefaultResultPath();
            return runnerArgs;
        }

        private static bool HasResultOption(string[] args)
        {
            if (args == null)
                return false;

            foreach (var argument in args)
            {
                if (IsResultOption(argument))
                    return true;
            }

            return false;
        }

        private static bool IsResultOption(string argument)
        {
            if (string.IsNullOrEmpty(argument))
                return false;

            var option = argument.TrimStart('-', '/');
            return string.Equals(option, "result", StringComparison.OrdinalIgnoreCase)
                || option.StartsWith("result=", StringComparison.OrdinalIgnoreCase)
                || option.StartsWith("result:", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetDefaultResultPath()
        {
            var sourceDirectory = TryGetSourceDirectory();
            var resultDirectory = sourceDirectory == null
                ? AppDomain.CurrentDomain.BaseDirectory
                : sourceDirectory.FullName;

            return Path.Combine(resultDirectory, "TestResult.xml");
        }

        private static DirectoryInfo TryGetSourceDirectory()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                if (string.Equals(directory.Name, "src", StringComparison.OrdinalIgnoreCase))
                    return directory;

                directory = directory.Parent;
            }

            return null;
        }
    }
}
