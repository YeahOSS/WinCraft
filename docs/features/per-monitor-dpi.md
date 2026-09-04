# Per-Monitor DPI

## Process configuration

| Layer | Owner | Contract |
|-------|-------|----------|
| Process manifest | `WinCraft/Properties/app.manifest` | `PerMonitorV2, PerMonitor` selects PerMonitorV2 where supported, with PerMonitor and `true/PM` fallbacks for older Windows |
| WPF runtime switches | `WinCraft/Properties/app.config` | Enables the WPF DPI-change path on supported .NET Framework runtimes |
| Code startup | `WpfApplicationInitializer.ConfigureRuntime()` | Applies the same WPF switches before constructing `Application` |

`WinCraft`, `WinCraft.Portable`, and `WinCraft.Gallery` embed the shared
manifest. The first recognized `dpiAwareness` value wins, so retain its order.
PerMonitorV2 is recognized starting with Windows 10 version 1703; Windows 10
version 1607 uses the `PerMonitor` fallback. See the [Microsoft manifest
reference](https://learn.microsoft.com/en-us/windows/win32/sbscs/application-manifests)
and [DPI-awareness setup guidance](https://learn.microsoft.com/en-us/windows/win32/hidpi/setting-the-default-dpi-awareness-for-a-process).

## Coordinate boundary

- The manifest owns process awareness; do not add a later process-wide DPI setter.
- Keep Win32 screen geometry in physical pixels: window rectangles, monitor work
  areas, and DPI-specific system metrics remain native until a WPF conversion is
  explicitly required.
- Do not use WPF `Left` / `Top` as cross-monitor physical coordinates under
  .NET Framework PerMonitorV2. See [Custom Window Chrome](custom-window-chrome.md#startup-positioning)
  for its bounded correction and the upstream WPF issue.
- Screen-relative WPF popups should target a visual and use WPF placement
  coordinates; `MessageTip` uses a custom placement callback rather than an
  `AbsolutePoint` calculation from window coordinates. It refreshes that
  placement while its owner moves or resizes because WPF popups do not follow a
  moved parent automatically.
