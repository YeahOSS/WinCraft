using System.Runtime.InteropServices;

namespace Windows.Win32
{
    /// <summary>
    /// Accent policy for <c>SetWindowCompositionAttribute</c>.
    /// CsWin32 cannot generate this API or its associated types.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ACCENT_POLICY
    {
        public ACCENT_STATE AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }
}
