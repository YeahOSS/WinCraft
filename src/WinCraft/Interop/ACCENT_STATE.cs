namespace Windows.Win32
{
    /// <summary>
    /// Accent state for <c>SetWindowCompositionAttribute</c>.
    /// CsWin32 cannot generate this API or its associated types — it is an
    /// undocumented user32 export used for acrylic and blur-behind effects.
    /// </summary>
    internal enum ACCENT_STATE
    {
        ACCENT_DISABLED,
        ACCENT_ENABLE_GRADIENT,
        ACCENT_ENABLE_TRANSPARENTGRADIENT,
        ACCENT_ENABLE_BLURBEHIND,
        ACCENT_ENABLE_ACRYLICBLURBEHIND,
        ACCENT_ENABLE_HOSTBACKDROP,
        ACCENT_INVALID_STATE,
    }
}
