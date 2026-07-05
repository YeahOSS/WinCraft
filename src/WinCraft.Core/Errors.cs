namespace WinCraft
{
    /// <summary>
    /// Shared error messages for the entire project.
    /// </summary>
    internal static class Errors
    {
        public const string ValueCannotBeEmpty = "Value cannot be empty.";
        public const string SubkeyPathRequired = "The registry subkey path is required.";
        public const string SourceSubkeyPathRequired = "The registry source subkey path is required.";
        public const string DestSubkeyPathRequired = "The registry destination subkey path is required.";
        public const string NotValidDWord = "The DWord value data is not a valid 32-bit integer.";
        public const string NotValidQWord = "The QWord value data is not a valid 64-bit integer.";
        public const string ExecutablePathRequired = "The executable path is required.";
        public const string NullOrEmpty = "The value cannot be null or empty.";
        public const string NullOrWhiteSpace = "The value cannot be null, empty, or whitespace.";
    }
}
