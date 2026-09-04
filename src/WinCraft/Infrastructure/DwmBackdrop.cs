using System;
using System.Runtime.InteropServices;
using WinCraft.Infrastructure.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace WinCraft.Infrastructure
{
    internal enum DwmBackdropType
    {
        None,
        Mica,
        MicaAlt,
        Acrylic,
        LegacyBlurBehind,
        Auto,
    }

    /// <summary>
    /// OS-version-gated window backdrop effects. Windows 11 uses its native
    /// system-backdrop attributes for Mica, MicaAlt, and Acrylic. The internal
    /// legacy blur fallback uses Accent Policy before classic DWM.
    /// Mica auto-dims when the window loses focus; <c>WindowBase.KeepActiveVisuals</c>
    /// keeps the DWM-managed visual state active (gated to Win11 21H2+).
    /// Legacy Acrylic (SetWindowCompositionAttribute) has a drag-lag
    /// performance bug; use <see cref="BeginDragFallback"/> /
    /// <see cref="EndDragFallback"/> to temporarily switch to legacy blur.
    /// </summary>
    internal static class DwmBackdrop
    {
        /// <summary>
        /// Apply the requested backdrop effect using the implementation
        /// available for the current OS version.
        /// </summary>
        internal static void Apply(IntPtr hwnd, DwmBackdropType type, int acrylicGradientColor)
        {
            var resolved = ResolveBackdrop(type);
            if (resolved == DwmBackdropType.None)
            {
                ClearBackdrop(hwnd);
                return;
            }

            ApplyBackdrop(hwnd, resolved, acrylicGradientColor);
        }

        internal static void ApplyBackdrop(IntPtr hwnd, DwmBackdropType resolved, int acrylicGradientColor)
        {
            // Win11 22H2+: Mica and Acrylic use the native system-backdrop
            // attribute. Do not fall back to the legacy Accent Policy path.
            if (WindowsVersion.IsAtLeast(WindowsRelease.Win11_22H2)
                && (resolved == DwmBackdropType.Mica
                    || resolved == DwmBackdropType.MicaAlt
                    || resolved == DwmBackdropType.Acrylic))
            {
                ClearLegacyBackdrop(hwnd);
                ApplySystemBackdrop(hwnd, resolved);
                return;
            }

            // Win11 21H2: Mica has only the native undocumented attribute.
            if (WindowsVersion.IsAtLeast(WindowsRelease.Win11_21H2)
                && (resolved == DwmBackdropType.Mica || resolved == DwmBackdropType.MicaAlt)
                && !WindowsVersion.IsAtLeast(WindowsRelease.Win11_22H2))
            {
                ClearLegacyBackdrop(hwnd);
                ApplyMicaUndocumented(hwnd, enable: true);
                return;
            }

            if (resolved == DwmBackdropType.LegacyBlurBehind)
            {
                ClearSystemBackdrop(hwnd);
                TryLegacyBlurBehind(hwnd);
                return;
            }

            // Mica, MicaAlt, and Acrylic map to Accent Acrylic on Windows 10.
            // Do not substitute legacy blur when a public material is unavailable.
            if (resolved == DwmBackdropType.Mica
                || resolved == DwmBackdropType.MicaAlt
                || resolved == DwmBackdropType.Acrylic)
            {
                ClearSystemBackdrop(hwnd);
                ApplyAcrylic(hwnd, acrylicGradientColor);
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(resolved));
        }

        // ── Drag fallback ──

        /// <summary>
        /// Temporarily switch from legacy Acrylic to lightweight legacy blur during
        /// move/resize drag.  <c>SetWindowCompositionAttribute</c> Acrylic has
        /// a known performance bug where the window lags behind the cursor;
        /// <c>ACCENT_ENABLE_BLURBEHIND</c> avoids this because DWM does not
        /// pause the legacy blur accent during drag.
        /// No-op when the backdrop uses <c>DWMWA_SYSTEMBACKDROP_TYPE</c> or the
        /// undocumented Mica API (Win11 21H2+), neither of which has the bug.
        /// </summary>
        internal static void BeginDragFallback(IntPtr hwnd, DwmBackdropType type)
        {
            if (WindowsVersion.IsAtLeast(WindowsRelease.Win11_22H2))
                return;

            var resolved = ResolveBackdrop(type);
            if (resolved != DwmBackdropType.Mica
                && resolved != DwmBackdropType.MicaAlt
                && resolved != DwmBackdropType.Acrylic)
                return;

            if ((resolved == DwmBackdropType.Mica || resolved == DwmBackdropType.MicaAlt)
                && WindowsVersion.IsAtLeast(WindowsRelease.Win11_21H2))
            {
                return;
            }

            // Accent Blur (no acrylic tint) — lightweight, no drag lag.
            if (WindowsVersion.IsAtLeast(WindowsRelease.Win10_1803))
                TrySetAccentPolicy(hwnd, ACCENT_STATE.ACCENT_ENABLE_BLURBEHIND, 0);
        }

        /// <summary>
        /// Restore the original backdrop after drag ends.
        /// Only meaningful after a preceding <see cref="BeginDragFallback"/>.
        /// </summary>
        internal static void EndDragFallback(IntPtr hwnd, DwmBackdropType type, int gradientColor)
        {
            if (WindowsVersion.IsAtLeast(WindowsRelease.Win11_22H2))
                return;

            var resolved = ResolveBackdrop(type);
            if (resolved == DwmBackdropType.None) return;
            ApplyBackdrop(hwnd, resolved, gradientColor);
        }

        private static void ApplySystemBackdrop(IntPtr hwnd, DwmBackdropType type)
        {
            var backdrop = type switch
            {
                DwmBackdropType.None => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE,
                DwmBackdropType.Mica => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW,
                DwmBackdropType.MicaAlt => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TABBEDWINDOW,
                DwmBackdropType.Acrylic => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW,
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };

            DwmWindowAttribute.Set(
                hwnd,
                DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                (int)backdrop);
        }

        private static void ApplyMicaUndocumented(IntPtr hwnd, bool enable)
        {
            const int dwmwaMica = 1029;
            DwmWindowAttribute.Set(
                hwnd,
                (DWMWINDOWATTRIBUTE)dwmwaMica,
                enable ? 1 : 0);
        }

        /// <summary>
        /// Resolve Auto to the best available backdrop for the current OS.
        /// </summary>
        internal static DwmBackdropType ResolveBackdrop(DwmBackdropType preferred)
        {
            if (preferred != DwmBackdropType.Auto)
                return preferred;

            if (WindowsVersion.IsAtLeast(WindowsRelease.Win11_22H2))
                return DwmBackdropType.Mica;
            if (WindowsVersion.IsAtLeast(WindowsRelease.Win10_1803))
                return DwmBackdropType.Acrylic;
            if (WindowsVersion.IsAtLeast(WindowsRelease.Vista)
                && WindowsVersion.IsBelow(WindowsRelease.Win8))
                return DwmBackdropType.LegacyBlurBehind;
            return DwmBackdropType.None;
        }

        // ── Acrylic (Win10 1803+) ──

        private static void ApplyAcrylic(IntPtr hwnd, int gradientColor)
        {
            if (!WindowsVersion.IsAtLeast(WindowsRelease.Win10_1803))
                return;

            TrySetAccentPolicy(
                hwnd,
                ACCENT_STATE.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                gradientColor);
        }

        private static bool TrySetAccentPolicy(
            IntPtr hwnd,
            ACCENT_STATE accentState,
            int gradientColor)
        {
            try
            {
                var accent = new ACCENT_POLICY
                {
                    AccentState = accentState,
                    AccentFlags = 0,
                    GradientColor = gradientColor,
                    AnimationId = 0,
                };

                int accentSize = Marshal.SizeOf(typeof(ACCENT_POLICY));
                IntPtr accentPtr = Marshal.AllocHGlobal(accentSize);
                try
                {
                    Marshal.StructureToPtr(accent, accentPtr, false);

                    var data = new WINDOWCOMPOSITIONATTRIBDATA
                    {
                        Attribute = PInvoke.WCA_ACCENT_POLICY,
                        Data = accentPtr,
                        SizeOfData = accentSize,
                    };

                    return PInvoke.SetWindowCompositionAttribute(hwnd, ref data);
                }
                finally
                {
                    Marshal.FreeHGlobal(accentPtr);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to set accent policy ({accentState}): {ex.Message}");
                return false;
            }
        }

        // ── Legacy blur ──

        private static void ClearBackdrop(IntPtr hwnd)
        {
            ClearSystemBackdrop(hwnd);
            ClearLegacyBackdrop(hwnd);
        }

        private static void ClearSystemBackdrop(IntPtr hwnd)
        {
            if (WindowsVersion.IsAtLeast(WindowsRelease.Win11_22H2))
                ApplySystemBackdrop(hwnd, DwmBackdropType.None);
            else if (WindowsVersion.IsAtLeast(WindowsRelease.Win11_21H2))
                ApplyMicaUndocumented(hwnd, enable: false);
        }

        private static void ClearLegacyBackdrop(IntPtr hwnd)
        {
            if (WindowsVersion.IsAtLeast(WindowsRelease.Win10_1709))
            {
                TrySetAccentPolicy(hwnd, ACCENT_STATE.ACCENT_DISABLED, 0);
                return;
            }

            if (WindowsVersion.IsBelow(WindowsRelease.Win8))
                TryClassicBlurBehind(hwnd, enable: false);
        }

        private static void TryLegacyBlurBehind(IntPtr hwnd)
        {
            // Accent Policy is the usable blur implementation on modern
            // Windows. DwmEnableBlurBehindWindow is for down-level Windows.
            if (WindowsVersion.IsAtLeast(WindowsRelease.Win10_1709)
                && TrySetAccentPolicy(
                    hwnd,
                    ACCENT_STATE.ACCENT_ENABLE_BLURBEHIND,
                    0))
            {
                return;
            }

            if (WindowsVersion.IsBelow(WindowsRelease.Win8))
                TryClassicBlurBehind(hwnd, enable: true);
        }

        private static void TryClassicBlurBehind(IntPtr hwnd, bool enable)
        {
            if (!WindowsVersion.IsAtLeast(WindowsRelease.Vista))
                return;

            var bb = new DWM_BLURBEHIND
            {
                dwFlags = PInvoke.DWM_BB_ENABLE | PInvoke.DWM_BB_TRANSITIONONMAXIMIZED,
                fEnable = enable,
                fTransitionOnMaximized = enable,
            };

            PInvoke.DwmEnableBlurBehindWindow((HWND)hwnd, in bb);
        }
    }

    /// <summary>
    /// DWM window corner rounding and composition state queries.
    /// </summary>
    internal static class DwmCorner
    {
        /// <summary>
        /// Apply the DWM corner rounding preference (Win11 only).
        /// Below Win11 no DWM API rounds the frame; corners stay square.
        /// </summary>
        internal static void Apply(IntPtr hwnd, DWM_WINDOW_CORNER_PREFERENCE preference)
        {
            if (!WindowsVersion.IsAtLeast(WindowsRelease.Win11_21H2))
                return;

            DwmWindowAttribute.Set(
                hwnd,
                DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
                (int)preference);
        }

        /// <summary>
        /// Enable DWM non-client rendering so the shadow is drawn for a
        /// window that handles NC area itself.
        /// </summary>
        internal static void EnableNcRendering(IntPtr hwnd)
        {
            if (!IsDwmAvailable()) return;
            DwmWindowAttribute.Set(
                hwnd,
                DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_POLICY,
                (int)DWMNCRENDERINGPOLICY.DWMNCRP_ENABLED);
        }

        /// <summary>
        /// Check whether DWM composition is available and enabled.
        /// </summary>
        internal static bool IsDwmAvailable()
        {
            if (!WindowsVersion.IsAtLeast(WindowsRelease.Vista))
                return false;

            return PInvoke.DwmIsCompositionEnabled(out BOOL enabled).Succeeded && enabled;
        }
    }

    /// <summary>
    /// Per-window immersive dark mode support (Win10 1809+).
    /// </summary>
    internal static class DwmDarkMode
    {
        internal static void Apply(IntPtr hwnd, bool useDark)
        {
            if (!WindowsVersion.IsAtLeast(WindowsRelease.Win10_1809))
                return;

            DwmWindowAttribute.Set(
                hwnd,
                DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                useDark ? 1 : 0);
        }
    }

    internal static class DwmWindowAttribute
    {
        internal static unsafe HRESULT Set(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, int value)
        {
            return PInvoke.DwmSetWindowAttribute((HWND)hwnd, attribute, &value, sizeof(int));
        }

        internal static unsafe bool TryGetExtendedFrameBounds(IntPtr hwnd, out RECT bounds)
        {
            bounds = default;
            fixed (RECT* pBounds = &bounds)
            {
                return PInvoke.DwmGetWindowAttribute(
                    (HWND)hwnd,
                    DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
                    pBounds,
                    (uint)sizeof(RECT)).Succeeded;
            }
        }

    }
}
