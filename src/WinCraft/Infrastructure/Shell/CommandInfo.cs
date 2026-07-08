using WinCraft.Infrastructure.FileSystem;

namespace WinCraft.Infrastructure.Shell
{
    /// <summary>
    /// Parsed shell command: file, arguments, working directory, verb, and window style.
    /// </summary>
    internal struct CommandInfo
    {
        public string File;
        public string Args;
        public string Dir;
        public string Verb;
        public int Style; // SW_SHOW etc.

        public static CommandInfo Empty => new()
        {
            File = string.Empty,
            Args = string.Empty,
            Dir = string.Empty,
            Verb = string.Empty,
        };

        public readonly bool HasVerb => !string.IsNullOrEmpty(Verb);

        public readonly bool HasDir => !string.IsNullOrEmpty(Dir);

        /// <summary>
        /// Creates a plain quoted command: "File" [Args].
        /// </summary>
        public readonly string ToCommandString()
        {
            if (HasVerb || HasDir || Style != 0)
                return ToMshtaString();

            string quoted = PathUtility.QuoteSpaces(File);
            if (string.IsNullOrEmpty(Args))
                return quoted;

            return quoted + " " + Args;
        }

        private readonly string ToMshtaString()
        {
            string escapedFile = (File ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
            string escapedArgs = (Args ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
            string escapedDir = (Dir ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
            string escapedVerb = Verb ?? string.Empty;

            string script = "vbscript:createobject(\"shell.application\")"
                + ".shellexecute(\"" + escapedFile + "\","
                + "\"" + escapedArgs + "\","
                + "\"" + escapedDir + "\","
                + "\"" + escapedVerb + "\","
                + Style + ")";

            return "mshta.exe " + script;
        }
    }
}
