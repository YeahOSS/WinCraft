namespace WinCraft.UI
{
    /// <summary>
    /// DWM color mode for Win11+ window colors (border, caption, caption text).
    /// </summary>
    public enum DwmColorMode
    {
        /// <summary>Use the system default color.</summary>
        Default = 0,
        /// <summary>Suppresses the border; with an active backdrop, extends it through the caption. Not meaningful for caption text.</summary>
        None = -1,
        /// <summary>Use the custom color from the paired color property.</summary>
        Custom = 1,
    }
}
