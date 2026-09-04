namespace WinCraft.UI
{
    /// <summary>
    /// Window backdrop material type, with cascading fallback across OS versions.
    /// </summary>
    public enum WindowBackdropType
    {
        /// <summary>No backdrop effect. Uses the window's solid Background brush.</summary>
        None = 0,

        /// <summary>
        /// Mica material (Win11 22H2+ preferred, Win11 21H2 with undocumented API).
        /// Uses Acrylic on Windows 10 when the Mica material is unavailable.
        /// </summary>
        Mica,

        /// <summary>
        /// Mica's alternate variant (Win11 22H2+). This is the material
        /// intended for windows with a tabbed title bar.
        /// </summary>
        MicaAlt,

        /// <summary>
        /// Desktop Acrylic material. Uses the Windows 11 system-backdrop API
        /// and the Accent Policy implementation on Windows 10 1803+.
        /// </summary>
        Acrylic,

        /// <summary>
        /// Automatically select the best available backdrop for the current OS version.
        /// Win11 22H2+ → Mica, Win10 1803+ → Acrylic, Vista/Win7 → an internal
        /// legacy blur implementation, otherwise None.
        /// </summary>
        Auto,
    }
}
