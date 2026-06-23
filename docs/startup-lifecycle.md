# Startup Lifecycle

## Purpose

This document explains the application entry point and startup ownership model.
It stays at the composition level: process roles, WPF application setup, and
single-instance activation. Privileged host IPC details live in
`docs/elevated-agent-ipc.md`.

## Entry Point

`Program.cs` is the only executable startup entry point. The project file sets
`StartupObject` to `WinCraft.Program`, so all process modes start there before
any WPF window is created.

`Program` is responsible for process-mode routing:

- SYSTEM helper modes
- TrustedInstaller helper modes
- elevated agent mode
- elevated bootstrap mode
- full-administrator UI mode
- normal unelevated UI mode

Keep this high-level routing centralized. New startup modes should enter
through `Program` and then delegate to focused infrastructure or feature code
when the behavior grows beyond startup composition.

## WPF Application Object

`App.xaml` exists to define the WPF `Application` type and hold application
resources. It is not the startup driver.

The UI process creates `App` explicitly from `Program`, calls
`InitializeComponent`, creates `MainWindow`, and then runs the dispatcher
through the single-instance host. This keeps startup decisions in ordinary C#
code where command-line mode checks, elevation routing, and service setup can
be ordered explicitly.

There is intentionally no `App.xaml.cs` today. Add one only when the application
needs real WPF application-level event handling or shared `Application` state.
Do not add it just to move startup code out of `Program`; startup routing and
process selection belong in `Program`.

## Single Instance Model

The visible UI is expected to be a single unelevated instance. `SingleInstanceHost`
owns that policy and runs the WPF dispatcher for the first instance.

When a later process starts with additional command-line arguments, the existing
UI receives the activation through `StartupNextInstance`. The active UI brings
its main window forward and handles the incoming command-line context.

This keeps shell interaction, drag-and-drop, and window activation in the
unelevated UI process. Elevated handoff behavior is part of the privileged host
model and is covered in `docs/elevated-agent-ipc.md`.

Built-in Administrator and other non split-token administrator sessions run the
UI directly instead of launching a separate unelevated copy. In that account
model the shell token already has the same administrator capability, so a
"downgrade" through Explorer would not reduce the UI token. Administrator-level
operations execute in-process, while `TrustedInstaller` operations still use the
privileged bridge.

## Design Notes

- Keep `Program` focused on startup composition, not product behavior.
- Keep reusable process, command-line, IPC, and elevation helpers outside
  `Program`.
- Keep WPF resources in `App.xaml`; keep startup routing out of XAML.
- Prefer adding focused infrastructure types over growing long inline startup
  workflows when a flow needs independent testing or reuse.
