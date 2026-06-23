# Win32 Interop

## CsWin32 Workflow

Prefer `CsWin32` for Win32 API declarations, constants, enums, and COM interfaces
such as `IShellItem`. Do not add an `Interop/` directory for Win32 bindings.

When adding a Win32 API or COM interface:

1. Add entries to `src/WinCraft/NativeMethods.txt` first.
2. Use the undecorated API or interface name, for example `CreateProcessWithTokenW`
   or `IShellItem`. COM interfaces are emitted with `[ComImport]` and `[Guid]`;
   CsWin32 also generates friendly extension methods for common patterns.
3. Build the project.
4. Inspect generated source under
   `obj/<config>/<tfm>/generated/Microsoft.Windows.CsWin32/`.
5. Use the exact generated C# signature and namespace at the call site.

Do not guess generated signatures from memory.

## Call Site Rules

When a CsWin32 API offers a friendly wrapper overload, such as `SafeFileHandle`,
`out`, or nullable value types, prefer it over the raw pointer overload. When
only the raw overload exists, note why with a short comment.

When calling Win32 APIs, use the CsWin32-generated P/Invoke wrappers by
default. Do not mix BCL wrappers such as `Process.GetCurrentProcess()` or
`Process.MainModule` with raw Win32 calls. The BCL wrappers often allocate extra
objects or surface different error semantics.

Use hand-written P/Invoke only when CsWin32 cannot emit the required binding.
Keep that exception local to the owning feature or infrastructure type, and add
a short comment explaining why CsWin32 is not used.

## Windows Compatibility

When introducing or changing Win32 API usage, explicitly consider Windows
version compatibility. Do not assume that targeting `.NET Framework 3.0`
implies support for pre-Vista Windows behavior.

Document or guard APIs whose semantics depend on Vista-era features such as
UAC, split tokens, or elevation metadata.
