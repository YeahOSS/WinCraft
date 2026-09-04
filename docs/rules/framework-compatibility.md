# Framework Compatibility

## Target Frameworks

Multi-targets `net30` (Legacy) and `net45` (Standard).  Shared version
numbers live in `publish/version.props`.

## Theraot

`Theraot.Core` backfills APIs absent in `net30` (LINQ, tuples, delegates,
collections, caller info, expression trees) and supplements post-4.5 APIs
on `net45` (`HashCode`, `Index`, `Range`, `IsExternalInit`, nullable
attributes).  Both targets need it — the package reference is unconditional.

## Build Validation

Run the net45 build with an explicit project or solution path; its post-build
validation invokes the net30 toolchain outside Visual Studio.

| Command | Use |
|------|-----|
| `dotnet build src/WinCraft.slnx -f net45` | Day-to-day validation; also runs net30 validation |
| `dotnet build src/WinCraft.Tests/WinCraft.Tests.csproj -f net45` then `src/bin/Debug/net45/WinCraft.Tests.exe` | Test validation |
| `publish/build.ps1 -BuildOnly` | Release readiness |

The post-build event in `WinCraft.csproj` validates net30 automatically
outside Visual Studio (`Net30ValidationBuild=true` guard prevents re-entrant
loops).

Do not report a standalone `dotnet build -f net30` failure as a bug — use the net45 build path above.

## WPF Template Bindings

- For an attached dependency property on a templated parent, use `TemplateBinding` (for example, `{TemplateBinding ui:Design.Icon}`).
- Do not use `{Binding (ui:Design.Icon), RelativeSource={RelativeSource TemplatedParent}}`; the Legacy BAML path can fall back to the dependency property's default value.

## Compatibility Helpers

Prefer `WinCraft.Compatibility` helpers over `#if` blocks.  Available:

| Helper | Backfills | Methods |
|--------|-----------|---------|
| `StringCompat` | `string` | `IsNullOrWhiteSpace(string)` |
| `EnumCompat` | `Enum` | `TryParse<TEnum>(string, bool, out TEnum)` |
| `GuidCompat` | `Guid` | `TryParse(string, out Guid)` |
| `PathCompat` | `Path` | `Combine` with 3+ parameters |
| `RegistryKeyCompat` | `RegistryKey` | `DeleteSubKeyTree(key, name)`, `DeleteSubKey(key, name)` |
| `ThrowCompat` | — | `IfNull<T>(T, string)` |
| `FrameworkCompat` | Target framework | `IsNet30` (bool const) |
| `TextBoxBaseCompat` | `TextBoxBase`, `PasswordBox` | `EnableNonAdornerSelectionRendering()`, `SetTextInputBrushes(d, caret, selection, selectionText)` |
| `MarshalCompat` | `Marshal` | `PtrToStructure<T>(IntPtr)`, `StructureToPtr<T>(T, IntPtr, bool)` — non-generic overloads for net45 |
| `DependencyObjectCompat` | `DependencyObject` | `SetControlValue(d, property, value)` — calls `SetCurrentValue` on net45, `SetValue` otherwise |
| `MathCompat` | `Math` | `Clamp(double, double, double)` — backfills `Math.Clamp` (introduced in .NET Core 2.0) |
| `TextOptionsCompat` | `TextOptions` | `ApplyIdealTextRendering(DependencyObject)` — sets `TextFormattingMode.Ideal` + `TextRenderingMode.Grayscale` on net45, no-op on net30 |
| `JitCompat` | `ProfileOptimization` | `ConfigureMulticoreJit()` — calls `SetProfileRoot` + `StartProfile` on net45, no-op on net30 |

Add to this table when adding a new helper.

## Language Version

Fixed in the project file.  Do not switch to `latest` or `preview` unless
explicitly requested.
